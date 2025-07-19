using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public record ReactionCountDto(
    ReactionType ReactionType,
    string Emoji,
    string DisplayName,
    int Count
);
