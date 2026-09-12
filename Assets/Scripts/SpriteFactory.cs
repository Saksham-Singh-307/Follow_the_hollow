using System.Collections.Generic;
using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// The single place every visual in the game comes from. By default it draws placeholder
    /// shapes in code so the game runs with zero art assets. Assign sprites to GameBootstrap's
    /// AssetLibrary and this same factory serves your real art instead — ChunkBuilder and
    /// everything else that asks for a sprite never needs to know which one it got.
    /// </summary>
    public static class SpriteFactory
    {
        public const float PPU = 32f;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Material spriteMat;

        /// <summary>
        /// Assigned once by GameBootstrap from its inspector-exposed AssetLibrary. Every
        /// "Xyz Sprite()" method below checks here first and only falls back to the
        /// procedural generator when a slot is left empty, so the game still runs with
        /// zero art if nothing is assigned.
        /// </summary>
        public static AssetLibrary Assets;

        // ---------------------------------------------------------------- asset-aware lookups
        // These are what ChunkBuilder / GameBootstrap should call. Each one prefers a real
        // asset from AssetLibrary and only draws the placeholder shape as a fallback.

        public static Sprite GroundTopTile()  => Assets != null && Assets.groundTopTile  != null ? Assets.groundTopTile  : Tile(Palette.Ground, true);
        public static Sprite GroundFillTile() => Assets != null && Assets.groundFillTile != null ? Assets.groundFillTile : Tile(Palette.Ground, false);
        public static Sprite PlatformTile()   => Assets != null && Assets.platformTile   != null ? Assets.platformTile   : Tile(Palette.Platform, true);
        public static Sprite WallTile()       => Assets != null && Assets.wallTile       != null ? Assets.wallTile       : Tile(Palette.GroundEdge, false);

        public static Sprite BlockSprite()         => Assets != null && Assets.blockSprite         != null ? Assets.blockSprite         : Block(Palette.Block, false);
        public static Sprite QuestionBlockSprite() => Assets != null && Assets.questionBlockSprite  != null ? Assets.questionBlockSprite  : Block(Palette.Block, true);
        public static Sprite CamoBlockSprite()     => Assets != null && Assets.camoBlockSprite      != null ? Assets.camoBlockSprite      : Block(new Color(0.75f, 0.72f, 0.45f), true);
        public static Sprite FallingBlockSprite()  => Assets != null && Assets.fallingBlockSprite   != null ? Assets.fallingBlockSprite   : Block(Palette.Falling, false);
        public static Sprite CrusherBodySprite()   => Assets != null && Assets.crusherBodySprite    != null ? Assets.crusherBodySprite    : Block(new Color(0.45f, 0.20f, 0.26f), false);

        public static Sprite SpikesSprite()     => Assets != null && Assets.spikesSprite     != null ? Assets.spikesSprite     : Spikes(2);
        public static Sprite SpikesWideSprite() => Assets != null && Assets.spikesWideSprite != null ? Assets.spikesWideSprite
                                                  : Assets != null && Assets.spikesSprite    != null ? Assets.spikesSprite
                                                  : Spikes(3);

        public static Sprite ChomperSprite() => Assets != null && Assets.chomperSprite != null ? Assets.chomperSprite : Chomper();
        public static Sprite CoinSprite()    => Assets != null && Assets.coinSprite    != null ? Assets.coinSprite    : Coin();

        public static Sprite SpeedPowerUpSprite()        => Assets != null && Assets.speedPowerUpSprite        != null ? Assets.speedPowerUpSprite        : Bolt();
        public static Sprite HighJumpPowerUpSprite()     => Assets != null && Assets.highJumpPowerUpSprite     != null ? Assets.highJumpPowerUpSprite     : UpArrow();
        public static Sprite InvisibilityPowerUpSprite() => Assets != null && Assets.invisibilityPowerUpSprite != null ? Assets.invisibilityPowerUpSprite : GhostIcon();

        public static Sprite CatSprite(Color body) => Assets != null && Assets.catSprite    != null ? Assets.catSprite    : Cat(body);
        public static Sprite HollowSprite()        => Assets != null && Assets.hollowSprite != null ? Assets.hollowSprite : Hollow();
        public static Sprite AuraGlowSprite()      => Assets != null && Assets.auraGlowSprite != null ? Assets.auraGlowSprite : Glow();

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

        // ---------------------------------------------------------------- power-up icons
        // Authored as an 8x8 mask (row 0 = bottom) and stamped onto a 32x32 texture,
        // 4 real pixels per mask cell - easiest way to keep these readable at a glance.

        static bool Cell8(string[] rows, int x, int y)
        {
            int col = Mathf.Clamp(x / 4, 0, 7);
            int row = Mathf.Clamp(y / 4, 0, 7);
            return rows[row][col] == 'X';
        }

        static readonly string[] BoltRows =
        {
            ".XX.....",
            "..XX....",
            "....XX..",
            ".XXXXXX.",
            "..XX....",
            "...XX...",
            "....XX..",
            "..XX....",
        };

        /// <summary>Lightning bolt: the speed boost.</summary>
        public static Sprite Bolt()
        {
            return Build("bolt_powerup", 32, 32, (x, y) =>
            {
                if (!Cell8(BoltRows, x, y)) return Clear;
                bool hi = (x % 4 < 2) == (y % 4 < 2);
                return hi ? new Color(1f, 0.95f, 0.55f) : new Color(1f, 0.80f, 0.15f);
            });
        }

        static readonly string[] ArrowRows =
        {
            "..XXXX..",
            "..XXXX..",
            "..XXXX..",
            "..XXXX..",
            "XXXXXXXX",
            ".XXXXXX.",
            "..XXXX..",
            "...XX...",
        };

        /// <summary>Upward arrow: the high jump.</summary>
        public static Sprite UpArrow()
        {
            return Build("uparrow_powerup", 32, 32, (x, y) =>
            {
                if (!Cell8(ArrowRows, x, y)) return Clear;
                bool hi = (x / 4 + y / 4) % 2 == 0;
                return hi ? new Color(0.65f, 1f, 0.70f) : new Color(0.35f, 0.92f, 0.45f);
            });
        }

        static readonly string[] GhostRows =
        {
            "X.X.X.X.",
            "XXXXXXXX",
            "XXXXXXXX",
            "XXXXXXXX",
            "XXXXXXXX",
            ".XXXXXX.",
            "..XXXX..",
            "...XX...",
        };

        /// <summary>Little ghost: the invisibility power-up.</summary>
        public static Sprite GhostIcon()
        {
            return Build("ghost_powerup", 32, 32, (x, y) =>
            {
                if (!Cell8(GhostRows, x, y)) return Clear;
                int col = x / 4, row = y / 4;
                if (row == 4 && (col == 2 || col == 5)) return new Color(0.25f, 0.15f, 0.38f);
                return new Color(0.82f, 0.78f, 0.98f, 0.85f);
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
