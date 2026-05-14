namespace TileGeneratorV2.Core.Noise;

/// <summary>
/// Divides the tile into two macro zones using a very low-frequency fBm field,
/// then samples a different noise type at its own frequency in each zone.
/// The zone boundary can be sharp (hard geological contact) or soft (gradual transition).
/// Both layers and their frequencies are independent of each other and of the macro field.
/// </summary>
public class CompositeNoise : INoiseSampler
{
    private readonly INoiseSampler _layerA;
    private readonly INoiseSampler _layerB;
    private readonly PerlinNoise   _macro;
    private readonly float _freqA;
    private readonly float _freqB;
    private readonly float _macroFreq;
    private readonly float _threshold;   // zone balance in [0,1]
    private readonly float _blendWidth;  // 0 = hard edge, >0 = soft transition band

    public CompositeNoise(
        INoiseSampler layerA, float freqA,
        INoiseSampler layerB, float freqB,
        int seed, float macroFreq, float threshold, float blendWidth)
    {
        _layerA     = layerA;     _freqA     = freqA;
        _layerB     = layerB;     _freqB     = freqB;
        _macro      = new PerlinNoise(seed ^ unchecked((int)0xC07B05EA));
        _macroFreq  = macroFreq;
        _threshold  = threshold;
        _blendWidth = blendWidth;
    }

    public float SampleTileable(float x, float y, int tileSize, float frequency = 4f)
    {
        float macro = _macro.SampleFbm(x, y, tileSize, _macroFreq, 3);

        float t;
        if (_blendWidth < 0.01f)
        {
            t = macro >= _threshold ? 1f : 0f;
        }
        else
        {
            t = Math.Clamp((macro - _threshold + _blendWidth * 0.5f) / _blendWidth, 0f, 1f);
            t = t * t * (3f - 2f * t); // smoothstep the transition band
        }

        float a = _layerA.SampleTileable(x, y, tileSize, _freqA);
        float b = _layerB.SampleTileable(x, y, tileSize, _freqB);

        return a + (b - a) * t;
    }
}
