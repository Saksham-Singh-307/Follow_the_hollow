using UnityEngine;

namespace VoiceRunner
{
    public static class Palette
    {
        public static readonly Color Ground     = new Color(0.29f, 0.24f, 0.38f);
        public static readonly Color GroundEdge = new Color(0.45f, 0.33f, 0.60f);
        public static readonly Color Block      = new Color(0.62f, 0.45f, 0.28f);
        public static readonly Color FakeBlock  = new Color(0.62f, 0.45f, 0.28f);
        public static readonly Color Falling    = new Color(0.55f, 0.38f, 0.45f);
        public static readonly Color Platform   = new Color(0.33f, 0.52f, 0.60f);
        public static readonly Color Cat        = new Color(0.98f, 0.78f, 0.34f);
        public static readonly Color Sky        = new Color(0.086f, 0.067f, 0.126f);
        public static readonly Color Far        = new Color(0.16f, 0.12f, 0.24f);
        public static readonly Color Near       = new Color(0.12f, 0.09f, 0.19f);
    }

    /// <summary>
    /// Low level geometry helpers. Everything the director places goes through here,
    /// so replacing placeholder art later means touching one file.
    /// </summary>
    public static class ChunkBuilder
    {
        public const float GroundTopY = 0f;

        static GameObject Node(Transform parent, string name, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            return go;
        }

        static GameObject Art(GameObject parent, Sprite sprite, int order, Color tint, Vector2 localPos = default)
        {
            var art = new GameObject("Art");
            art.transform.SetParent(parent.transform, false);
            art.transform.localPosition = localPos;
            SpriteFactory.NewRenderer(art, sprite, order, tint);
            return art;
        }

        /// <summary>Solid floor from xLeft, width tiles wide, surface at GroundTopY.</summary>
        public static GameObject Ground(Transform parent, float xLeft, int width, float topY = GroundTopY)
        {
            var go = Node(parent, "Ground", new Vector2(xLeft, topY));
            go.layer = VRLayers.Ground;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width + 0.08f, 3f);
            col.offset = new Vector2(width * 0.5f, -1.5f);

            var top = SpriteFactory.Tile(Palette.Ground, true);
            var fill = SpriteFactory.Tile(Palette.Ground);
            for (int i = 0; i < width; i++)
            {
                Art(go, top, -1, Color.white, new Vector2(i + 0.5f, -0.5f));
                Art(go, fill, -2, new Color(0.8f, 0.8f, 0.85f), new Vector2(i + 0.5f, -1.5f));
            }
            return go;
        }

        /// <summary>Floating platform; top surface at topY.</summary>
        public static GameObject Platform(Transform parent, float xLeft, int width, float topY)
        {
            var go = Node(parent, "Platform", new Vector2(xLeft, topY));
            go.layer = VRLayers.Ground;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width + 0.04f, 1f);
            col.offset = new Vector2(width * 0.5f, -0.5f);

            var sp = SpriteFactory.Tile(Palette.Platform, true);
            for (int i = 0; i < width; i++) Art(go, sp, -1, Color.white, new Vector2(i + 0.5f, -0.5f));
            return go;
        }

        /// <summary>Single 1x1 block centred on (cx, cy).</summary>
        public static GameObject Block(Transform parent, float cx, float cy, bool marked = false)
        {
            var go = Node(parent, "Block", new Vector2(cx, cy));
            go.layer = VRLayers.Ground;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            Art(go, SpriteFactory.Block(Palette.Block, marked), 0, Color.white);
            return go;
        }

        public static GameObject FakeBlock(Transform parent, float cx, float cy)
        {
            var go = Block(parent, cx, cy, true);
            go.name = "FakeBlock";
            go.AddComponent<VoiceRunner.FakeBlock>();
            return go;
        }

        public static GameObject InvisibleBlock(Transform parent, float cx, float cy)
        {
            var go = Node(parent, "InvisibleBlock", new Vector2(cx, cy));
            go.layer = VRLayers.Ground;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            Art(go, SpriteFactory.Block(new Color(0.75f, 0.72f, 0.45f), true), 0, Color.white);
            go.AddComponent<VoiceRunner.InvisibleBlock>();
            return go;
        }

        public static GameObject FallingBlock(Transform parent, float cx, float cy)
        {
            var go = Node(parent, "FallingBlock", new Vector2(cx, cy));
            go.layer = VRLayers.Ground;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            Art(go, SpriteFactory.Block(Palette.Falling, false), 0, Color.white);
            go.AddComponent<VoiceRunner.FallingBlock>();
            return go;
        }

        public static GameObject MovingPlatform(Transform parent, float cx, float topY, int width, float amp, float speed)
        {
            var go = Platform(parent, cx, width, topY);
            go.name = "MovingPlatform";
            var mp = go.AddComponent<VoiceRunner.MovingPlatform>();
            mp.amplitude = amp;
            mp.speed = speed;
            return go;
        }

        /// <summary>Static spikes sitting on the floor.</summary>
        public static GameObject Spikes(Transform parent, float cx, float baseY = GroundTopY)
        {
            var go = Node(parent, "Spikes", new Vector2(cx, baseY));
            go.layer = VRLayers.Hazard;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 0.6f);
            col.offset = new Vector2(0f, 0.3f);
            Art(go, SpriteFactory.Spikes(2), 1, Color.white, new Vector2(0f, 0.5f));
            return go;
        }

        /// <summary>Spikes hidden in the floor that launch as you get close.</summary>
        public static GameObject PopupSpikes(Transform parent, float cx, float baseY = GroundTopY)
        {
            var go = Node(parent, "PopupSpikes", new Vector2(cx, baseY));
            go.layer = VRLayers.Hazard;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.5f);
            col.enabled = false;
            Art(go, SpriteFactory.Spikes(2), 1, new Color(1f, 0.85f, 0.85f));
            go.AddComponent<PopupSpike>();
            return go;
        }

        public static GameObject Chomper(Transform parent, float cx, float cy, bool leaps)
        {
            var go = Node(parent, "Chomper", new Vector2(cx, cy));
            go.layer = VRLayers.Hazard;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.42f;
            Art(go, SpriteFactory.Chomper(), 1, Color.white);
            var ch = go.AddComponent<VoiceRunner.Chomper>();
            ch.leaps = leaps;
            return go;
        }

        public static GameObject FakeCoin(Transform parent, float cx, float cy)
        {
            var go = Node(parent, "FakeCoin", new Vector2(cx, cy));
            go.layer = VRLayers.Hazard;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.40f;
            Art(go, SpriteFactory.Coin(), 2, Color.white);
            var ch = go.AddComponent<VoiceRunner.Chomper>();
            ch.disguisedAsCoin = true;
            return go;
        }

        public static GameObject Coin(Transform parent, float cx, float cy)
        {
            var go = Node(parent, "Coin", new Vector2(cx, cy));
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.40f;
            Art(go, SpriteFactory.Coin(), 2, Color.white);
            go.AddComponent<VoiceRunner.Coin>();
            return go;
        }

        public static GameObject Crusher(Transform parent, float cx, float cy)
        {
            var go = Node(parent, "Crusher", new Vector2(cx, cy));
            go.layer = VRLayers.Hazard;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.95f, 0.95f);
            Art(go, SpriteFactory.Block(new Color(0.45f, 0.20f, 0.26f), false), 1, Color.white);
            Art(go, SpriteFactory.Spikes(3), 2, Color.white, new Vector2(0f, -0.75f))
                .transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            var cr = go.AddComponent<VoiceRunner.Crusher>();
            cr.armDistance = Random.Range(3.1f, 4.1f);
            return go;
        }

        /// <summary>A wall you have to clear. Bumping it costs you distance on the Hollow.</summary>
        public static GameObject Wall(Transform parent, float cx, int height)
        {
            var go = Node(parent, "Wall", new Vector2(cx, GroundTopY));
            go.layer = VRLayers.Ground;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, height);
            col.offset = new Vector2(0f, height * 0.5f);
            var sp = SpriteFactory.Tile(Palette.GroundEdge);
            for (int i = 0; i < height; i++) Art(go, sp, 0, Color.white, new Vector2(0f, i + 0.5f));
            return go;
        }
    }
}
