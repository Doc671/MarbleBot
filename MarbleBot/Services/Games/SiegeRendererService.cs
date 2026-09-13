using MarbleBot.Data.Repositories;
using MarbleBot.Entities;
using MarbleBot.Games.Siege;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace MarbleBot.Services.Games;

public class SiegeRendererService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SiegeRendererService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<MemoryStream> RenderMarblesWithAttackWarningAsync(IReadOnlyList<SiegeMarble> marbles, uint imageBackground, int maxXSpots, int minX, int attackMinX, int attackMaxX)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var marbleAppearanceDao = scope.ServiceProvider.GetRequiredService<MarbleAppearanceDao>();

        const int marbleRadius = 16;
        int maxY = marbles.Max(marble => marble.Y);

        int imageHeight = marbleRadius * (maxY + 1) * 4;
        int imageWidth = imageHeight * maxXSpots;
        var imageInfo = new SKImageInfo(imageWidth, imageHeight);

        using SKSurface surface = SKSurface.Create(imageInfo);

        SKCanvas canvas = surface.Canvas;
        canvas.Clear(imageBackground);

        int rowHeight = (int)Math.Round((float)imageHeight / (maxY + 1));

        foreach (SiegeMarble marble in marbles)
        {
            MarbleAppearance? appearance = await marbleAppearanceDao.GetAppearanceByUserIdAsync(marble.Id);
            appearance ??= new MarbleAppearance
            {
                AppearanceType = MarbleAppearanceType.Solid,
                UserId = marble.Id,
                Colors =
                [
                    new MarbleAppearanceColor()
                    {
                        RawColor = Color.LighterGrey
                    }
                ]
            };

            int imageX = (marble.X - minX) * imageHeight + marbleRadius * 2;
            int imageY = (maxY - marble.Y) * rowHeight + marbleRadius * 2;
            var imagePosition = new SKPoint(imageX, imageY);

            MarbleRendererService.RenderMarbleToCanvas(canvas, appearance, imagePosition, marbleRadius);
        }

        int attackWidthImage = (attackMaxX - attackMinX) * imageHeight;
        int attackXImage = (attackMinX - minX) * imageHeight;
        var attackWarningRect = SKRectI.Create(attackXImage, 0, attackWidthImage, imageHeight);
        using var attackWarningPaint = new SKPaint()
        {
            Color = new SKColor(255, 0, 0, 63)
        };
        canvas.DrawRect(attackWarningRect, attackWarningPaint);

        using SKImage image = surface.Snapshot();

        const int imageQuality = 100;
        using SKData data = image.Encode(SKEncodedImageFormat.Png, imageQuality);

        var memoryStream = new MemoryStream();
        data.SaveTo(memoryStream);
        memoryStream.Position = 0;

        return memoryStream;
    }
}
