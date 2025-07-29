using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Common;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;

namespace Yapplr.Api.Services;

public class BlockService : IBlockService
{
    private readonly YapplrDbContext _context;

    public BlockService(YapplrDbContext context)
    {
        _context = context;
    }

    public async Task<bool> BlockUserAsync(int blockerId, int blockedId)
    {
        // Can't block yourself
        if (blockerId == blockedId)
            return false;

        // Check if already blocked
        if (await _context.Blocks.AnyAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId))
            return false;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Remove any existing follow relationships in both directions
            var followsToRemove = await _context.Follows
                .Where(f => (f.FollowerId == blockerId && f.FollowingId == blockedId) ||
                           (f.FollowerId == blockedId && f.FollowingId == blockerId))
                .ToListAsync();

            if (followsToRemove.Any())
            {
                _context.Follows.RemoveRange(followsToRemove);
            }

            // Create the block relationship
            var block = new Block
            {
                BlockerId = blockerId,
                BlockedId = blockedId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Blocks.Add(block);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> UnblockUserAsync(int blockerId, int blockedId)
    {
        var block = await _context.Blocks
            .FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId);

        if (block == null)
            return false;

        _context.Blocks.Remove(block);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsUserBlockedAsync(int blockerId, int blockedId)
    {
        return await _context.Blocks
            .AnyAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId);
    }

    public async Task<bool> IsBlockedByUserAsync(int userId, int potentialBlockerId)
    {
        return await _context.Blocks
            .AnyAsync(b => b.BlockerId == potentialBlockerId && b.BlockedId == userId);
    }

    public async Task<IEnumerable<UserDto>> GetBlockedUsersAsync(int userId)
    {
        var blockedUsers = await _context.Blocks
            .Include(b => b.Blocked)
            .Where(b => b.BlockerId == userId)
            .Select(b => b.Blocked)
            .ToListAsync();

        return blockedUsers.Select(user => user.MapToUserDto());
    }
}
