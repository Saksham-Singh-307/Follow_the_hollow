using System.Collections.Generic;
using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// The "AI" that keeps building the level. It streams chunks ahead of the cat forever,
    /// picking each one from a weighted vocabulary that opens up as difficulty rises, and
    /// it reads the run back: survive and it sharpens, die repeatedly and it eases off,
    /// get crowded by the Hollow and it stops piling on traps so you are not squeezed
    /// from both sides. Every gap it authors is clamped to a jump the cat can actually make.
    /// </summary>
    public class LevelDirector : MonoBehaviour
    {
        [Header("Streaming")]
        public float generateAhead = 48f;
        public float cleanupBehind = 30f;

        [Header("Tuning")]
        public float distanceToMaxDifficulty = 520f;
        public float skillGainPerSecond = 0.006f;
        public float mercyPerDeath = 0.09f;
        [Tooltip("Minimum number of calm/trap chunks between one power-up and the next.")]
        public int minChunksBetweenPowerUps = 5;

        [Header("Refs")]
        public VoicePlayer player;
        public ChaserHollow chaser;

        public float Difficulty { get; private set; }
        public float FrontierX => cursorX;
        public int ChunksBuilt { get; private set; }
        public string LastChunkName { get; private set; } = "-";

        struct Section { public GameObject root; public float endX; }

        readonly List<Section> live = new List<Section>();
        System.Random rng;
        float cursorX;
        float runStartX;
        float survivedTime;
        int mercyStack;
        int chunksSinceTrap;
        int chunksSincePowerUp;
        bool lastChunkWasFake;

        public void ResetLevel(int seed, float startX)
        {
            foreach (var s in live) if (s.root != null) Destroy(s.root);
            live.Clear();

            rng = new System.Random(seed);
            cursorX = startX;
            runStartX = startX;
            survivedTime = 0f;
            chunksSinceTrap = 99;
            chunksSincePowerUp = 2; // let the runway settle before the first one shows up
            lastChunkWasFake = false;
            ChunksBuilt = 0;
            Difficulty = 0f;

            // Safe runway so the first seconds teach the voice control.
            var root = NewRoot("Chunk_Runway");
            ChunkBuilder.Ground(root, cursorX, 26);
            ChunkBuilder.Coin(root, cursorX + 14f, 2.2f);
            ChunkBuilder.Coin(root, cursorX + 16f, 2.2f);
            Close(root, cursorX + 26f);
            cursorX += 26f;

            EnsureAhead(startX);
        }

        public void NotifyDeath()
        {
            mercyStack = Mathf.Min(mercyStack + 1, 4);
            survivedTime = 0f;
        }

        public void NotifyCleanRun() { mercyStack = Mathf.Max(0, mercyStack - 1); }

        void Update()
        {
            if (player == null) return;
            survivedTime += Time.deltaTime;
            EnsureAhead(player.transform.position.x);
            Cleanup(player.transform.position.x);
        }

        void EnsureAhead(float x)
        {
            int guard = 0;
            while (cursorX < x + generateAhead && guard++ < 12) BuildNext();
        }

        void Cleanup(float x)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                if (live[i].endX < x - cleanupBehind)
                {
                    if (live[i].root != null) Destroy(live[i].root);
                    live.RemoveAt(i);
                }
            }
        }

        Transform NewRoot(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        void Close(Transform root, float endX)
        {
            live.Add(new Section { root = root.gameObject, endX = endX });
        }

        // ---------------------------------------------------------------- difficulty

        void RecomputeDifficulty()
        {
            float travelled = (player != null ? player.transform.position.x : cursorX) - runStartX;
            float byDistance = Mathf.Clamp01(travelled / Mathf.Max(distanceToMaxDifficulty, 1f));
            float bySkill = Mathf.Clamp(survivedTime * skillGainPerSecond, 0f, 0.22f);
            float mercy = mercyStack * mercyPerDeath;

            float d = Mathf.Clamp01(byDistance + bySkill - mercy);

            // Being hunted is already pressure; do not stack traps on top of it.
            if (chaser != null && chaser.Pressure > 0.65f) d *= 0.75f;

            Difficulty = d;
        }

        int Rand(int minInclusive, int maxInclusive) => rng.Next(minInclusive, maxInclusive + 1);
        float Rand01() => (float)rng.NextDouble();
        bool Chance(float p) => Rand01() < p;

        /// <summary>Widest gap the cat can clear, kept honest against its real jump arc.</summary>
        int MaxGap()
        {
            float reach = 4.4f;
            if (player != null)
            {
                float g = Mathf.Abs(Physics2D.gravity.y) * player.gravityRise;
                float rise = player.jumpVelocity / g;
                float apex = player.jumpVelocity * player.jumpVelocity / (2f * g);
                float fall = Mathf.Sqrt(2f * apex / (Mathf.Abs(Physics2D.gravity.y) * player.gravityFall));
                reach = (rise + fall) * player.runSpeed;
            }
            int safe = Mathf.FloorToInt(reach * 0.78f);           // voice latency headroom
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, safe, Difficulty)), 2, Mathf.Max(2, safe));
        }

        float TrapChance()
        {
            if (chunksSinceTrap < 1) return 0f;                    // never two traps back to back
            return Mathf.Lerp(0.12f, 0.72f, Difficulty);
        }

        // ---------------------------------------------------------------- chunk picking

        void BuildNext()
        {
            RecomputeDifficulty();
            var root = NewRoot("Chunk_" + ChunksBuilt);
            float x = cursorX;
            float d = Difficulty;

            int width;
            bool trapped = Chance(TrapChance());
            if (!trapped)
            {
                if (chunksSincePowerUp >= minChunksBetweenPowerUps && Chance(0.3f))
                {
                    width = PowerUpChunk(root, x, d);
                    LastChunkName = "Power-up";
                    chunksSincePowerUp = 0;
                }
                else
                {
                    width = Chance(0.45f) ? Flat(root, x, d) : Gap(root, x, d);
                    LastChunkName = "Calm";
                    chunksSincePowerUp++;
                }
                chunksSinceTrap++;
            }
            else
            {
                width = PickTrapChunk(root, x, d);
                chunksSinceTrap = 0;
                chunksSincePowerUp++;
            }

            cursorX = x + width;
            Close(root, cursorX);
            ChunksBuilt++;
        }

        int PickTrapChunk(Transform root, float x, float d)
        {
            // Weighted menu; entries unlock as difficulty climbs.
            float roll = Rand01();
            var options = new List<(string name, float weight, System.Func<Transform, float, float, int> build)>
            {
                ("Spike floor",     1.0f,                       SpikeFloor),
                ("Wall stall",      0.7f,                       WallStall),
                ("Stairs",          0.6f,                       Stairs),
                ("Platform hop",    d > 0.10f ? 1.0f : 0f,      PlatformHop),
                ("Coin bait",       d > 0.15f ? 0.8f : 0f,      CoinBait),
                ("Fake blocks",     d > 0.22f ? 1.1f : 0f,      FakeBlockBait),
                ("Chompers",        d > 0.28f ? 0.9f : 0f,      ChomperNest),
                ("Moving ground",   d > 0.34f ? 0.8f : 0f,      Movers),
                ("Falling road",    d > 0.42f ? 1.0f : 0f,      FallingRoad),
                ("Invisible bonk",  d > 0.48f ? 1.0f : 0f,      InvisibleBonk),
                ("Crusher hall",    d > 0.55f ? 0.9f : 0f,      CrusherHall),
                ("Gauntlet",        d > 0.72f ? 1.0f : 0f,      Gauntlet),
            };

            float total = 0f;
            foreach (var o in options) total += o.weight;
            float pick = roll * total, acc = 0f;
            foreach (var o in options)
            {
                acc += o.weight;
                if (pick <= acc && o.weight > 0f)
                {
                    LastChunkName = o.name;
                    return o.build(root, x, d);
                }
            }
            LastChunkName = "Spike floor";
            return SpikeFloor(root, x, d);
        }

        // ---------------------------------------------------------------- chunks

        int Flat(Transform root, float x, float d)
        {
            int w = Rand(6, 10);
            ChunkBuilder.Ground(root, x, w);
            if (Chance(0.5f))
            {
                int n = Rand(2, 4);
                for (int i = 0; i < n; i++) ChunkBuilder.Coin(root, x + 2.5f + i, 2.2f);
            }
            return w;
        }

        /// <summary>A calm stretch built around a single power-up pickup.</summary>
        int PowerUpChunk(Transform root, float x, float d)
        {
            int w = Rand(8, 11);
            ChunkBuilder.Ground(root, x, w);

            var kind = (PowerUpKind)Rand(0, 2);
            float cx = x + w * 0.5f;
            ChunkBuilder.PowerUp(root, cx, 1.6f, kind);

            // A couple of flanking coins so it still feels part of the coin trail.
            if (Chance(0.5f)) ChunkBuilder.Coin(root, cx - 2.2f, 1.6f);
            if (Chance(0.5f)) ChunkBuilder.Coin(root, cx + 2.2f, 1.6f);
            return w;
        }

        int Gap(Transform root, float x, float d)
        {
            int pre = Rand(3, 5);
            int gap = Rand(2, MaxGap());
            int post = Rand(4, 6);
            ChunkBuilder.Ground(root, x, pre);
            ChunkBuilder.Ground(root, x + pre + gap, post);
            if (Chance(0.6f))
                for (int i = 0; i < gap; i++)
                    ChunkBuilder.Coin(root, x + pre + i + 0.5f, 2.6f);
            return pre + gap + post;
        }

        int Stairs(Transform root, float x, float d)
        {
            int steps = Rand(3, 4);
            bool up = Chance(0.6f);
            int w = 0;
            ChunkBuilder.Ground(root, x, 3); w += 3;
            for (int i = 0; i < steps; i++)
            {
                float h = up ? (i + 1) : (steps - i);
                ChunkBuilder.Platform(root, x + w, 2, h);
                w += 2;
            }
            ChunkBuilder.Ground(root, x + w, 4); w += 4;
            return w;
        }

        int SpikeFloor(Transform root, float x, float d)
        {
            int w = Rand(8, 11);
            ChunkBuilder.Ground(root, x, w);
            int count = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 3f, d)), 1, 3);
            int at = 3;
            for (int i = 0; i < count && at < w - 3; i++)
            {
                if (Chance(0.6f)) ChunkBuilder.PopupSpikes(root, x + at + 0.5f);
                else ChunkBuilder.Spikes(root, x + at + 0.5f);
                at += Rand(2, 4);
            }
            return w;
        }

        int WallStall(Transform root, float x, float d)
        {
            int w = Rand(8, 10);
            ChunkBuilder.Ground(root, x, w);
            int h = d > 0.5f && Chance(0.45f) ? 3 : 2;   // 3 tall forces the double jump
            ChunkBuilder.Wall(root, x + 4.5f, h);
            if (h == 3) ChunkBuilder.Coin(root, x + 4.5f, 4.6f);
            return w;
        }

        int PlatformHop(Transform root, float x, float d)
        {
            int pre = 3;
            ChunkBuilder.Ground(root, x, pre);
            int hops = Rand(2, 4);
            float cx = x + pre;
            for (int i = 0; i < hops; i++)
            {
                int gap = Rand(2, Mathf.Max(2, MaxGap() - 1));
                cx += gap;
                int pw = Rand(2, 3);
                float h = Rand(1, 3);
                bool fake = d > 0.35f && !lastChunkWasFake && i > 0 && Chance(0.25f);
                if (fake)
                {
                    for (int k = 0; k < pw; k++) ChunkBuilder.FakeBlock(root, cx + k + 0.5f, h - 0.5f);
                    lastChunkWasFake = true;
                }
                else
                {
                    ChunkBuilder.Platform(root, cx, pw, h);
                    lastChunkWasFake = false;
                }
                cx += pw;
            }
            cx += Rand(2, Mathf.Max(2, MaxGap() - 1));
            ChunkBuilder.Ground(root, cx, 5);
            return Mathf.RoundToInt(cx + 5 - x);
        }

        int FakeBlockBait(Transform root, float x, float d)
        {
            int pre = Rand(3, 4);
            int gap = Rand(3, MaxGap() + 1);
            int post = Rand(4, 6);
            ChunkBuilder.Ground(root, x, pre);
            ChunkBuilder.Ground(root, x + pre + gap, post);

            // Stepping stones over the pit: at least one of them is a lie.
            int fakeIndex = Rand(0, gap - 1);
            for (int i = 0; i < gap; i++)
            {
                float bx = x + pre + i + 0.5f;
                if (i == fakeIndex) ChunkBuilder.FakeBlock(root, bx, 1.5f);
                else ChunkBuilder.Block(root, bx, 1.5f, Chance(0.5f));
            }
            lastChunkWasFake = true;
            return pre + gap + post;
        }

        int CoinBait(Transform root, float x, float d)
        {
            int w = Rand(9, 12);
            ChunkBuilder.Ground(root, x, w);
            int n = Rand(3, 5);
            int poison = Rand(1, n - 1);
            for (int i = 0; i < n; i++)
            {
                float cx = x + 3f + i * 1.4f;
                if (i == poison) ChunkBuilder.FakeCoin(root, cx, 1.6f);
                else ChunkBuilder.Coin(root, cx, 1.6f);
            }
            return w;
        }

        int ChomperNest(Transform root, float x, float d)
        {
            int w = Rand(9, 12);
            ChunkBuilder.Ground(root, x, w);
            int n = d > 0.6f ? 2 : 1;
            for (int i = 0; i < n; i++)
                ChunkBuilder.Chomper(root, x + 4f + i * 3.5f, 0.5f, d > 0.5f && Chance(0.6f));
            if (Chance(0.5f)) ChunkBuilder.Platform(root, x + 3f, 2, 3f);
            return w;
        }

        int Movers(Transform root, float x, float d)
        {
            int pre = 3;
            ChunkBuilder.Ground(root, x, pre);
            float cx = x + pre + Rand(2, 3);
            int n = Rand(1, 2);
            for (int i = 0; i < n; i++)
            {
                ChunkBuilder.MovingPlatform(root, cx, 2.5f, 3, Mathf.Lerp(1.0f, 2.0f, d), Mathf.Lerp(1.0f, 1.9f, d));
                cx += 3 + Rand(2, Mathf.Max(2, MaxGap() - 1));
            }
            ChunkBuilder.Ground(root, cx, 5);
            return Mathf.RoundToInt(cx + 5 - x);
        }

        int FallingRoad(Transform root, float x, float d)
        {
            int pre = 3;
            ChunkBuilder.Ground(root, x, pre);
            int span = Rand(5, 8);
            for (int i = 0; i < span; i++)
                ChunkBuilder.FallingBlock(root, x + pre + i + 0.5f, -0.5f);
            float endX = x + pre + span;
            ChunkBuilder.Ground(root, endX, 5);
            return Mathf.RoundToInt(endX + 5 - x);
        }

        int InvisibleBonk(Transform root, float x, float d)
        {
            int pre = Rand(3, 4);
            int gap = Rand(3, MaxGap());
            int post = Rand(5, 6);
            ChunkBuilder.Ground(root, x, pre);
            ChunkBuilder.Ground(root, x + pre + gap, post);
            // Sits exactly where the jump arc peaks, right over the pit.
            ChunkBuilder.InvisibleBlock(root, x + pre + gap * 0.5f, 3.4f);
            if (Chance(0.5f)) ChunkBuilder.Coin(root, x + pre + gap * 0.5f, 2.2f);
            return pre + gap + post;
        }

        int CrusherHall(Transform root, float x, float d)
        {
            int w = Rand(10, 13);
            ChunkBuilder.Ground(root, x, w);
            int n = Rand(2, 3);
            for (int i = 0; i < n; i++)
                ChunkBuilder.Crusher(root, x + 3.5f + i * 3f, 4.5f);
            if (Chance(0.5f)) ChunkBuilder.PopupSpikes(root, x + w - 3.5f);
            return w;
        }

        int Gauntlet(Transform root, float x, float d)
        {
            int w = 0;
            w += SpikeFloor(root, x, d);
            w += FakeBlockBait(root, x + w, d);
            ChunkBuilder.Crusher(root, x + w - 4f, 4.5f);
            w += Flat(root, x + w, d);
            return w;
        }
    }
}
