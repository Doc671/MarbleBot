using MarbleBot.Entities.Games.Siege;
using Microsoft.EntityFrameworkCore;

namespace MarbleBot.Data.Repositories.Games;

public class BossDao
{
    private readonly MarbleBotDbContext _dbContext;

    public BossDao(MarbleBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Boss>> GetAllAsync()
    {
        return await _dbContext.Bosses
            .AsNoTracking()
            .Include(boss => boss.Attacks)
            .Include(boss => boss.Drops)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Boss>> GetAllInStageAsync(int stage)
    {
        return await _dbContext.Bosses
            .AsNoTracking()
            .Include(boss => boss.Attacks)
            .Include(boss => boss.Drops)
            .Where(boss => boss.Stage == stage)
            .OrderBy(boss => boss.Difficulty)
            .ToListAsync();
    }

    public async Task<Boss?> GetByNameAsync(string name)
    {
        return await _dbContext.Bosses
            .AsNoTracking()
            .Include(boss => boss.Attacks)
            .Include(boss => boss.Drops)
            .ThenInclude(drop => drop.Item)
            .SingleOrDefaultAsync(boss => boss.Name == name);
    }
}
