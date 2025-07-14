using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class ContentModerationService : IContentModerationService
{
    private readonly HttpClient _httpClient;
    private readonly YapplrDbContext _context;
    private readonly ILogger<ContentModerationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;
    private readonly string _moderationServiceUrl;

    public ContentModerationService(
        HttpClient httpClient,
        YapplrDbContext context,
        ILogger<ContentModerationService> logger,
        IConfiguration configuration,
        IAuditService auditService)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _auditService = auditService;
        _moderationServiceUrl = _configuration.GetValue<string>("ContentModeration:ServiceUrl") ?? "http://localhost:8000";
        
        // Configure HttpClient
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<ContentModerationResult> AnalyzeContentAsync(string content, bool includeSentiment = true)
    {
        try
        {
            var request = new
            {
                text = content,
                include_sentiment = includeSentiment
            };

            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_moderationServiceUrl}/moderate", httpContent);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Content moderation service returned {StatusCode}: {Content}", 
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return CreateFallbackResult(content);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("AI Moderation Service Response: {Response}", responseContent);

            var result = JsonSerializer.Deserialize<ContentModerationApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("Parsed Result - Risk Level: {Level}, Score: {Score}, RequiresReview: {RequiresReview}",
                result?.RiskAssessment?.Level, result?.RiskAssessment?.Score, result?.RequiresReview);

            return MapToContentModerationResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing content with moderation service");
            return CreateFallbackResult(content);
        }
    }

    public async Task<IEnumerable<ContentModerationResult>> AnalyzeContentBatchAsync(IEnumerable<string> contents, bool includeSentiment = true)
    {
        try
        {
            var contentList = contents.ToList();
            if (contentList.Count == 0)
                return Enumerable.Empty<ContentModerationResult>();

            var request = new
            {
                texts = contentList,
                include_sentiment = includeSentiment
            };

            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_moderationServiceUrl}/batch-moderate", httpContent);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Batch content moderation service returned {StatusCode}: {Content}", 
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return contentList.Select(CreateFallbackResult);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<BatchContentModerationApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Results?.Select(MapToContentModerationResult) ?? contentList.Select(CreateFallbackResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing content batch with moderation service");
            return contents.Select(CreateFallbackResult);
        }
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_moderationServiceUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Content moderation service health check failed");
            return false;
        }
    }

    public async Task<bool> ApplySuggestedTagsToPostAsync(int postId, ContentModerationResult moderationResult, int appliedByUserId)
    {
        try
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("Cannot apply suggested tags: Post {PostId} not found", postId);
                return false;
            }

            var appliedTags = new List<PostSystemTag>();

            foreach (var (category, tagNames) in moderationResult.SuggestedTags)
            {
                foreach (var tagName in tagNames)
                {
                    var systemTag = await _context.SystemTags
                        .FirstOrDefaultAsync(st => st.Name == tagName && st.IsActive);

                    if (systemTag != null)
                    {
                        // Check if tag is already applied
                        var existingTag = await _context.PostSystemTags
                            .FirstOrDefaultAsync(pst => pst.PostId == postId && pst.SystemTagId == systemTag.Id);

                        if (existingTag == null)
                        {
                            var postSystemTag = new PostSystemTag
                            {
                                PostId = postId,
                                SystemTagId = systemTag.Id,
                                AppliedByUserId = appliedByUserId,
                                Reason = $"AI-suggested: {category} - Risk Level: {moderationResult.RiskAssessment.Level}",
                                AppliedAt = DateTime.UtcNow
                            };

                            _context.PostSystemTags.Add(postSystemTag);
                            appliedTags.Add(postSystemTag);
                        }
                    }
                }
            }

            if (appliedTags.Any())
            {
                // Save the system tags first
                await _context.SaveChangesAsync();
                _logger.LogInformation("Applied {Count} AI-suggested system tags to post {PostId}", appliedTags.Count, postId);

                // Create audit logs for each applied tag (with error handling)
                foreach (var appliedTag in appliedTags)
                {
                    try
                    {
                        // Verify the post still exists before creating audit log
                        var postStillExists = await _context.Posts.AnyAsync(p => p.Id == postId);
                        if (postStillExists)
                        {
                            await _auditService.LogPostSystemTagAddedAsync(
                                postId,
                                appliedTag.SystemTagId,
                                appliedByUserId,
                                appliedTag.Reason
                            );
                        }
                        else
                        {
                            _logger.LogWarning("Skipping audit log for post {PostId} - post was deleted", postId);
                        }
                    }
                    catch (Exception auditEx)
                    {
                        _logger.LogError(auditEx, "Failed to create audit log for system tag {SystemTagId} on post {PostId}",
                            appliedTag.SystemTagId, postId);
                        // Don't fail the entire operation if audit logging fails
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying suggested tags to post {PostId}", postId);
            return false;
        }
    }

    public async Task<bool> ApplySuggestedTagsToCommentAsync(int commentId, ContentModerationResult moderationResult, int appliedByUserId)
    {
        try
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return false;

            var appliedTags = new List<CommentSystemTag>();

            foreach (var (category, tagNames) in moderationResult.SuggestedTags)
            {
                foreach (var tagName in tagNames)
                {
                    var systemTag = await _context.SystemTags
                        .FirstOrDefaultAsync(st => st.Name == tagName && st.IsActive);

                    if (systemTag != null)
                    {
                        // Check if tag is already applied
                        var existingTag = await _context.CommentSystemTags
                            .FirstOrDefaultAsync(cst => cst.CommentId == commentId && cst.SystemTagId == systemTag.Id);

                        if (existingTag == null)
                        {
                            var commentSystemTag = new CommentSystemTag
                            {
                                CommentId = commentId,
                                SystemTagId = systemTag.Id,
                                AppliedByUserId = appliedByUserId,
                                Reason = $"AI-suggested: {category} - Risk Level: {moderationResult.RiskAssessment.Level}",
                                AppliedAt = DateTime.UtcNow
                            };

                            _context.CommentSystemTags.Add(commentSystemTag);
                            appliedTags.Add(commentSystemTag);
                        }
                    }
                }
            }

            if (appliedTags.Any())
            {
                // Save the system tags first
                await _context.SaveChangesAsync();
                _logger.LogInformation("Applied {Count} AI-suggested system tags to comment {CommentId}", appliedTags.Count, commentId);

                // Create audit logs for each applied tag (with error handling)
                foreach (var appliedTag in appliedTags)
                {
                    try
                    {
                        // Verify the comment still exists before creating audit log
                        var commentStillExists = await _context.Comments.AnyAsync(c => c.Id == commentId);
                        if (commentStillExists)
                        {
                            await _auditService.LogCommentSystemTagAddedAsync(
                                commentId,
                                appliedTag.SystemTagId,
                                appliedByUserId,
                                appliedTag.Reason
                            );
                        }
                        else
                        {
                            _logger.LogWarning("Skipping audit log for comment {CommentId} - comment was deleted", commentId);
                        }
                    }
                    catch (Exception auditEx)
                    {
                        _logger.LogError(auditEx, "Failed to create audit log for system tag {SystemTagId} on comment {CommentId}",
                            appliedTag.SystemTagId, commentId);
                        // Don't fail the entire operation if audit logging fails
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying suggested tags to comment {CommentId}", commentId);
            return false;
        }
    }

    private ContentModerationResult CreateFallbackResult(string content)
    {
        return new ContentModerationResult
        {
            Text = content,
            SuggestedTags = new Dictionary<string, List<string>>(),
            RiskAssessment = new RiskAssessment
            {
                Score = 0.0,
                Level = "UNKNOWN"
            },
            RequiresReview = false
        };
    }

    private ContentModerationResult MapToContentModerationResult(ContentModerationApiResponse? apiResponse)
    {
        if (apiResponse == null)
            return CreateFallbackResult("");

        return new ContentModerationResult
        {
            Text = apiResponse.Text ?? "",
            Sentiment = apiResponse.Sentiment != null ? new SentimentResult
            {
                Label = apiResponse.Sentiment.Label ?? "",
                Confidence = apiResponse.Sentiment.Confidence
            } : null,
            SuggestedTags = apiResponse.SuggestedTags ?? new Dictionary<string, List<string>>(),
            RiskAssessment = new RiskAssessment
            {
                Score = apiResponse.RiskAssessment?.Score ?? 0.0,
                Level = apiResponse.RiskAssessment?.Level ?? "UNKNOWN"
            },
            RequiresReview = apiResponse.RequiresReview
        };
    }
}
