using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarbleBot.Data.Repositories;

public class ItemDao
{
    private readonly MarbleBotDbContext _dbContext;

    public ItemDao(MarbleBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Items.FindAsync([id], cancellationToken);
    }

    public async Task<List<Item>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Items.ToListAsync(cancellationToken);
    }

    public async Task<Item> CreateAsync(Item item, CancellationToken cancellationToken = default)
    {
        _dbContext.Items.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task UpdateAsync(Item item, CancellationToken cancellationToken = default)
    {
        _dbContext.Items.Update(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Item item, CancellationToken cancellationToken = default)
    {
        Item? foundItem = await _dbContext.Items.FindAsync([item.Id], cancellationToken);
        if (foundItem != null)
        {
            _dbContext.Items.Remove(foundItem);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
