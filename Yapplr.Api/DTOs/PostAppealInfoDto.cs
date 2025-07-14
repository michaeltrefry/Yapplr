using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public record PostAppealInfoDto(
    int Id,
    AppealStatus Status,
    string Reason,
    string? AdditionalInfo,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? ReviewedByUsername,
    string? ReviewNotes
);