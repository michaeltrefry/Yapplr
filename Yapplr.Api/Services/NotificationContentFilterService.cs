using System.Text.RegularExpressions;

namespace Yapplr.Api.Services;

public class NotificationContentFilterService : INotificationContentFilterService
{
    private readonly ILogger<NotificationContentFilterService> _logger;
    private ContentFilterConfig _config = new();
    
    // Profanity and inappropriate content patterns
    private readonly HashSet<string> _profanityWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Add common profanity words - this is a simplified list
        "spam", "scam", "phishing", "malware", "virus"
    };

    // Suspicious patterns
    private readonly List<Regex> _spamPatterns = new()
    {
        new Regex(@"(click here|act now|limited time|urgent|free money|guaranteed)", RegexOptions.IgnoreCase),
        new Regex(@"(\$\d+|\d+% off|free \w+)", RegexOptions.IgnoreCase),
        new Regex(@"(winner|congratulations|you've won)", RegexOptions.IgnoreCase),
        new Regex(@"(verify account|suspended|confirm identity)", RegexOptions.IgnoreCase)
    };

    private readonly List<Regex> _phishingPatterns = new()
    {
        new Regex(@"(login|password|account|verify|confirm|update).*(expired|suspended|locked)", RegexOptions.IgnoreCase),
        new Regex(@"(click|visit|go to).*(link|url|website)", RegexOptions.IgnoreCase),
        new Regex(@"(security|verification|authentication).*(required|needed|urgent)", RegexOptions.IgnoreCase)
    };

    // Suspicious domains (simplified list)
    private readonly HashSet<string> _suspiciousDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "bit.ly", "tinyurl.com", "t.co", "goo.gl", "ow.ly",
        "suspicious-domain.com", "phishing-site.net"
    };

    // URL detection pattern
    private readonly Regex _urlPattern = new(@"https?://[^\s]+", RegexOptions.IgnoreCase);

    // Statistics
    private long _totalValidations = 0;
    private long _totalViolations = 0;
    private long _totalBlocked = 0;
    private long _totalSanitized = 0;

    public NotificationContentFilterService(ILogger<NotificationContentFilterService> logger)
    {
        _logger = logger;
    }

    public async Task<ContentValidationResult> ValidateContentAsync(string content, string contentType = "text")
    {
        Interlocked.Increment(ref _totalValidations);

        var result = new ContentValidationResult
        {
            IsValid = true,
            SanitizedContent = content
        };

        if (string.IsNullOrWhiteSpace(content))
        {
            result.IsValid = false;
            result.Violations.Add("Content cannot be empty");
            return result;
        }

        // Check content length
        if (content.Length > _config.MaxContentLength)
        {
            result.IsValid = false;
            result.Violations.Add($"Content exceeds maximum length of {_config.MaxContentLength} characters");
            result.RiskLevel = ContentRiskLevel.Medium;
        }

        // Profanity filter
        if (_config.EnableProfanityFilter)
        {
            var profanityViolations = await DetectProfanityAsync(content);
            if (profanityViolations.Any())
            {
                result.Violations.AddRange(profanityViolations);
                result.RiskLevel = (ContentRiskLevel)Math.Max((int)result.RiskLevel, (int)ContentRiskLevel.Medium);
            }
        }

        // Spam detection
        if (_config.EnableSpamDetection)
        {
            var spamViolations = await DetectSpamAsync(content);
            if (spamViolations.Any())
            {
                result.Violations.AddRange(spamViolations);
                result.RiskLevel = (ContentRiskLevel)Math.Max((int)result.RiskLevel, (int)ContentRiskLevel.High);
            }
        }

        // Phishing detection
        if (_config.EnablePhishingDetection)
        {
            var phishingViolations = await DetectPhishingAsync(content);
            if (phishingViolations.Any())
            {
                result.Violations.AddRange(phishingViolations);
                result.RiskLevel = (ContentRiskLevel)Math.Max((int)result.RiskLevel, (int)ContentRiskLevel.Critical);
            }
        }

        // Malicious link detection
        if (_config.EnableMaliciousLinkDetection)
        {
            var linkViolations = await DetectSuspiciousLinksAsync(content);
            if (linkViolations.Any())
            {
                result.Violations.AddRange(linkViolations.Select(l => $"Suspicious link detected: {l}"));
                result.RiskLevel = (ContentRiskLevel)Math.Max((int)result.RiskLevel, (int)ContentRiskLevel.High);
            }
        }

        // Content sanitization
        if (_config.EnableContentSanitization)
        {
            result.SanitizedContent = await SanitizeContentAsync(content);
            if (result.SanitizedContent != content)
            {
                Interlocked.Increment(ref _totalSanitized);
            }
        }

        // Determine if content should be blocked
        if (result.Violations.Any())
        {
            Interlocked.Increment(ref _totalViolations);
            
            if (result.RiskLevel >= ContentRiskLevel.High)
            {
                result.IsValid = false;
                Interlocked.Increment(ref _totalBlocked);
            }
        }

        return result;
    }

    public async Task<ContentValidationResult> ValidateNotificationAsync(string title, string body, Dictionary<string, string>? data = null)
    {
        var titleResult = await ValidateContentAsync(title, "title");
        var bodyResult = await ValidateContentAsync(body, "body");

        var combinedResult = new ContentValidationResult
        {
            IsValid = titleResult.IsValid && bodyResult.IsValid,
            SanitizedContent = $"{titleResult.SanitizedContent}|{bodyResult.SanitizedContent}",
            RiskLevel = (ContentRiskLevel)Math.Max((int)titleResult.RiskLevel, (int)bodyResult.RiskLevel)
        };

        combinedResult.Violations.AddRange(titleResult.Violations.Select(v => $"Title: {v}"));
        combinedResult.Violations.AddRange(bodyResult.Violations.Select(v => $"Body: {v}"));

        // Validate data if present
        if (data != null)
        {
            foreach (var kvp in data)
            {
                var dataResult = await ValidateContentAsync(kvp.Value, $"data.{kvp.Key}");
                if (!dataResult.IsValid)
                {
                    combinedResult.IsValid = false;
                    combinedResult.Violations.AddRange(dataResult.Violations.Select(v => $"Data.{kvp.Key}: {v}"));
                    combinedResult.RiskLevel = (ContentRiskLevel)Math.Max((int)combinedResult.RiskLevel, (int)dataResult.RiskLevel);
                }
            }
        }

        return combinedResult;
    }

    public async Task<bool> IsContentSafeAsync(string content)
    {
        var result = await ValidateContentAsync(content);
        return result.IsValid && result.RiskLevel < ContentRiskLevel.High;
    }

    public async Task<string> SanitizeContentAsync(string content)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(content))
            return content;

        var sanitized = content;

        // Remove or replace profanity
        foreach (var word in _profanityWords)
        {
            var pattern = new Regex($@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase);
            sanitized = pattern.Replace(sanitized, new string('*', word.Length));
        }

        // Remove suspicious URLs
        var urls = _urlPattern.Matches(sanitized);
        foreach (Match url in urls)
        {
            if (IsSuspiciousUrl(url.Value))
            {
                sanitized = sanitized.Replace(url.Value, "[LINK REMOVED]");
            }
        }

        // Trim excessive whitespace
        sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();

        // Limit length
        if (sanitized.Length > _config.MaxContentLength)
        {
            sanitized = sanitized[.._config.MaxContentLength] + "...";
        }

        return sanitized;
    }

    public async Task<List<string>> DetectSuspiciousLinksAsync(string content)
    {
        await Task.CompletedTask;

        var suspiciousLinks = new List<string>();
        var urls = _urlPattern.Matches(content);

        if (urls.Count > _config.MaxUrlsPerMessage)
        {
            suspiciousLinks.Add($"Too many URLs ({urls.Count} > {_config.MaxUrlsPerMessage})");
        }

        foreach (Match url in urls)
        {
            if (IsSuspiciousUrl(url.Value))
            {
                suspiciousLinks.Add(url.Value);
            }
        }

        return suspiciousLinks;
    }

    public async Task<Dictionary<string, object>> GetFilterStatsAsync()
    {
        await Task.CompletedTask;

        return new Dictionary<string, object>
        {
            ["total_validations"] = _totalValidations,
            ["total_violations"] = _totalViolations,
            ["total_blocked"] = _totalBlocked,
            ["total_sanitized"] = _totalSanitized,
            ["violation_rate"] = _totalValidations > 0 ? (double)_totalViolations / _totalValidations * 100 : 0,
            ["block_rate"] = _totalValidations > 0 ? (double)_totalBlocked / _totalValidations * 100 : 0,
            ["sanitization_rate"] = _totalValidations > 0 ? (double)_totalSanitized / _totalValidations * 100 : 0,
            ["config"] = new
            {
                enable_profanity_filter = _config.EnableProfanityFilter,
                enable_spam_detection = _config.EnableSpamDetection,
                enable_phishing_detection = _config.EnablePhishingDetection,
                enable_malicious_link_detection = _config.EnableMaliciousLinkDetection,
                max_content_length = _config.MaxContentLength,
                max_urls_per_message = _config.MaxUrlsPerMessage
            }
        };
    }

    public async Task UpdateFilterConfigAsync(ContentFilterConfig config)
    {
        await Task.CompletedTask;
        _config = config;
        _logger.LogInformation("Updated content filter configuration");
    }

    private async Task<List<string>> DetectProfanityAsync(string content)
    {
        await Task.CompletedTask;

        var violations = new List<string>();
        
        foreach (var word in _profanityWords)
        {
            if (content.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"Inappropriate content detected: {word}");
            }
        }

        return violations;
    }

    private async Task<List<string>> DetectSpamAsync(string content)
    {
        await Task.CompletedTask;

        var violations = new List<string>();

        foreach (var pattern in _spamPatterns)
        {
            if (pattern.IsMatch(content))
            {
                violations.Add($"Spam pattern detected: {pattern}");
            }
        }

        // Check for excessive repetition
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var wordCounts = words.GroupBy(w => w.ToLower()).ToDictionary(g => g.Key, g => g.Count());
        
        foreach (var kvp in wordCounts)
        {
            if (kvp.Value > 5) // Same word repeated more than 5 times
            {
                violations.Add($"Excessive repetition detected: '{kvp.Key}' repeated {kvp.Value} times");
            }
        }

        return violations;
    }

    private async Task<List<string>> DetectPhishingAsync(string content)
    {
        await Task.CompletedTask;

        var violations = new List<string>();

        foreach (var pattern in _phishingPatterns)
        {
            if (pattern.IsMatch(content))
            {
                violations.Add($"Phishing pattern detected: {pattern}");
            }
        }

        return violations;
    }

    private bool IsSuspiciousUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            
            // Check against known suspicious domains
            if (_config.BlockSuspiciousDomains && _suspiciousDomains.Contains(uri.Host))
            {
                return true;
            }

            // Check for URL shorteners (could hide malicious links)
            var shorteners = new[] { "bit.ly", "tinyurl.com", "t.co", "goo.gl", "ow.ly" };
            if (shorteners.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check for suspicious patterns in URL
            if (uri.AbsoluteUri.Contains("phish", StringComparison.OrdinalIgnoreCase) ||
                uri.AbsoluteUri.Contains("malware", StringComparison.OrdinalIgnoreCase) ||
                uri.AbsoluteUri.Contains("virus", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
        catch
        {
            // Invalid URL format is suspicious
            return true;
        }
    }
}
