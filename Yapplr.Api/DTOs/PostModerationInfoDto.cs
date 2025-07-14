namespace Yapplr.Api.DTOs;

public record PostModerationInfoDto(
    bool IsHidden,
    string? HiddenReason,
    DateTime? HiddenAt,
    UserDto? HiddenByUser,
    IEnumerable<PostSystemTagDto> SystemTags,
    double? RiskScore,
    string? RiskLevel,
    PostAppealInfoDto? AppealInfo
);