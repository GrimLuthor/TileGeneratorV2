namespace TileGeneratorV2.Core.Noise;

public interface INoiseSampler
{
    // Returns value in [0, 1], seamlessly tileable over tileSize pixels.
    // frequency controls feature density (must be a positive integer for perfect tileability).
    float SampleTileable(float x, float y, int tileSize, float frequency = 4f);
}
