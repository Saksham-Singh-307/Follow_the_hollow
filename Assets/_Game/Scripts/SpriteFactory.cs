using System.Collections.Generic;
using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// Generates every placeholder sprite in code so the game runs with zero art assets.
    /// Swap these out later by assigning real sprites to the renderers the builders create.
    /// </summary>
    public static class SpriteFactory
    {
        public const float PPU = 32f;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Material spriteMat;

        /// <summary>URP's 2D renderer defaults sprites to a LIT material; with no 2D lights in
        /// the scene everything renders black. We force an unlit material instead.</summary>
        public static Material SpriteMaterial
        {
            get
            {
                if (spriteMat == null)
                {
                    Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                    if (sh == null) sh = Shader.Find("Sprites/Default");
                    spriteMat = new Material(sh) { name = "VR_SpriteUnlit" };
                }
                return spriteMat;
            }
        }

        static Sprite Build(string key, int w, int h, System.Func<int, int, Color> paint)
        {
            Sprite s;
            if (cache.TryGetValue(key, out s) && s != null) return s;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "VR_" + key
            };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = paint(x, y);
            tex.SetPixels(px);
            tex.Apply();

            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect);
            s.name = key;
            cache[key] = s;
            return s;
        }

        static Color Shade(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, c.a);
        static readonly Color Clear = new Color(0, 0, 0, 0);

        public static Sprite Solid(Color c)
        {
            return Build("solid_" + ColorUtility.ToHtmlStringRGBA(c), 8, 8, (x, y) => c);
        }

        /// <summary>A 32x32 bevelled brick tile.</summary>
        public static Sprite Tile(Color c, bool grassTop = false)
        {
            string key = "tile_" + ColorUtility.ToHtmlStringRGBA(c) + (grassTop ? "_g" : "");
            return Build(key, 32, 32, (x, y) =>
            {
                bool edge = x == 0 || y == 0 || x == 31 || y == 31;
                bool light = (x + y) < 6;
                bool mortar = (y % 16 == 0) || (x % 16 == (y < 16 ? 0 : 8));
                Color baseCol = c;
                if (grassTop && y >= 24) baseCol = Color.Lerp(c, new Color(0.42f, 0.80f, 0.42f), 0.85f);
                if (edge) return Shade(baseCol, 0.55f);
                if (mortar) return Shade(baseCol, 0.80f);
                if (light) return Shade(baseCol, 1.25f);
                return baseCol;
            });
        }

        /// <summary>Floating block with a question-mark dot: the classic bait.</summary>
        public static Sprite Block(Color c, bool marked)
        {
            string key = "block_" + ColorUtility.ToHtmlStringRGBA(c) + (marked ? "_m" : "");
            return Build(key, 32, 32, (x, y) =>
            {
                bool edge = x < 2 || y < 2 || x > 29 || y > 29;
                if (edge) return Shade(c, 0.5f);
                if (marked)
                {
                    float dx = (x - 15.5f) / 7f, dy = (y - 15.5f) / 7f;
                    float d = dx * dx + dy * dy;
                    if (d < 1f && d > 0.42f) return new Color(1f, 0.94f, 0.55f);
                }
                bool hi = x + y < 12;
                return hi ? Shade(c, 1.2f) : c;
            });
        }

        /// <summary>Row of spikes pointing up.</summary>
        public static Sprite Spikes(int teeth = 2)
        {
            return Build("spikes_" + teeth, 32, 32, (x, y) =>
            {
                int w = 32 / teeth;
                int lx = x % w;
                float half = w * 0.5f;
                float need = Mathf.Abs(lx - half + 0.5f) / half; // 0 at centre, 1 at edge
                float top = (1f - need) * 30f;
                if (y > top) return Clear;
                float t = y / Mathf.Max(top, 1f);
                return Color.Lerp(new Color(0.85f, 0.88f, 0.95f), new Color(0.45f, 0.48f, 0.58f), t);
            });
        }

        /// <summary>The player: a blocky cat.</summary>
        public static Sprite Cat(Color body)
        {
            string key = "cat_" + ColorUtility.ToHtmlStringRGBA(body);
            return Build(key, 32, 32, (x, y) =>
            {
                // ears
                bool leftEar = y >= 24 && x >= 4 && x <= 11 && (y - 24) <= (11 - x) + 4;
                bool rightEar = y >= 24 && x >= 20 && x <= 27 && (y - 24) <= (x - 20) + 4;
                if (leftEar || rightEar) return Shade(body, 0.9f);

                bool head = x >= 3 && x <= 28 && y >= 2 && y <= 26;
                if (!head) return Clear;
                bool corner = (x < 5 || x > 26) && (y < 4 || y > 24);
                if (corner) return Clear;

                // eyes
                if (y >= 15 && y <= 20)
                {
                    if (x >= 8 && x <= 12) return (x >= 10 && y >= 17) ? Color.black : Color.white;
                    if (x >= 19 && x <= 23) return (x >= 21 && y >= 17) ? Color.black : Color.white;
                }
                // nose + mouth
                if (y >= 10 && y <= 12 && x >= 14 && x <= 17) return new Color(1f, 0.55f, 0.62f);
                if (y == 8 && x >= 12 && x <= 19) return Shade(body, 0.45f);
                // muzzle
                if (y >= 6 && y <= 13 && x >= 9 && x <= 22) return Shade(body, 1.25f);

                bool rim = x == 3 || x == 28 || y == 2 || y == 26;
                return rim ? Shade(body, 0.55f) : body;
            });
        }

        /// <summary>The chaser: a hollow shade with glowing eyes.</summary>
        public static Sprite Hollow()
        {
            return Build("hollow", 32, 32, (x, y) =>
            {
                float dx = (x - 15.5f) / 14f;
                float dy = (y - 14f) / 15f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                // ragged lower edge
                float wobble = Mathf.PerlinNoise(x * 0.35f, 0.5f) * 0.18f;
                if (y < 6 && Mathf.PerlinNoise(x * 0.4f, 2.3f) < 0.45f) return Clear;
                if (d > 1f - wobble) return Clear;

                if (y >= 17 && y <= 22)
                {
                    if (x >= 9 && x <= 13) return new Color(1f, 0.35f, 0.25f);
                    if (x >= 18 && x <= 22) return new Color(1f, 0.35f, 0.25f);
                }
                float t = Mathf.Clamp01(d);
                return Color.Lerp(new Color(0.10f, 0.06f, 0.14f), new Color(0.30f, 0.12f, 0.34f), t);
            });
        }

        public static Sprite Coin()
        {
            return Build("coin", 32, 32, (x, y) =>
            {
                float dx = (x - 15.5f) / 11f, dy = (y - 15.5f) / 14f;
                float d = dx * dx + dy * dy;
                if (d > 1f) return Clear;
                if (d > 0.68f) return new Color(0.80f, 0.58f, 0.10f);
                if (x >= 14 && x <= 17 && y >= 8 && y <= 23) return new Color(1f, 0.95f, 0.70f);
                return new Color(1f, 0.82f, 0.20f);
            });
        }

        /// <summary>Chomper: teeth and malice.</summary>
        public static Sprite Chomper()
        {
            return Build("chomper", 32, 32, (x, y) =>
            {
                float dx = (x - 15.5f) / 14f, dy = (y - 15.5f) / 14f;
                if (dx * dx + dy * dy > 1f) return Clear;
                bool upperTeeth = y > 19 && y < 24 && (x % 6) < 3;
                bool lowerTeeth = y > 8 && y < 13 && ((x + 3) % 6) < 3;
                if (upperTeeth || lowerTeeth) return Color.white;
                if (y >= 13 && y <= 19) return new Color(0.35f, 0.05f, 0.10f);
                return new Color(0.85f, 0.20f, 0.28f);
            });
        }

        /// <summary>Soft vertical gradient used for parallax backdrop bands.</summary>
        public static Sprite Band(Color top, Color bottom)
        {
            string key = "band_" + ColorUtility.ToHtmlStringRGBA(top) + ColorUtility.ToHtmlStringRGBA(bottom);
            return Build(key, 4, 64, (x, y) => Color.Lerp(bottom, top, y / 63f));
        }

        /// <summary>Soft radial glow used for the Hollow's aura.</summary>
        public static Sprite Glow()
        {
            return Build("glow", 64, 64, (x, y) =>
            {
                float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                return new Color(1f, 1f, 1f, a * a);
            });
        }

        public static SpriteRenderer NewRenderer(GameObject go, Sprite sprite, int order, Color? tint = null)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = SpriteMaterial;
            sr.sortingOrder = order;
            if (tint.HasValue) sr.color = tint.Value;
            return sr;
        }
    }
}
