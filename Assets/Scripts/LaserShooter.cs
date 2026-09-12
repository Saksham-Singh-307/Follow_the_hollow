using System.Collections.Generic;
using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// Fires a forward laser off a quick voice combo instead of one long sustained shout.
    /// A real held shout naturally wavers across the jump-trigger threshold, so it kept
    /// firing extra jumps while the player tried to "hold still and charge". Three quick
    /// shouts in a row reads the same OnBurst event the jump already uses, so it never
    /// fights with jumping - it just counts it.
    /// </summary>
    public class LaserShooter : MonoBehaviour
    {
        [Header("Trigger")]
        [Tooltip("How many quick shouts in a row fire the laser.")]
        public int shoutsToFire = 3;
        [Tooltip("Max gap allowed between shouts before the combo resets, in seconds.")]
        public float comboWindow = 1.1f;

        [Header("Beam")]
        public float range = 16f;
        public float width = 0.6f;
        public float visualLife = 0.18f;
        public Color color = new Color(0.35f, 1f, 0.85f);

        [Header("Refs")]
        public MicVoiceInput voice;
        public GameManager game;

        /// <summary>How many shouts of the current combo have landed so far.</summary>
        public int ComboCount { get; private set; }
        /// <summary>0..1 combo progress, for HUD bars.</summary>
        public float Charge01 => shoutsToFire <= 0 ? 0f : Mathf.Clamp01((float)ComboCount / shoutsToFire);
        /// <summary>True briefly after a beam fires - handy for HUD flashes.</summary>
        public bool JustFired => justFiredTimer > 0f;

        static readonly List<Collider2D> hitBuffer = new List<Collider2D>();
        float lastBurstTime = -99f;
        float justFiredTimer;

        /// <summary>Call after assigning refs so the combo listens to the right mic.</summary>
        public void Bind(MicVoiceInput mic)
        {
            if (voice != null) voice.OnBurst -= HandleBurst;
            voice = mic;
            if (voice != null) voice.OnBurst += HandleBurst;
        }

        void OnEnable() { if (voice != null) voice.OnBurst += HandleBurst; }
        void OnDisable() { if (voice != null) voice.OnBurst -= HandleBurst; }

        void Update()
        {
            if (justFiredTimer > 0f) justFiredTimer -= Time.deltaTime;

            // Waited too long between shouts - the combo goes stale.
            if (ComboCount > 0 && Time.time - lastBurstTime > comboWindow) ComboCount = 0;
        }

        void HandleBurst()
        {
            if (game == null || game.State != GameState.Playing) return;

            if (Time.time - lastBurstTime > comboWindow) ComboCount = 0;
            lastBurstTime = Time.time;
            ComboCount++;

            if (ComboCount >= shoutsToFire)
            {
                ComboCount = 0;
                Fire();
            }
        }

        void Fire()
        {
            justFiredTimer = 0.4f;

            Vector3 origin = transform.position;
            Vector2 center = (Vector2)origin + Vector2.right * (range * 0.5f);
            Vector2 size = new Vector2(range, width);

            // Hazard colliders are triggers, and the project turns off global trigger
            // queries (see GameManager.Awake), so this needs its own filter to see them.
            var filter = new ContactFilter2D();
            filter.SetLayerMask(VRLayers.HazardMask);
            filter.useTriggers = true;

            hitBuffer.Clear();
            Physics2D.OverlapBox(center, size, 0f, filter, hitBuffer);
            foreach (var h in hitBuffer)
            {
                var enemy = h.GetComponent<ForwardEnemyMover>();
                if (enemy != null) enemy.Zap();
            }

            SpawnBeamVisual(origin);
        }

        void SpawnBeamVisual(Vector3 origin)
        {
            var go = new GameObject("LaserBeam");
            go.transform.position = origin + Vector3.right * (range * 0.5f);

            const float texWorldSize = 8f / SpriteFactory.PPU; // SpriteFactory.Solid() is an 8x8 sprite
            var sr = SpriteFactory.NewRenderer(go, SpriteFactory.Solid(Color.white), 6, color);
            go.transform.localScale = new Vector3(range / texWorldSize, width / texWorldSize, 1f);

            go.AddComponent<LaserBeamFX>().Init(sr, visualLife);
        }
    }

    /// <summary>Bright flash that thins and fades - the beam's brief lifetime.</summary>
    public class LaserBeamFX : MonoBehaviour
    {
        SpriteRenderer sr;
        float life, t;
        Vector3 startScale;

        public void Init(SpriteRenderer renderer, float lifetime)
        {
            sr = renderer;
            life = Mathf.Max(lifetime, 0.01f);
            startScale = transform.localScale;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / life);
            if (sr != null)
            {
                var c = sr.color; c.a = Mathf.Lerp(1f, 0f, k); sr.color = c;
            }
            transform.localScale = new Vector3(startScale.x, startScale.y * Mathf.Lerp(1f, 0.15f, k), 1f);
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
