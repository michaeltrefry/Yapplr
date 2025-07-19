using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public record ReactToCommentDto(
    [Required] ReactionType ReactionType
);
