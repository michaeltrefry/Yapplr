using System.Security.Claims;

namespace Yapplr.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user, bool throwException = false)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            return userId;
        if (throwException)
        {
            throw new BadHttpRequestException("Missing user id claim.");
        }
        return -1;
    }
}