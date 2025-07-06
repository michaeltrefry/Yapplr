using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Extensions;

public static class UserExtensions
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto(
            user.Id,
            user.Email,
            user.Username,
            user.Bio,
            user.Birthday,
            user.Pronouns,
            user.Tagline,
            user.ProfileImageFileName,
            user.CreatedAt,
            user.FcmToken,
            user.EmailVerified,
            user.Role,
            user.Status
        );
    }
}
