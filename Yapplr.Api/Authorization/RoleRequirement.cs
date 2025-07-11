using Microsoft.AspNetCore.Authorization;
using Yapplr.Api.Models;

namespace Yapplr.Api.Authorization;

public class RoleRequirement(UserRole minimumRole, UserStatus? userStatus = null) : IAuthorizationRequirement
{
    public UserRole MinimumRole { get; } = minimumRole;
    public UserStatus? UserStatus { get; } = userStatus;
}

