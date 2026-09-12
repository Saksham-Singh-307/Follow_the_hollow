using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace VoiceRunner
{
    /// <summary>
    /// Builds the whole game at runtime: camera, backdrop, mic, cat, Hollow, director, HUD.
    /// The scene only has to contain this one object, which keeps the project free of
    /// hand-authored prefabs while you iterate.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Feel")]
        public float runSpeed = 7.0f;
        public float jumpVelocity = 14.0f;
        public float doubleJumpVelocity = 12.5f;
        public float cameraSize = 6.2f;

        [Header("Voice")]
        [Range(0.15f, 0.9f)] public float voiceTriggerLevel = 0.45f;

        [Header("Art (optional — leave slots empty to keep the procedural placeholder shapes)")]
        public AssetLibrary assets = new AssetLibrary();

        [Header("Sound (optional — leave slots empty to keep the game silent, just like art)")]
        public AudioLibrary sounds = new AudioLibrary();

        [Header("Forward Enemies")]
        public bool forwardEnemiesEnabled = true;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Physics2D.queriesHitTriggers = false;

            // Must happen before anything below asks SpriteFactory for a sprite.
            SpriteFactory.Assets = assets;

            // Same idea for audio: assign before anything below could try to play a sound.
            Sfx.Assets = sounds;
            Sfx.PlayMusic(sounds.backgroundMusic, sounds.musicVolume);

            var camGo = BuildCamera();
            var camFollow = camGo.GetComponent<RunnerCamera>();

            BuildBackdrop(camGo.transform, -20, Palette.Far, 0.25f, 6f, 9f, 26f);
            BuildBackdrop(camGo.transform, -12, Palette.Near, 0.45f, 3.5f, 6f, 18f);

            var voice = new GameObject("VoiceInput").AddComponent<MicVoiceInput>();
            voice.triggerLevel = voiceTriggerLevel;

            var player = BuildPlayer();

            var chaser = BuildChaser();

            var levelGo = new GameObject("Level");
            var director = levelGo.AddComponent<LevelDirector>();

            var game = gameObject.AddComponent<GameManager>();
            game.player = player;
            game.chaser = chaser;
            game.director = director;
            game.voice = voice;
            game.cam = camFollow;
            game.spawn = new Vector3(2f, 2.5f, 0f);

            player.game = game;
            player.Bind(voice);
            chaser.player = player;
            chaser.game = game;
            director.player = player;
            director.chaser = chaser;
            camFollow.target = player.transform;

            if (forwardEnemiesEnabled)
            {
                var enemies = new GameObject("ForwardEnemies").AddComponent<ForwardEnemySpawner>();
                enemies.player = player.transform;
                enemies.game = game;
                enemies.director = director;
            }

            var hud = gameObject.AddComponent<RunnerHUD>();
            hud.game = game;
            hud.voice = voice;
            hud.director = director;
            hud.chaser = chaser;

            // Stand somewhere sane until the first run starts.
            director.ResetLevel(Random.Range(1, 99999), -6f);
            player.ResetForRun(game.spawn);
            chaser.ResetForRun(game.spawn.x, game.spawn.y);
            camFollow.Snap();
        }

        GameObject BuildCamera()
        {
            Camera cam = Camera.main;
            GameObject go;
            if (cam != null)
            {
                go = cam.gameObject;
            }
            else
            {
                go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }

            go.transform.position = new Vector3(0f, 1.5f, -10f);
            go.transform.rotation = Quaternion.identity;

            cam.orthographic = true;
            cam.orthographicSize = cameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Sky;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;

            var urp = go.GetComponent<UniversalAdditionalCameraData>();
            if (urp == null) urp = go.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = false;

            if (go.GetComponent<AudioListener>() == null) go.AddComponent<AudioListener>();

            var follow = go.GetComponent<RunnerCamera>();
            if (follow == null) follow = go.AddComponent<RunnerCamera>();
            return go;
        }

        void BuildBackdrop(Transform cam, int order, Color color, float factor,
                           float minH, float maxH, float tileWidth)
        {
            var layer = new GameObject("Backdrop_" + order);
            var px = layer.AddComponent<Parallax>();
            px.cam = cam;
            px.factor = factor;
            px.tileWidth = tileWidth;
            layer.transform.position = new Vector3(0f, -1f, 10f);

            bool usingBgArt = assets.backgroundBuildingSprites != null && assets.backgroundBuildingSprites.Length > 0;
            // Real art keeps its own colors; give the far layer a slight atmospheric fade so
            // the two depths still read as distinct even when they draw from the same set.
            Color tint = usingBgArt ? (order <= -16 ? new Color(0.72f, 0.72f, 0.85f) : Color.white) : color;

            var rng = new System.Random(order * 7919);
            float x = -tileWidth * 2f;
            float end = tileWidth * 3f;
            while (x < end)
            {
                float w = 1.6f + (float)rng.NextDouble() * 2.6f;
                float h = minH + (float)rng.NextDouble() * (maxH - minH);

                // A different building can be picked for every tower in the row.
                Sprite bgSprite = usingBgArt
                    ? assets.backgroundBuildingSprites[rng.Next(assets.backgroundBuildingSprites.Length)]
                    : SpriteFactory.Solid(Color.white);
                // Read the sprite's real size (its pixel rect divided by its own import PPU) instead
                // of assuming a fixed pixel size, so ANY image dimensions scale correctly here.
                float nativeW = bgSprite.rect.width / bgSprite.pixelsPerUnit;
                float nativeH = bgSprite.rect.height / bgSprite.pixelsPerUnit;

                var tower = new GameObject("Tower");
                tower.transform.SetParent(layer.transform, false);
                tower.transform.localPosition = new Vector3(x + w * 0.5f, h * 0.5f - 2f, 0f);
                tower.transform.localScale = new Vector3(w / nativeW, h / nativeH, 1f);
                SpriteFactory.NewRenderer(tower, bgSprite, order, tint);
                x += w + 0.4f + (float)rng.NextDouble() * 1.4f;
            }
        }

        VoicePlayer BuildPlayer()
        {
            var go = new GameObject("Cat");
            go.layer = VRLayers.Player;
            go.transform.position = new Vector3(2f, 2.5f, 0f);

            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            SpriteFactory.NewRenderer(art, SpriteFactory.CatSprite(Palette.Cat), 5);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.72f, 0.86f);
            box.offset = new Vector2(0f, -0.05f);

            var vp = go.AddComponent<VoicePlayer>();
            vp.runSpeed = runSpeed;
            vp.jumpVelocity = jumpVelocity;
            vp.doubleJumpVelocity = doubleJumpVelocity;

            return vp;
        }

        ChaserHollow BuildChaser()
        {
            var go = new GameObject("Hollow");
            go.transform.position = new Vector3(-12f, 2f, 0f);

            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            var gsr = SpriteFactory.NewRenderer(glow, SpriteFactory.AuraGlowSprite(), 3,
                new Color(0.35f, 0.05f, 0.30f, 0.22f));
            gsr.transform.localScale = Vector3.one * 5.5f;

            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            SpriteFactory.NewRenderer(art, SpriteFactory.HollowSprite(), 4);
            art.transform.localScale = Vector3.one * 2.4f;

            return go.AddComponent<ChaserHollow>();
        }
    }
}
