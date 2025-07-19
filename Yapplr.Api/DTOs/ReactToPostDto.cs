using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public record ReactionDto(
    [Required] ReactionType ReactionType
);
