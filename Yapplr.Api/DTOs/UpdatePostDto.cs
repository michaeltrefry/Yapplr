using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public record UpdatePostDto(
    [Required][StringLength(256, MinimumLength = 1)] string Content,
    PostPrivacy Privacy = PostPrivacy.Public
);