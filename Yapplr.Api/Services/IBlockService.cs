using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

public interface IBlockService
{
    Task<bool> BlockUserAsync(int blockerId, int blockedId);
    Task<bool> UnblockUserAsync(int blockerId, int blockedId);
    Task<bool> IsUserBlockedAsync(int blockerId, int blockedId);
    Task<IEnumerable<UserDto>> GetBlockedUsersAsync(int userId);
    Task<bool> IsBlockedByUserAsync(int userId, int potentialBlockerId);
}
