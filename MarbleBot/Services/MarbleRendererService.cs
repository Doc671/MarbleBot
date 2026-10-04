using MarbleBot.Data.Repositories;
using MarbleBot.Entities;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace MarbleBot.Services;

public class MarbleRendererService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MarbleRendererService(IServiceScopeFactory serviceScopeFactory)
    {
        _scopeFactory = serviceScopeFactory;
    }

    public async Task<MemoryStream> RenderMarbleApperanceAsync(ulong userId, uint imageBackground, int imageWidth, int imageHeight, int marblePositionX, int marblePositionY, int marbleRadius)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var marbleAppearanceDao = scope.ServiceProvider.GetRequiredService<MarbleAppearanceDao>();

        var imageInfo = new SKImageInfo(imageWidth, imageHeight);

        using SKSurface surface = SKSurface.Create(imageInfo);

        SKCanvas canvas = surface.Canvas;
        canvas.Clear(imageBackground);

        var marblePosition = new SKPoint(marblePositionX, marblePositionY);

        var appearance = await marbleAppearanceDao.GetAppearanceByUserIdAsync(userId);
        RenderMarbleToCanvas(canvas, appearance, marblePosition, marbleRadius);

        using SKImage image = surface.Snapshot();

        const int imageQuality = 100;
        using SKData data = image.Encode(SKEncodedImageFormat.Png, imageQuality);

        var memoryStream = new MemoryStream();
        data.SaveTo(memoryStream);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public static void RenderMarbleToCanvas(SKCanvas canvas, MarbleAppearance? appearance, SKPoint marblePosition, int marbleRadius)
    {
        SKColor[] colors;
        if (appearance == null)
        {
            colors = [new SKColor()];
        }
        else
        {
            colors = appearance.Colors
                .OrderBy(appearanceColor => appearanceColor.Order)
                .Select(appearanceColor =>
                {
                    var color = new SKColor(appearanceColor.RawColor);
                    if (color.Alpha == 0)
                    {
                        color = color.WithAlpha(255);
                    }
                    return color;
                })
                .ToArray();
        }

        switch (appearance?.AppearanceType ?? MarbleAppearanceType.Solid)
        {
            case MarbleAppearanceType.Solid:
                RenderSolidMarble(canvas, colors[0], marblePosition, marbleRadius);
                break;
            case MarbleAppearanceType.Concentric:
                RenderConcentricMarble(canvas, colors, marblePosition, marbleRadius);
                break;
            case MarbleAppearanceType.Sectors:
                RenderSectorsMarble(canvas, colors, marblePosition, marbleRadius, appearance!.AngleOffset ?? 0f);
                break;
            case MarbleAppearanceType.LinearGradient:
                RenderLinearGradientMarble(canvas, colors, marblePosition, marbleRadius, appearance!.AngleOffset ?? 0f);
                break;
            case MarbleAppearanceType.RadialGradient:
                RenderRadialGradientMarble(canvas, colors, marblePosition, marbleRadius);
                break;
        }
    }

    private static void RenderSolidMarble(SKCanvas canvas, SKColor color, SKPoint marblePosition, int marbleRadius)
    {
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(marblePosition, marbleRadius, paint);
    }

    private static void RenderConcentricMarble(SKCanvas canvas, SKColor[] colors, SKPoint marblePosition, int marbleRadius)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
               
        for (int i = 0; i < colors.Length; i++)
        {
            paint.Color = colors[i];
            float radius = marbleRadius * (1 - (float)i / colors.Length);
            canvas.DrawCircle(marblePosition, radius, paint);
        }
    }

    private static void RenderSectorsMarble(SKCanvas canvas, SKColor[] colors, SKPoint marblePosition, int marbleRadius, float angleOffset)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        float sweepAngle = 360f / colors.Length;
        var rect = new SKRect(marblePosition.X - marbleRadius, marblePosition.Y - marbleRadius, marblePosition.X + marbleRadius, marblePosition.Y + marbleRadius);

        for (int i = 0; i < colors.Length; i++) 
        {
            paint.Color = colors[i];

            var pathBuilder = new SKPathBuilder();
            pathBuilder.MoveTo(marblePosition);
            pathBuilder.ArcTo(rect, i * sweepAngle + angleOffset, sweepAngle, false);
            canvas.DrawPath(pathBuilder.Snapshot(), paint);
        }
    }

    private static void RenderLinearGradientMarble(SKCanvas canvas, SKColor[] colors, SKPoint marblePosition, int marbleRadius, float gradientAngleDegrees)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        float gradientAngleRadians = gradientAngleDegrees * MathF.PI / 180;
        (float sinAngle, float cosAngle) = MathF.SinCos(gradientAngleRadians);
        var gradientStart = new SKPoint(marblePosition.X - marbleRadius * cosAngle, marblePosition.Y - marbleRadius * sinAngle);
        var gradientEnd = new SKPoint(marblePosition.X + marbleRadius * cosAngle, marblePosition.Y + marbleRadius * sinAngle);

        using var shader = SKShader.CreateLinearGradient(gradientStart, gradientEnd, colors, null, SKShaderTileMode.Clamp);
        paint.Shader = shader;

        canvas.DrawCircle(marblePosition, marbleRadius, paint);
    }

    private static void RenderRadialGradientMarble(SKCanvas canvas, SKColor[] colors, SKPoint marblePosition, int marbleRadius)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        using var shader = SKShader.CreateRadialGradient(marblePosition, marbleRadius, colors, null, SKShaderTileMode.Clamp);
        paint.Shader = shader;

        canvas.DrawCircle(marblePosition, marbleRadius, paint);
    }
}
