using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Extensions;

public static class TagExtensions
{
    public static TagDto ToDto(this Tag tag)
    {
        return new TagDto(
            tag.Id,
            tag.Name,
            tag.PostCount
        );
    }
}
