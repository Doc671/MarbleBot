using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarbleBot.Data.Repositories;

public class MarbleAppearanceDao
{
    private readonly MarbleBotDbContext _dbContext;

    public MarbleAppearanceDao(MarbleBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MarbleAppearance?> GetAppearanceByUserIdAsync(ulong userId)
    {
        return await _dbContext.MarbleAppearances
            .Include(appearance => appearance.Colors)
            .FirstOrDefaultAsync(appearance => appearance.UserId == userId);
    }

    public async Task UpdateAppearanceAsync(ulong userId, MarbleAppearanceType appearanceType, IReadOnlyList<Color> colors, float? gradientAngleDegrees = null)
    {
        var marbleAppearanceColors = colors.Select((color, index) => new MarbleAppearanceColor
        {
            UserId = userId,
            RawColor = color.RawValue,
            Order = index
        }).ToList();

        var existingAppearance = await _dbContext.MarbleAppearances
            .Include(appearance => appearance.Colors)
            .FirstOrDefaultAsync(appearance => appearance.UserId == userId);

        if (existingAppearance != null)
        {
            // Update existing appearance
            existingAppearance.Colors = marbleAppearanceColors;
            existingAppearance.AppearanceType = appearanceType;
            existingAppearance.GradientAngleDegrees = gradientAngleDegrees;
            _dbContext.MarbleAppearances.Update(existingAppearance);
        }
        else
        {
            // Create new appearance
            var newAppearance = new MarbleAppearance
            {
                UserId = userId,
                AppearanceType = appearanceType,
                Colors = marbleAppearanceColors,
                GradientAngleDegrees = gradientAngleDegrees
            };
            await _dbContext.MarbleAppearances.AddAsync(newAppearance);
        }

        await _dbContext.SaveChangesAsync();
    }
}
