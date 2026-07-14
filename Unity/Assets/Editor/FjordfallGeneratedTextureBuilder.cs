#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fjordfall.EditorTools
{
    /// <summary>
    /// Deterministically regenerates the bundled 256px texture set when PNG files are missing.
    /// The manual ZIP already includes the PNGs; this mainly keeps Git/API checkouts reproducible.
    /// </summary>
    [InitializeOnLoad]
    public static class FjordfallGeneratedTextureBuilder
    {
        private const int Size = 256;
        private const string TargetDir = "Assets/Resources/External/Textures";

        private enum Pattern { Noise, Grass, Stone, Plaster, Roof, Wood, Water, Cloth }

        static FjordfallGeneratedTextureBuilder()
        {
            EditorApplication.delayCall += EnsureTextures;
        }

        [MenuItem("Fjordfall/4. 외부 텍스처 재생성", priority = 4)]
        public static void EnsureTextures()
        {
            Directory.CreateDirectory(TargetDir);
            bool wrote = false;
            wrote |= Make("grass_moss", C(77, 101, 98), C(125, 143, 126), 11, Pattern.Grass, true);
            wrote |= Make("cliff_stone", C(166, 174, 163), C(205, 209, 194), 12, Pattern.Stone, true);
            wrote |= Make("village_plaster", C(206, 207, 196), C(236, 232, 217), 13, Pattern.Plaster, true);
            wrote |= Make("roof_shingle", C(50, 61, 63), C(91, 101, 98), 14, Pattern.Roof, true);
            wrote |= Make("dark_bark", C(47, 34, 35), C(91, 67, 56), 15, Pattern.Wood, true);
            wrote |= Make("fjord_water", C(122, 151, 149), C(182, 201, 193), 16, Pattern.Water, true);
            wrote |= Make("dark_foliage", C(38, 34, 43), C(73, 69, 76), 17, Pattern.Noise, false);
            wrote |= Make("linen_sail", C(173, 158, 144), C(222, 211, 194), 18, Pattern.Cloth, false);
            wrote |= Make("iron_worn", C(76, 82, 84), C(145, 151, 148), 19, Pattern.Stone, false);
            if (wrote) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static Color32 C(byte r, byte g, byte b) => new Color32(r, g, b, 255);

        private static bool Make(string name, Color32 a, Color32 b, int seed, Pattern pattern, bool normal)
        {
            string path = Path.Combine(TargetDir, name + ".png");
            Texture2D texture = null;
            bool wrote = false;
            if (!File.Exists(path))
            {
                texture = Build(a, b, seed, pattern);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                wrote = true;
                Debug.Log("[Fjordfall] Generated external texture: " + path);
            }
            if (normal)
            {
                string normalPath = Path.Combine(TargetDir, name + "_normal.png");
                if (!File.Exists(normalPath))
                {
                    if (texture == null)
                    {
                        byte[] bytes = File.ReadAllBytes(path);
                        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        texture.LoadImage(bytes);
                    }
                    Texture2D normalTexture = BuildNormal(texture, name == "cliff_stone" ? 4f : 2.4f);
                    File.WriteAllBytes(normalPath, normalTexture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(normalTexture);
                    wrote = true;
                }
            }
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            return wrote;
        }

        private static Texture2D Build(Color32 a, Color32 b, int seed, Pattern pattern)
        {
            Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            Color32[] pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float n = Fractal(x, y, seed);
                float t = Mathf.Clamp01((n - .28f) / .55f);
                Color c = Color.Lerp(a, b, t);
                c *= PatternShade(x, y, seed, pattern);
                pixels[y * Size + x] = c;
            }
            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            return tex;
        }

        private static float PatternShade(int x, int y, int seed, Pattern p)
        {
            switch (p)
            {
                case Pattern.Grass:
                    return ((x * 17 + y * 31 + seed) % 37 < 4 && y % 7 < 5) ? .72f : 1f;
                case Pattern.Stone:
                    return Mathf.Abs(Mathf.Sin(x * .035f + Mathf.Sin(y * .07f))) > .94f ? .72f : 1f;
                case Pattern.Plaster:
                    return Hash(x / 3, y / 3, seed) > .94f ? .68f : 1f;
                case Pattern.Roof:
                    return y % 24 < 2 || ((x + (y / 24 % 2) * 12) % 24 < 2) ? .68f : 1f;
                case Pattern.Wood:
                    return y % 32 < 2 || Mathf.Abs(Mathf.Sin(x * .04f + Mathf.Sin(y * .09f) * 2f)) > .97f ? .70f : 1f;
                case Pattern.Water:
                    return Mathf.Abs(Mathf.Sin(x * .065f + Mathf.Sin(y * .09f) * 1.5f)) > .92f ? 1.18f : 1f;
                case Pattern.Cloth:
                    return x % 8 == 0 || y % 8 == 0 ? .88f : 1f;
                default:
                    return 1f;
            }
        }

        private static float Fractal(int x, int y, int seed)
        {
            float value = 0f, weight = 0f, scale = 1f;
            for (int octave = 0; octave < 5; octave++)
            {
                int cell = 4 << octave;
                value += SmoothNoise(x / (float)cell, y / (float)cell, seed + octave * 101) * scale;
                weight += scale;
                scale *= .5f;
            }
            return value / weight;
        }

        private static float SmoothNoise(float x, float y, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float tx = Mathf.SmoothStep(0f, 1f, x - x0), ty = Mathf.SmoothStep(0f, 1f, y - y0);
            float a = Hash(x0, y0, seed), b = Hash(x0 + 1, y0, seed);
            float c = Hash(x0, y0 + 1, seed), d = Hash(x0 + 1, y0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 69069);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h & 0x00FFFFFF) / 16777215f;
            }
        }

        private static Texture2D BuildNormal(Texture2D source, float strength)
        {
            Texture2D normal = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true, true);
            Color[] output = new Color[source.width * source.height];
            for (int y = 0; y < source.height; y++)
            for (int x = 0; x < source.width; x++)
            {
                float l = source.GetPixel((x - 1 + source.width) % source.width, y).grayscale;
                float r = source.GetPixel((x + 1) % source.width, y).grayscale;
                float d = source.GetPixel(x, (y - 1 + source.height) % source.height).grayscale;
                float u = source.GetPixel(x, (y + 1) % source.height).grayscale;
                Vector3 n = new Vector3((l - r) * strength, (d - u) * strength, 1f).normalized;
                output[y * source.width + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1f);
            }
            normal.SetPixels(output);
            normal.Apply(true, false);
            return normal;
        }
    }
}
#endif
