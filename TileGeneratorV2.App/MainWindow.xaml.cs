using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TileGeneratorV2.App.Helpers;
using TileGeneratorV2.Core.Context;
using TileGeneratorV2.Core.Materials;
using TileGeneratorV2.Core.Structure;
using TileGeneratorV2.Core.Tileset;
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

    private void ExportTileset_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new SaveFileDialog
            {
                Title    = "Export tileset — choose manifest location",
                FileName = "manifest.json",
                Filter   = "Tileset manifest (*.json)|*.json",
            };
            if (dlg.ShowDialog() != true) return;

            string dir = Path.GetDirectoryName(dlg.FileName)!;

            int seed = int.TryParse(SeedBox.Text, out int s) ? s : 0;
            var palette = new ColorPalette(
                ParseDegrees(Hue1Box.Text,  30f) / 360f,
                ParseDegrees(Hue2Box.Text,  26f) / 360f,
                ParsePercent(SatBox.Text,   48f) / 100f,
                ParsePercent(LightBox.Text, 62f) / 100f,
                ParsePercent(DarkBox.Text,  14f) / 100f);
            MaterialParameters? matParams = MaterialCombo.SelectedIndex == 0
                ? null
                : MaterialParameters.FromSeed(seed, (MaterialType)MaterialCombo.SelectedIndex);
            var wallParams = StructureParameters.FromSeed(seed);

            var cap  = CapTilesetBuilder.Build(seed, palette, matParams, wallParams);
            var wall = WallTilesetBuilder.Build(seed, palette, matParams, wallParams);

            var manifest = new TilesetManifest
            {
                Seed = seed,
                Cap  = WriteAtlas(cap,  Path.Combine(dir, "cap.png"),  "cap.png"),
                Wall = WriteAtlas(wall, Path.Combine(dir, "wall.png"), "wall.png"),
            };
            File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(manifest, new JsonSerializerOptions
            {
                WriteIndented        = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }));

            StatusLabel.Text = $"Exported cap ({cap.Tiles.Count}) + wall ({wall.Tiles.Count}) tiles + manifest to {dir}";
        }
        catch (Exception ex) { StatusLabel.Text = $"Export error: {ex.Message}"; }
    }

    // Packs a tile grid into a tight atlas PNG and returns its manifest entry.
    // Autotile classes lay each variant on its own row (atlasColumns = cycleX·cycleY);
    // surface classes use a plain cycleX-wide grid.
    private static TilesetManifest.TileClass WriteAtlas(TileGrid grid, string path, string atlasName)
    {
        int cols  = grid.Variants > 1 ? grid.CycleX * grid.CycleY : grid.CycleX;
        var atlas = BitmapHelper.ToGridBitmapSource(grid.Tiles, columns: cols, scale: 1, gap: 0);
        using (var fs = File.Create(path))
        {
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(atlas));
            enc.Save(fs);
        }
        return new TilesetManifest.TileClass
        {
            Atlas        = atlasName,
            Variants     = grid.Variants,
            CycleX       = grid.CycleX,
            CycleY       = grid.CycleY,
            AtlasColumns = cols,
        };
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

        // Floor uses a different material seed so noise pattern differs, same palette
        int floorSeed = seed ^ unchecked((int)0x5A5A5A5A);
        MaterialParameters? floorMatParams = MaterialCombo.SelectedIndex == 0
            ? null
            : MaterialParameters.FromSeed(floorSeed, (MaterialType)MaterialCombo.SelectedIndex);

        var wallParams  = StructureParameters.FromSeed(seed);
        var floorParams = StructureParameters.FromSeedFloor(seed);
        var matGen      = new MaterialGenerator();
        var structGen   = new StructureGenerator();

        var sw = Stopwatch.StartNew();

        // ── Wall previews ──
        var wallSingle = matGen.Generate(seed, palette, matParams);
        structGen.Apply(wallSingle, 0, 0, wallParams);
        SingleTileImage.Source = BitmapHelper.ToBitmapSource(wallSingle, scale: 4);

        var wallTiles = new PixelBuffer[3, 3];
        for (int ty = 0; ty < 3; ty++)
        for (int tx = 0; tx < 3; tx++)
        {
            wallTiles[tx, ty] = matGen.Generate(seed, palette, matParams);
            structGen.Apply(wallTiles[tx, ty], tx, ty, wallParams);
        }
        TilingPreviewImage.Source = BitmapHelper.ToTiledBitmapSource(wallTiles, scale: 2);

        int wcx = StructureParameters.OddPart(wallParams.BrickWidth);
        int wcy = StructureParameters.OddPart(wallParams.BrickHeight);
        WallTilingLabel.Text = wcx == 1 && wcy == 1
            ? "TILING PREVIEW  3×3 tiles · self-tiling"
            : $"TILING PREVIEW  3×3 tiles · cycle {wcx}×{wcy}";

        // ── Floor previews ──
        var floorSingle = matGen.Generate(floorSeed, palette, floorMatParams);
        structGen.Apply(floorSingle, 0, 0, floorParams);
        FloorSingleTileImage.Source = BitmapHelper.ToBitmapSource(floorSingle, scale: 4);

        var floorTiles = new PixelBuffer[3, 3];
        for (int ty = 0; ty < 3; ty++)
        for (int tx = 0; tx < 3; tx++)
        {
            floorTiles[tx, ty] = matGen.Generate(floorSeed, palette, floorMatParams);
            structGen.Apply(floorTiles[tx, ty], tx, ty, floorParams);
        }
        FloorTilingPreviewImage.Source = BitmapHelper.ToTiledBitmapSource(floorTiles, scale: 2);

        // ── Cap previews ──
        GenerateCap(seed, palette, matParams, wallParams);

        sw.Stop();

        var mp  = matParams ?? MaterialParameters.FromSeed(seed);
        var wp  = wallParams;
        var fp  = floorParams;
        StatusLabel.Text =
            $"Wall:  {mp.Type}  {wp.Bond}  W={wp.BrickWidth}  H={wp.BrickHeight}  M={wp.MortarWidth:F1}\n" +
            $"Floor: {fp.Bond}  W={fp.BrickWidth}  H={fp.BrickHeight}  M={fp.MortarWidth:F1}\n" +
            $"Generated in {sw.ElapsedMilliseconds}ms";
    }

    private void GenerateCap(int seed, ColorPalette palette, MaterialParameters? matParams, StructureParameters wallParams)
    {
        var tiles = Blob47Table.CanonicalFlags
            .Select(flags =>
            {
                var buf = new PixelBuffer();
                CapContextRenderer.Apply(buf, flags, palette, matParams, wallParams, 0, 0, seed);
                return buf;
            })
            .ToList();

        int fw    = CapContextRenderer.FasciaWidth(wallParams);
        int cycle = wallParams.CapCycle;
        CapGridImage.Source = BitmapHelper.ToGridBitmapSource(tiles, columns: 4, scale: 3, gap: 2);
        CapStatusLabel.Text = $"16 cap variants · fasciaWidth={fw}px · bond={wallParams.Bond} · " +
                              $"cycle={cycle} → {16 * cycle * cycle} export tiles · 3× zoom";
    }

    private static float ParseDegrees(string? text, float fallback)
        => float.TryParse(text, out float v) ? ((v % 360f) + 360f) % 360f : fallback;

    private static float ParsePercent(string? text, float fallback)
        => float.TryParse(text, out float v) ? Math.Clamp(v, 0f, 100f) : fallback;
}
