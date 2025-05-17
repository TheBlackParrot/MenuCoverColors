using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MenuCoverColors.ColorThief;

// https://github.com/chiutse/ColorThief
// (i also let rider go ham on what it wanted to scream about)

/// <summary>
///     Defines a color in RGB space.
/// </summary>
public struct Color
{
    /// <summary>
    ///     Get or Set the Alpha component value for sRGB.
    /// </summary>
    public byte A;

    /// <summary>
    ///     Get or Set the Blue component value for sRGB.
    /// </summary>
    public byte B;

    /// <summary>
    ///     Get or Set the Green component value for sRGB.
    /// </summary>
    public byte G;

    /// <summary>
    ///     Get or Set the Red component value for sRGB.
    /// </summary>
    public byte R;

    /// <summary>
    ///     Get HSL color.
    /// </summary>
    /// <returns></returns>
    public HslColor ToHsl()
    {
        const double toDouble = 1.0 / 255;
        double r = toDouble * R;
        double g = toDouble * G;
        double b = toDouble * B;
        double max = Math.Max(Math.Max(r, g), b);
        double min = Math.Min(Math.Min(r, g), b);
        double chroma = max - min;
        double h1;

        // ReSharper disable CompareOfFloatsByEqualityOperator
        if (chroma == 0)
            h1 = 0;
        else if (max == r)
            h1 = (g - b) / chroma % 6;
        else if (max == g)
            h1 = 2 + (b - r) / chroma;
        else //if (max == b)
            h1 = 4 + (r - g) / chroma;

        double lightness = 0.5 * (max - min);
        double saturation = chroma == 0 ? 0 : chroma / (1 - Math.Abs(2 * lightness - 1));
        HslColor ret;
        ret.H = 60 * h1;
        ret.S = saturation;
        ret.L = lightness;
        ret.A = toDouble * A;
        return ret;
        // ReSharper restore CompareOfFloatsByEqualityOperator
    }
}

/// <summary>
///     Defines a color in Hue/Saturation/Lightness (HSL) space.
/// </summary>
public struct HslColor
{
    /// <summary>
    ///     The Alpha/opacity in 0..1 range.
    /// </summary>
    public double A;

    /// <summary>
    ///     The Hue in 0..360 range.
    /// </summary>
    public double H;

    /// <summary>
    ///     The Lightness in 0..1 range.
    /// </summary>
    public double L;

    /// <summary>
    ///     The Saturation in 0..1 range.
    /// </summary>
    public double S;
}

internal static class Mmcq
{
    private const int Sigbits = 5;
    public const int Rshift = 8 - Sigbits;
    public const int Mult = 1 << Rshift;
    private const int Histosize = 1 << (3 * Sigbits);
    private const int VboxLength = 1 << Sigbits;
    private const double FractByPopulation = 0.75;
    private const int MaxIterations = 1000;
    private const double WeightSaturation = 3d;
    private const double WeightLuma = 6d;
    private const double WeightPopulation = 1d;
    private static readonly VBoxComparer ComparatorProduct = new();
    private static readonly VBoxCountComparer ComparatorCount = new();

    public static int GetColorIndex(int r, int g, int b)
    {
        return (r << (2 * Sigbits)) + (g << Sigbits) + b;
    }

    /// <summary>
    ///     Gets the histo.
    /// </summary>
    /// <param name="pixels">The pixels.</param>
    /// <returns>Histo (1-d array, giving the number of pixels in each quantized region of color space), or null on error.</returns>
    private static int[] GetHisto(IEnumerable<int[]> pixels)
    {
        int[] histo = new int[Histosize];

        foreach (int[]? pixel in pixels)
        {
            int rval = pixel[0] >> Rshift;
            int gval = pixel[1] >> Rshift;
            int bval = pixel[2] >> Rshift;
            int index = GetColorIndex(rval, gval, bval);
            histo[index]++;
        }

        return histo;
    }

    private static VBox VboxFromPixels(IList<int[]> pixels, int[] histo)
    {
        int rmin = 1000000, rmax = 0;
        int gmin = 1000000, gmax = 0;
        int bmin = 1000000, bmax = 0;

        // find min/max
        int numPixels = pixels.Count;
        for (int i = 0; i < numPixels; i++)
        {
            int[]? pixel = pixels[i];
            int rval = pixel[0] >> Rshift;
            int gval = pixel[1] >> Rshift;
            int bval = pixel[2] >> Rshift;

            if (rval < rmin)
                rmin = rval;
            else if (rval > rmax) rmax = rval;

            if (gval < gmin)
                gmin = gval;
            else if (gval > gmax) gmax = gval;

            if (bval < bmin)
                bmin = bval;
            else if (bval > bmax) bmax = bval;
        }

        return new VBox(rmin, rmax, gmin, gmax, bmin, bmax, histo);
    }

    private static VBox[] DoCut(char color, VBox vbox, IList<int> partialsum, IList<int> lookaheadsum, int total)
    {
        int vboxDim1;
        int vboxDim2;

        switch (color)
        {
            case 'r':
                vboxDim1 = vbox.R1;
                vboxDim2 = vbox.R2;
                break;
            case 'g':
                vboxDim1 = vbox.G1;
                vboxDim2 = vbox.G2;
                break;
            default:
                vboxDim1 = vbox.B1;
                vboxDim2 = vbox.B2;
                break;
        }

        for (int i = vboxDim1; i <= vboxDim2; i++)
            if (partialsum[i] > total / 2)
            {
                VBox vbox1 = vbox.Clone();
                VBox vbox2 = vbox.Clone();

                int left = i - vboxDim1;
                int right = vboxDim2 - i;

                int d2 = left <= right
                    ? Math.Min(vboxDim2 - 1, Math.Abs(i + right / 2))
                    : Math.Max(vboxDim1, Math.Abs(Convert.ToInt32(i - 1 - left / 2.0)));

                // avoid 0-count boxes
                while (d2 < 0 || partialsum[d2] <= 0) d2++;
                int count2 = lookaheadsum[d2];
                while (count2 == 0 && d2 > 0 && partialsum[d2 - 1] > 0) count2 = lookaheadsum[--d2];

                // set dimensions
                switch (color)
                {
                    case 'r':
                        vbox1.R2 = d2;
                        vbox2.R1 = d2 + 1;
                        break;
                    case 'g':
                        vbox1.G2 = d2;
                        vbox2.G1 = d2 + 1;
                        break;
                    default:
                        vbox1.B2 = d2;
                        vbox2.B1 = d2 + 1;
                        break;
                }

                return [vbox1, vbox2];
            }

        throw new Exception("VBox can't be cut");
    }

    private static VBox[] MedianCutApply(IList<int> histo, VBox vbox)
    {
        if (vbox.Count(false) == 0) return null!;
        if (vbox.Count(false) == 1) return [vbox.Clone(), null!];

        // only one pixel, no split

        int rw = vbox.R2 - vbox.R1 + 1;
        int gw = vbox.G2 - vbox.G1 + 1;
        int bw = vbox.B2 - vbox.B1 + 1;
        int maxw = Math.Max(Math.Max(rw, gw), bw);

        // Find the partial sum arrays along the selected axis.
        int total = 0;
        int[] partialsum = new int[VboxLength];
        // -1 = not set / 0 = 0
        for (int l = 0; l < partialsum.Length; l++) partialsum[l] = -1;

        // -1 = not set / 0 = 0
        int[] lookaheadsum = new int[VboxLength];
        for (int l = 0; l < lookaheadsum.Length; l++) lookaheadsum[l] = -1;

        int i, j, k, sum, index;

        if (maxw == rw)
            for (i = vbox.R1; i <= vbox.R2; i++)
            {
                sum = 0;
                for (j = vbox.G1; j <= vbox.G2; j++)
                for (k = vbox.B1; k <= vbox.B2; k++)
                {
                    index = GetColorIndex(i, j, k);
                    sum += histo[index];
                }

                total += sum;
                partialsum[i] = total;
            }
        else if (maxw == gw)
            for (i = vbox.G1; i <= vbox.G2; i++)
            {
                sum = 0;
                for (j = vbox.R1; j <= vbox.R2; j++)
                for (k = vbox.B1; k <= vbox.B2; k++)
                {
                    index = GetColorIndex(j, i, k);
                    sum += histo[index];
                }

                total += sum;
                partialsum[i] = total;
            }
        else /* maxw == bw */
            for (i = vbox.B1; i <= vbox.B2; i++)
            {
                sum = 0;
                for (j = vbox.R1; j <= vbox.R2; j++)
                for (k = vbox.G1; k <= vbox.G2; k++)
                {
                    index = GetColorIndex(j, k, i);
                    sum += histo[index];
                }

                total += sum;
                partialsum[i] = total;
            }

        for (i = 0; i < VboxLength; i++)
            if (partialsum[i] != -1)
                lookaheadsum[i] = total - partialsum[i];

        // determine the cut planes
        return maxw == rw
            ? DoCut('r', vbox, partialsum, lookaheadsum, total)
            : maxw == gw
                ? DoCut('g', vbox, partialsum, lookaheadsum, total)
                : DoCut('b', vbox, partialsum, lookaheadsum, total);
    }

    /// <summary>
    ///     Inner function to do the iteration.
    /// </summary>
    /// <param name="lh">The lh.</param>
    /// <param name="comparator">The comparator.</param>
    /// <param name="target">The target.</param>
    /// <param name="histo">The histo.</param>
    /// <exception cref="System.Exception">vbox1 not defined; shouldn't happen!</exception>
    private static void Iter(List<VBox> lh, IComparer<VBox> comparator, int target, IList<int> histo)
    {
        int ncolors = 1;
        int niters = 0;

        while (niters < MaxIterations)
        {
#if PRE_V1_34_2
            // i... ok. sure. whatever
            VBox? vbox = lh[lh.Count - 1];
#else
            VBox? vbox = lh[^1];
#endif
            if (vbox.Count(false) == 0)
            {
                lh.Sort(comparator);
                niters++;
                continue;
            }

            lh.RemoveAt(lh.Count - 1);

            // do the cut
            VBox[] vboxes = MedianCutApply(histo, vbox);
            VBox vbox1 = vboxes[0];
            VBox vbox2 = vboxes[1];

            if (vbox1 == null)
                throw new Exception(
                    "vbox1 not defined; shouldn't happen!");

            lh.Add(vbox1);
            lh.Add(vbox2);
            ncolors++;

            lh.Sort(comparator);

            if (ncolors >= target) return;
            if (niters++ > MaxIterations) return;
        }
    }

    public static CMap Quantize(int[][] pixels, int maxcolors)
    {
        // short-circuit
        if (pixels.Length == 0 || maxcolors < 2 || maxcolors > 256) return null!;

        int[] histo = GetHisto(pixels);

        // get the beginning vbox from the colors
        VBox vbox = VboxFromPixels(pixels, histo);
        List<VBox> pq = [vbox];

        // Round up to have the same behaviour as in JavaScript
        int target = (int)Math.Ceiling(FractByPopulation * maxcolors);

        // first set of colors, sorted by population
        Iter(pq, ComparatorCount, target, histo);

        // Re-sort by the product of pixel occupancy times the size in color
        // space.
        pq.Sort(ComparatorProduct);

        // next set - generate the median cuts using the (npix * vol) sorting.
        Iter(pq, ComparatorProduct, maxcolors - pq.Count, histo);

        // Reverse to put the highest elements first into the color map
        pq.Reverse();

        // calculate the actual colors
        CMap cmap = new();
        foreach (VBox? vb in pq) cmap.Push(vb);

        return cmap;
    }

    public static double CreateComparisonValue(double saturation, double targetSaturation, double luma,
        double targetLuma, int population, int highestPopulation)
    {
        return WeightedMean(InvertDiff(saturation, targetSaturation), WeightSaturation,
            InvertDiff(luma, targetLuma), WeightLuma,
            population / (double)highestPopulation, WeightPopulation);
    }

    private static double WeightedMean(params double[] values)
    {
        double sum = 0;
        double sumWeight = 0;

        for (int i = 0; i < values.Length; i += 2)
        {
            double value = values[i];
            double weight = values[i + 1];

            sum += value * weight;
            sumWeight += weight;
        }

        return sum / sumWeight;
    }

    private static double InvertDiff(double value, double targetValue)
    {
        return 1 - Math.Abs(value - targetValue);
    }
}

/// <summary>
///     3D color space box.
/// </summary>
public class VBox
{
    private readonly int[] _histo;
    private int[]? _avg;
    public int B1;
    public int B2;
    private int? _count;
    public int G1;
    public int G2;
    public int R1;
    public int R2;
    private int? _volume;

    // ReSharper disable once ConvertToPrimaryConstructor
    public VBox(int r1, int r2, int g1, int g2, int b1, int b2, int[] histo)
    {
        R1 = r1;
        R2 = r2;
        G1 = g1;
        G2 = g2;
        B1 = b1;
        B2 = b2;

        _histo = histo;
    }

    public int Volume(bool force)
    {
        if (_volume == null || force) _volume = (R2 - R1 + 1) * (G2 - G1 + 1) * (B2 - B1 + 1);

        return _volume.Value;
    }

    public int Count(bool force)
    {
        if (_count == null || force)
        {
            int npix = 0;
            int i;

            for (i = R1; i <= R2; i++)
            {
                int j;
                for (j = G1; j <= G2; j++)
                {
                    int k;
                    for (k = B1; k <= B2; k++)
                    {
                        int index = Mmcq.GetColorIndex(i, j, k);
                        npix += _histo[index];
                    }
                }
            }

            _count = npix;
        }

        return _count.Value;
    }

    public VBox Clone()
    {
        return new VBox(R1, R2, G1, G2, B1, B2, _histo);
    }

    public int[] Avg(bool force)
    {
        if (_avg == null || force)
        {
            int ntot = 0;

            int rsum = 0;
            int gsum = 0;
            int bsum = 0;

            int i;

            for (i = R1; i <= R2; i++)
            {
                int j;
                for (j = G1; j <= G2; j++)
                {
                    int k;
                    for (k = B1; k <= B2; k++)
                    {
                        int histoindex = Mmcq.GetColorIndex(i, j, k);
                        int hval = _histo[histoindex];
                        ntot += hval;
                        rsum += Convert.ToInt32(hval * (i + 0.5) * Mmcq.Mult);
                        gsum += Convert.ToInt32(hval * (j + 0.5) * Mmcq.Mult);
                        bsum += Convert.ToInt32(hval * (k + 0.5) * Mmcq.Mult);
                    }
                }
            }

            if (ntot > 0)
                _avg =
                [
                    Math.Abs(rsum / ntot), Math.Abs(gsum / ntot),
                    Math.Abs(bsum / ntot)
                ];
            else
                _avg =
                [
                    Math.Abs(Mmcq.Mult * (R1 + R2 + 1) / 2),
                    Math.Abs(Mmcq.Mult * (G1 + G2 + 1) / 2),
                    Math.Abs(Mmcq.Mult * (B1 + B2 + 1) / 2)
                ];
        }

        return _avg;
    }

    public bool Contains(int[] pixel)
    {
        int rval = pixel[0] >> Mmcq.Rshift;
        int gval = pixel[1] >> Mmcq.Rshift;
        int bval = pixel[2] >> Mmcq.Rshift;

        return rval >= R1 && rval <= R2 && gval >= G1 && gval <= G2 && bval >= B1 && bval <= B2;
    }
}

internal class VBoxCountComparer : IComparer<VBox>
{
    public int Compare(VBox x, VBox y)
    {
        int a = x.Count(false);
        int b = y.Count(false);
        return a < b ? -1 : a > b ? 1 : 0;
    }
}

internal class VBoxComparer : IComparer<VBox>
{
    public int Compare(VBox x, VBox y)
    {
        int aCount = x.Count(false);
        int bCount = y.Count(false);
        int aVolume = x.Volume(false);
        int bVolume = y.Volume(false);

        // Otherwise sort by products
        int a = aCount * aVolume;
        int b = bCount * bVolume;
        return a < b ? -1 : a > b ? 1 : 0;
    }
}

/// <summary>
///     Color map
/// </summary>
public class CMap
{
    private readonly List<VBox> _vboxes = [];
    private List<QuantizedColor>? _palette;

    public void Push(VBox box)
    {
        _palette = null;
        _vboxes.Add(box);
    }

    public List<QuantizedColor> GeneratePalette()
    {
        return _palette ??= (from vBox in _vboxes
            let rgb = vBox.Avg(false)
            let color = FromRgb(rgb[0], rgb[1], rgb[2])
            select new QuantizedColor(color)).ToList();
    }

    public int Size()
    {
        return _vboxes.Count;
    }

    public int[] Map(int[] color)
    {
        foreach (VBox? vbox in _vboxes.Where(vbox => vbox.Contains(color))) return vbox.Avg(false);
        return Nearest(color);
    }

    private int[] Nearest(int[] color)
    {
        double d1 = double.MaxValue;
        int[] pColor = null!;

        foreach (VBox? t in _vboxes)
        {
            int[] vbColor = t.Avg(false);
            double d2 = Math.Sqrt(Math.Pow(color[0] - vbColor[0], 2)
                                  + Math.Pow(color[1] - vbColor[1], 2)
                                  + Math.Pow(color[2] - vbColor[2], 2));
            if (d2 < d1)
            {
                d1 = d2;
                pColor = vbColor;
            }
        }

        return pColor;
    }

    public VBox FindColor(double targetLuma, double minLuma, double maxLuma, double targetSaturation,
        double minSaturation, double maxSaturation)
    {
        VBox max = null!;
        double maxValue = 0;
        int highestPopulation = _vboxes.Select(p => p.Count(false)).Max();

        foreach (VBox? swatch in _vboxes)
        {
            int[] avg = swatch.Avg(false);
            HslColor hsl = FromRgb(avg[0], avg[1], avg[2]).ToHsl();
            double sat = hsl.S;
            double luma = hsl.L;

            if (sat >= minSaturation && sat <= maxSaturation &&
                luma >= minLuma && luma <= maxLuma)
            {
                double thisValue = Mmcq.CreateComparisonValue(sat, targetSaturation, luma, targetLuma,
                    swatch.Count(false), highestPopulation);

                if (thisValue > maxValue)
                {
                    max = swatch;
                    maxValue = thisValue;
                }
            }
        }

        return max;
    }

    private static Color FromRgb(int red, int green, int blue)
    {
        Color color = new()
        {
            A = 255,
            R = (byte)red,
            G = (byte)green,
            B = (byte)blue
        };

        return color;
    }
}

public class QuantizedColor(Color color)
{
    private Color Color { get; } = color;

    public UnityEngine.Color UnityColor
    {
        get
        {
            UnityEngine.Color color = UnityColor32;
            return color;
        }
    }

    private UnityEngine.Color UnityColor32 => new Color32(Color.R, Color.G, Color.B, Color.A);
}

public abstract class ColorThief
{
    private const int DefaultColorCount = 5;
    private const int DefaultQuality = 10;
    private const bool DefaultIgnoreWhite = true;

    /// <summary>
    ///     Use the median cut algorithm to cluster similar colors and return the base color from the largest cluster.
    /// </summary>
    /// <param name="sourceImage">The source image.</param>
    /// <param name="quality">
    ///     0 is the highest quality settings. 10 is the default. There is
    ///     a trade-off between quality and speed. The bigger the number,
    ///     the faster a color will be returned but the greater the
    ///     likelihood that it will not be the visually most dominant color.
    /// </param>
    /// <param name="ignoreWhite">if set to <c>true</c> [ignore white].</param>
    /// <returns></returns>
    public QuantizedColor GetColor(Texture2D sourceImage, int quality = DefaultQuality,
        bool ignoreWhite = DefaultIgnoreWhite)
    {
        List<QuantizedColor> palette = GetPalette(sourceImage, DefaultColorCount, quality, ignoreWhite);
        QuantizedColor? dominantColor = palette.FirstOrDefault();
        return dominantColor!;
    }

    /// <summary>
    ///     Use the median cut algorithm to cluster similar colors.
    /// </summary>
    /// <param name="sourceImage">The source image.</param>
    /// <param name="colorCount">The color count.</param>
    /// <param name="quality">
    ///     0 is the highest quality settings. 10 is the default. There is
    ///     a trade-off between quality and speed. The bigger the number,
    ///     the faster a color will be returned but the greater the
    ///     likelihood that it will not be the visually most dominant color.
    /// </param>
    /// <param name="ignoreWhite">if set to <c>true</c> [ignore white].</param>
    /// <returns></returns>
    /// <code>true</code>
    public static List<QuantizedColor> GetPalette(Texture2D sourceImage, int colorCount = DefaultColorCount,
        int quality = DefaultQuality, bool ignoreWhite = DefaultIgnoreWhite)
    {
        CMap cmap = GetColorMap(sourceImage, colorCount, quality, ignoreWhite);
        return cmap.GeneratePalette();
    }

    /// <summary>
    ///     Use the median cut algorithm to cluster similar colors.
    /// </summary>
    /// <param name="sourceImage">The source image.</param>
    /// <param name="colorCount">The color count.</param>
    /// <param name="quality">
    ///     0 is the highest quality settings. 10 is the default. There is
    ///     a trade-off between quality and speed. The bigger the number,
    ///     the faster a color will be returned but the greater the
    ///     likelihood that it will not be the visually most dominant color.
    /// </param>
    /// <param name="ignoreWhite">if set to <c>true</c> [ignore white].</param>
    /// <returns></returns>
    private static CMap GetColorMap(Texture2D sourceImage, int colorCount, int quality = DefaultQuality,
        bool ignoreWhite = DefaultIgnoreWhite)
    {
        int[][] pixelArray = GetPixelsFast(sourceImage, quality, ignoreWhite);

        // Send array to quantize function which clusters values using median
        // cut algorithm
        CMap cmap = Mmcq.Quantize(pixelArray, colorCount);
        return cmap;
    }

    private static IEnumerable<int> GetIntFromPixel(Texture2D bmp)
    {
        Color32[] clrs = bmp.GetPixels32();
        foreach (Color32 clr in clrs)
        {
            yield return clr.b;
            yield return clr.g;
            yield return clr.r;
            yield return clr.a;
        }
        // for(var x = 0; x < bmp.Width; x++)
        // {
        //     for(var y = 0; y < bmp.Height; y++)
        //     {
        //         var clr = bmp.GetPixel(x, y);
        //         yield return clr.B;
        //         yield return clr.G;
        //         yield return clr.R;
        //         yield return clr.A;
        //     }
        // }
    }

    private static int[][] GetPixelsFast(Texture2D sourceImage, int quality, bool ignoreWhite)
    {
        IEnumerable<int> imageData = GetIntFromPixel(sourceImage);
        int[] pixels = imageData.ToArray();
        int pixelCount = sourceImage.width * sourceImage.height;

        const int colorDepth = 4;

        int expectedDataLength = pixelCount * colorDepth;
        if (expectedDataLength != pixels.Length)
            throw new ArgumentException("(expectedDataLength = "
                                        + expectedDataLength + ") != (pixels.length = "
                                        + pixels.Length + ")");

        // Store the RGB values in an array format suitable for quantize
        // function

        // numRegardedPixels must be rounded up to avoid an
        // ArrayIndexOutOfBoundsException if all pixels are good.

        int numRegardedPixels = quality <= 0 ? 0 : (pixelCount + quality - 1) / quality;

        int numUsedPixels = 0;
        int[][] pixelArray = new int[numRegardedPixels][];

        for (int i = 0; i < pixelCount; i += quality)
        {
            int offset = i * 4;
            int b = pixels[offset];
            int g = pixels[offset + 1];
            int r = pixels[offset + 2];
            int a = pixels[offset + 3];

            // If pixel is mostly opaque and not white
            if (a >= 125 && !(ignoreWhite && r > 250 && g > 250 && b > 250))
            {
                pixelArray[numUsedPixels] = [r, g, b];
                numUsedPixels++;
            }
        }

        // Remove unused pixels from the array
        int[][] copy = new int[numUsedPixels][];
        Array.Copy(pixelArray, copy, numUsedPixels);
        return copy;
    }
}