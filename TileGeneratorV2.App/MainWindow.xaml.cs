using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TileGeneratorV2.App.Helpers;
using TileGeneratorV2.Core.Materials;
using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.App;

public partial class MainWindow : Window
{
    private static readonly Random _globalRng = new();

    public MainWindow()
    {
        InitializeComponent();
        UpdateColorPreviews();
        try { Generate(); }
        catch (Exception ex) { StatusLabel.Text = $"Error: {ex.Message}"; }
    }

    // ── Event handlers ──────────────────────────────────────────────────────

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        try { Generate(); }
        catch (Exception ex) { StatusLabel.Text = $"Error: {ex.Message}\n{ex.StackTrace}"; }
    }

    private void RandomizeSeed_Click(object sender, RoutedEventArgs e)
    {
        SeedBox.Text = _globalRng.Next().ToString();
        ApplyRandomColors();
        try { Generate(); }
        catch (Exception ex) { StatusLabel.Text = $"Error: {ex.Message}"; }
    }

    private void RandomizeColors_Click(object sender, RoutedEventArgs e)
    {
        ApplyRandomColors();
        try { Generate(); }
        catch (Exception ex) { StatusLabel.Text = $"Error: {ex.Message}"; }
    }

    private void ApplyRandomColors()
    {
        float hue1 = (float)_globalRng.NextDouble() * 360f;

        // 35% analogous (±5–25°), 65% wide separation (60°, 90°, 120°, 150°, 180°, ±split)
        float hue2;
        if (_globalRng.NextDouble() < 0.35)
        {
            float shift = 5f + (float)_globalRng.NextDouble() * 20f;
            hue2 = (hue1 + (_globalRng.NextDouble() < 0.5 ? shift : -shift) + 360f) % 360f;
        }
        else
        {
            float[] separations = { 60f, 90f, 120f, 150f, 180f, 210f, 240f };
            float sep = separations[_globalRng.Next(separations.Length)];
            hue2 = (hue1 + (_globalRng.NextDouble() < 0.5 ? sep : -sep) + 360f) % 360f;
        }

        float sat = 25f + (float)_globalRng.NextDouble() * 55f;

        float lightCenter = 30f + (float)_globalRng.NextDouble() * 40f;
        float halfRange   = 20f + (float)_globalRng.NextDouble() * 20f;
        float light = Math.Clamp(lightCenter + halfRange, 45f, 90f);
        float dark  = Math.Clamp(lightCenter - halfRange,  5f, 45f);

        Hue1Box.Text  = $"{hue1:F0}";
        Hue2Box.Text  = $"{hue2:F0}";
        SatBox.Text   = $"{sat:F0}";
        LightBox.Text = $"{light:F0}";
        DarkBox.Text  = $"{dark:F0}";
    }

    private void SeedBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            try { Generate(); } catch (Exception ex) { StatusLabel.Text = $"Error: {ex.Message}"; }
    }

    private void ColorBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => UpdateColorPreviews();

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void UpdateColorPreviews()
    {
        if (Hue1Preview is null || Hue2Preview is null) return;
        float sat   = ParsePercent(SatBox?.Text,   48f) / 100f;
        float light = ParsePercent(LightBox?.Text, 62f) / 100f;
        float dark  = ParsePercent(DarkBox?.Text,  14f) / 100f;
        float midLit = (light + dark) * 0.5f;

        TrySetHuePreview(Hue1Preview, ParseDegrees(Hue1Box?.Text, 30f), sat, midLit);
        TrySetHuePreview(Hue2Preview, ParseDegrees(Hue2Box?.Text, 26f), sat, midLit);
    }

    private static void TrySetHuePreview(System.Windows.Controls.Border border, float hueDeg, float sat, float lit)
    {
        try
        {
            var c = ColorMath.HslToRgb(hueDeg / 360f, sat, lit);
            border.Background = new SolidColorBrush(BitmapHelper.ToWpfColor(c));
        }
        catch { /* ignore malformed input */ }
    }

    private void Generate()
    {
        int seed = int.TryParse(SeedBox.Text, out int s) ? s : 0;

        float hue1  = ParseDegrees(Hue1Box.Text,  30f) / 360f;
        float hue2  = ParseDegrees(Hue2Box.Text,  26f) / 360f;
        float sat   = ParsePercent(SatBox.Text,   48f) / 100f;
        float light = ParsePercent(LightBox.Text, 62f) / 100f;
        float dark  = ParsePercent(DarkBox.Text,  14f) / 100f;

        var palette = new ColorPalette(hue1, hue2, sat, light, dark);

        MaterialParameters? matParams = MaterialCombo.SelectedIndex == 0
            ? null
            : MaterialParameters.FromSeed(seed, (MaterialType)MaterialCombo.SelectedIndex);

        var sw     = Stopwatch.StartNew();
        var buffer = new MaterialGenerator().Generate(seed, palette, matParams);
        sw.Stop();

        SingleTileImage.Source    = BitmapHelper.ToBitmapSource(buffer, scale: 4);
        TilingPreviewImage.Source = BitmapHelper.ToTiledBitmapSource(buffer, tilesX: 4, tilesY: 4, scale: 2);

        var effectiveParams = matParams ?? MaterialParameters.FromSeed(seed);
        StatusLabel.Text = $"{effectiveParams.Type}  freq={effectiveParams.BaseFrequency}  " +
                           $"oct={effectiveParams.Octaves}  curve={effectiveParams.Curve}\n" +
                           $"Generated in {sw.ElapsedMilliseconds}ms";
    }

    private static float ParseDegrees(string? text, float fallback)
        => float.TryParse(text, out float v) ? ((v % 360f) + 360f) % 360f : fallback;

    private static float ParsePercent(string? text, float fallback)
        => float.TryParse(text, out float v) ? Math.Clamp(v, 0f, 100f) : fallback;
}
