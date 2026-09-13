using System.Collections.Generic;
using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// Deliberately dependency-free HUD (no TMP essentials, no Canvas prefab) so the game
    /// runs the moment you press Play. Replace with uGUI/UI Toolkit whenever you like -
    /// everything it shows is readable off GameManager / MicVoiceInput.
    /// </summary>
    public class RunnerHUD : MonoBehaviour
    {
        public GameManager game;
        public MicVoiceInput voice;
        public LevelDirector director;
        public ChaserHollow chaser;
        public bool showDirectorDebug = true;

        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
        GUIStyle label, big, small, centerBig, coinCountStyle, popStyle;
        float uiScale;

        // ---- coin badge state ---------------------------------------------
        int lastCoinCount = -1;
        float coinBumpTimer;
        const float coinBumpDuration = 0.32f;

        static Texture2D Tex(Color c)
        {
            string k = ColorUtility.ToHtmlStringRGBA(c);
            Texture2D t;
            if (texCache.TryGetValue(k, out t) && t != null) return t;
            t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            t.hideFlags = HideFlags.HideAndDontSave;
            texCache[k] = t;
            return t;
        }

        void EnsureStyles()
        {
            uiScale = Mathf.Max(1f, Screen.height / 720f);
            if (label != null) { }
            label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(16 * uiScale), fontStyle = FontStyle.Bold };
            small = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(12 * uiScale) };
            big = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(30 * uiScale), fontStyle = FontStyle.Bold };
            centerBig = new GUIStyle(big) { alignment = TextAnchor.MiddleCenter };
            coinCountStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(19 * uiScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            popStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(13 * uiScale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            label.normal.textColor = Color.white;
            small.normal.textColor = new Color(1f, 1f, 1f, 0.65f);
            big.normal.textColor = Color.white;
            centerBig.normal.textColor = Color.white;
            coinCountStyle.normal.textColor = new Color(1f, 0.92f, 0.55f);
        }

        /// <summary>Icon + live count, in its own pill so it reads as a real HUD stat rather
        /// than buried text. Pulses and pops a "+1" for a moment whenever a coin is picked up.</summary>
        void DrawCoinBadge(float x, float y, float s)
        {
            float h = 30 * s;
            float w = 108 * s;
            var badge = new Rect(x, y, w, h);

            float pulse = coinBumpDuration > 0f ? coinBumpTimer / coinBumpDuration : 0f; // 1 -> 0
            var prevMatrix = GUI.matrix;
            if (pulse > 0f)
            {
                float pulseMag = 0.20f + 0.04f * Mathf.Min(game.LastCoinGain - 1, 4);
                GUIUtility.ScaleAroundPivot(Vector2.one * (1f + pulseMag * pulse), badge.center);
            }

            Box(badge, new Color(0f, 0f, 0f, 0.35f));
            Box(new Rect(badge.x + 1.5f * s, badge.y + 1.5f * s, badge.width - 3f * s, badge.height - 3f * s),
                new Color(1f, 0.85f, 0.25f, 0.08f));

            float iconSize = badge.height - 6 * s;
            var iconRect = new Rect(badge.x + 4 * s, badge.y + 3 * s, iconSize, iconSize);
            DrawSprite(iconRect, SpriteFactory.CoinSprite(), Color.white);

            var countRect = new Rect(iconRect.xMax + 6 * s, badge.y, badge.width - iconSize - 14 * s, badge.height);
            GUI.Label(countRect, game.Coins.ToString(), coinCountStyle);

            // ---- combo tag, overlapping the badge's top-right corner like a notification pip
            if (game.ComboMultiplier > 1)
            {
                Color comboColor = ComboColor(game.ComboMultiplier);
                float tagW = 30 * s, tagH = 17 * s;
                var tagRect = new Rect(badge.xMax - tagW * 0.55f, badge.y - tagH * 0.55f, tagW, tagH);
                Box(tagRect, new Color(comboColor.r * 0.3f, comboColor.g * 0.3f, comboColor.b * 0.3f, 0.92f));
                var tagStyle = new GUIStyle(popStyle) { fontSize = Mathf.RoundToInt(12 * s) };
                tagStyle.normal.textColor = comboColor;
                GUI.Label(tagRect, "x" + game.ComboMultiplier, tagStyle);
            }

            GUI.matrix = prevMatrix;

            // ---- streak countdown: how long you've got left to keep the combo alive
            if (game.ComboWindowRemaining01 > 0f)
            {
                float barY = badge.yMax + 3 * s;
                Box(new Rect(badge.x, barY, badge.width, 3 * s), new Color(1f, 1f, 1f, 0.10f));
                Box(new Rect(badge.x, barY, badge.width * game.ComboWindowRemaining01, 3 * s),
                    ComboColor(game.ComboMultiplier));
            }

            if (pulse > 0f)
            {
                var c = popStyle.normal.textColor;
                popStyle.normal.textColor = new Color(1f, 0.85f, 0.25f, pulse);
                GUI.Label(new Rect(badge.x, badge.y - (16 + 10 * (1f - pulse)) * s, badge.width, 18 * s),
                    "+" + game.LastCoinGain, popStyle);
                popStyle.normal.textColor = c;
            }

            GUI.Label(new Rect(badge.xMax + 8 * s, y, 120 * s, h), string.Format("best {0}", game.BestCoins), small);
        }

        /// <summary>Cool yellow at x1, ramping to a hot red as the streak climbs toward the cap.</summary>
        static Color ComboColor(int multiplier)
        {
            float t = Mathf.InverseLerp(1f, 5f, multiplier);
            return Color.Lerp(new Color(1f, 0.92f, 0.55f), new Color(1f, 0.35f, 0.15f), t);
        }

        void Box(Rect r, Color c) { GUI.DrawTexture(r, Tex(c)); }

        /// <summary>Draws a Sprite's own texture region (safe even if it's packed into an atlas).</summary>
        static void DrawSprite(Rect r, Sprite spr, Color tint)
        {
            if (spr == null || spr.texture == null) return;
            var tr = spr.textureRect;
            var tex = spr.texture;
            var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            var prev = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(r, tex, uv);
            GUI.color = prev;
        }

        void Update()
        {
            // Detected here (once per frame) rather than in OnGUI, which can run multiple
            // passes per frame and would otherwise re-trigger the bump animation.
            if (game == null) return;
            if (coinBumpTimer > 0f) coinBumpTimer = Mathf.Max(0f, coinBumpTimer - Time.deltaTime);

            if (lastCoinCount < 0) lastCoinCount = game.Coins;
            else if (game.Coins > lastCoinCount) coinBumpTimer = coinBumpDuration;
            lastCoinCount = game.Coins;
        }

        void OnGUI()
        {
            if (game == null) return;
            EnsureStyles();
            float s = uiScale;
            float pad = 14f * s;

            // ---- stats -------------------------------------------------------
            GUI.Label(new Rect(pad, pad, 420 * s, 40 * s),
                string.Format("{0:0} m", game.Distance), big);
            GUI.Label(new Rect(pad, pad + 34 * s, 420 * s, 24 * s),
                string.Format("best {0:0} m", game.BestDistance), small);

            DrawCoinBadge(pad, pad + 34 * s + 22 * s, s);

            // ---- mic meter ---------------------------------------------------
            float mw = 240 * s, mh = 16 * s;
            float mx = (Screen.width - mw) * 0.5f, my = pad;
            Box(new Rect(mx - 2, my - 2, mw + 4, mh + 4), new Color(0f, 0f, 0f, 0.45f));
            Box(new Rect(mx, my, mw, mh), new Color(1f, 1f, 1f, 0.10f));

            float lvl = voice != null ? Mathf.Clamp01(voice.level) : 0f;
            bool hot = voice != null && lvl >= voice.triggerLevel;
            Box(new Rect(mx, my, mw * lvl, mh),
                hot ? new Color(0.45f, 1f, 0.55f, 0.95f) : new Color(0.45f, 0.75f, 1f, 0.8f));

            if (voice != null)
            {
                float tx = mx + mw * voice.triggerLevel;
                Box(new Rect(tx - 1 * s, my - 3 * s, 2 * s, mh + 6 * s), new Color(1f, 0.9f, 0.3f, 0.95f));
            }
            string micLine = voice == null ? "no voice input"
                : (voice.micReady ? "MIC: " + voice.micStatus : voice.micStatus);
            var c = GUI.color;
            GUI.Label(new Rect(mx, my + mh + 2 * s, mw, 20 * s), micLine, small);
            GUI.color = c;

            // ---- active power-up -----------------------------------------------
            var vp = game.player;
            if (vp != null && (vp.IsSpeedBoosted || vp.IsHighJumping || vp.IsInvisible))
            {
                float lw = mw, lh = 10 * s;
                float lx = mx, ly = my + mh + 24 * s;

                string name; float timeLeft, duration; Color barColor;
                if (vp.IsInvisible)
                {
                    name = "INVISIBLE"; timeLeft = vp.InvisibleTimeLeft; duration = vp.InvisibleDuration;
                    barColor = new Color(0.6f, 0.5f, 1f);
                }
                else if (vp.IsSpeedBoosted)
                {
                    name = "SPEED BOOST"; timeLeft = vp.SpeedBoostTimeLeft; duration = vp.SpeedBoostDuration;
                    barColor = new Color(1f, 0.82f, 0.15f);
                }
                else
                {
                    name = "HIGH JUMP"; timeLeft = vp.HighJumpTimeLeft; duration = vp.HighJumpDuration;
                    barColor = new Color(0.45f, 1f, 0.55f);
                }

                GUI.Label(new Rect(lx, ly - 16 * s, lw, 16 * s),
                    string.Format("{0}  {1:0.0}s", name, timeLeft), small);
                Box(new Rect(lx, ly, lw, lh), new Color(1f, 1f, 1f, 0.10f));
                Box(new Rect(lx, ly, lw * Mathf.Clamp01(timeLeft / Mathf.Max(duration, 0.01f)), lh), barColor);
            }

            // ---- hollow proximity -------------------------------------------
            if (chaser != null)
            {
                float pw = 200 * s, ph = 10 * s;
                float px = Screen.width - pw - pad, py = pad + 4 * s;
                GUI.Label(new Rect(px, py - 18 * s, pw, 20 * s), "THE HOLLOW", small);
                Box(new Rect(px, py, pw, ph), new Color(1f, 1f, 1f, 0.10f));
                float p = Mathf.Clamp01(chaser.Pressure);
                Box(new Rect(px, py, pw * p, ph), Color.Lerp(new Color(0.5f, 0.3f, 0.8f), new Color(1f, 0.25f, 0.25f), p));
            }

            if (showDirectorDebug && director != null)
            {
                GUI.Label(new Rect(Screen.width - 220 * s - pad, pad + 34 * s, 220 * s, 20 * s),
                    string.Format("difficulty {0:0.00}  |  {1}", director.Difficulty, director.LastChunkName), small);
            }

            // ---- state overlays ---------------------------------------------
            switch (game.State)
            {
                case GameState.Calibrating:
                    Center("LISTENING TO THE ROOM...", "stay quiet for a second", s);
                    break;
                case GameState.Ready:
                    Center("SHOUT TO RUN",
                        "voice = jump   |   shout again mid-air = double jump   |   grab power-ups: speed, invisibility, high jump", s);
                    break;
                case GameState.Dead:
                    Box(new Rect(0, 0, Screen.width, Screen.height), new Color(0.05f, 0.02f, 0.08f, 0.55f));
                    Center(game.CauseText(),
                        string.Format("{0:0} m  -  shout or press SPACE to run again", game.Distance), s);
                    break;
            }
        }

        void Center(string title, string sub, float s)
        {
            float w = Screen.width, h = Screen.height;
            GUI.Label(new Rect(0, h * 0.40f, w, 50 * s), title, centerBig);
            var st = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(14 * s) };
            st.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(0, h * 0.40f + 44 * s, w, 30 * s), sub, st);
        }
    }
}
