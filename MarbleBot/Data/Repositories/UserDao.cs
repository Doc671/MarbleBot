using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarbleBot.Data.Repositories;

public class UserDao
{
    private readonly MarbleBotDbContext _dbContext;

    public UserDao(MarbleBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetUserAsync(ulong id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .Include(user => user.Inventory)
            .ThenInclude(userInventoryItem => userInventoryItem.Item)
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<User> GetOrCreateUserAsync(ulong id, string? username = null, string? globalName = null, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([id], cancellationToken: cancellationToken);
        if (user != null)
        {
            return user;
        }

        user = new User
        {
            Id = id,
            Username = username,
            GlobalName = globalName
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateNamesAsync(ulong userId, string? username, string? globalName)
    {
        await _dbContext.Users
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.Username, username)
                .SetProperty(user => user.GlobalName, globalName));
    }

    public async Task AddBalanceAsync(ulong userId, decimal amount, CancellationToken cancellationToken = default)
    {
        var user = await GetOrCreateUserAsync(userId, cancellationToken: cancellationToken);
        user.Balance += amount;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryRemoveBalanceAsync(ulong userId, decimal amount, CancellationToken cancellationToken = default)
    {
        int rowsAffected = await _dbContext.Users
            .Where(user => user.Id == userId && user.Balance >= amount)
            .ExecuteUpdateAsync(updateBuilder => updateBuilder.SetProperty(user => user.Balance, user => user.Balance - amount), cancellationToken);

        return rowsAffected > 0;
    }

    public async Task<List<UserInventoryItem>> GetInventoryAsync(ulong userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserInventoryItems
            .Include(userInventoryItem => userInventoryItem.Item)
            .Where(userInventoryItem => userInventoryItem.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddInventoryItemAsync(ulong userId, int itemId, int quantity = 1, CancellationToken cancellationToken = default)
    {
        var existingRow = await _dbContext.UserInventoryItems.FindAsync([userId, itemId], cancellationToken);

        if (existingRow != null)
        {
            existingRow.Quantity += quantity;
            _dbContext.UserInventoryItems.Update(existingRow);
        }
        else
        {
            var newRow = new UserInventoryItem
            {
                UserId = userId,
                ItemId = itemId,
                Quantity = quantity
            };
            _dbContext.UserInventoryItems.Add(newRow);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryRemoveInventoryItemAsync(ulong userId, int itemId, int quantity = 1, CancellationToken cancellationToken = default)
    {
        int rowsAffected = await _dbContext.UserInventoryItems
            .Where(userInventoryItem => userInventoryItem.UserId == userId && userInventoryItem.ItemId == itemId && userInventoryItem.Quantity >= quantity)
            .ExecuteUpdateAsync(updateBuilder => updateBuilder.SetProperty(userInventoryItem => userInventoryItem.Quantity, userInventoryItem => userInventoryItem.Quantity - quantity), cancellationToken);

        return rowsAffected > 0;
    }
}
