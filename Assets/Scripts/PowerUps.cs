using UnityEngine;

namespace VoiceRunner
{
    /// <summary>The three temporary buffs the level hands out in place of a weapon.</summary>
    public enum PowerUpKind { Speed, Invisibility, HighJump }

    /// <summary>
    /// A pickup that grants a timed buff instead of a way to fight back. Rather than
    /// shooting a hazard out of the way, these help you get past it: outrun the Hollow,
    /// phase straight through spikes/chompers/forward enemies, or clear a jump you'd
    /// otherwise clip. Built the same way Coin is - trigger collider, one Update for
    /// flourish, self-destroys once collected.
    /// </summary>
    public class PowerUp : MonoBehaviour
    {
        public PowerUpKind kind = PowerUpKind.Speed;

        [Header("Speed")]
        public float speedMultiplier = 1.6f;
        public float speedDuration = 4f;

        [Header("High Jump")]
        public float jumpMultiplier = 1.35f;
        public float jumpDuration = 5f;

        [Header("Invisibility")]
        public float invisibilityDuration = 3.5f;

        Vector3 home;

        void Awake() { home = transform.position; }

        void Update()
        {
            // Spin + gentle bob so it reads as "special", the same trick Coin uses for spin.
            transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 3f + home.x) * 50f, 0f);
            float y = Mathf.Sin(Time.time * 4f + home.x) * 0.12f;
            transform.position = home + new Vector3(0f, y, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var vp = other.GetComponent<VoicePlayer>();
            if (vp == null) return;

            switch (kind)
            {
                case PowerUpKind.Speed:
                    vp.ApplySpeedBoost(speedMultiplier, speedDuration);
                    break;
                case PowerUpKind.HighJump:
                    vp.ApplyHighJump(jumpMultiplier, jumpDuration);
                    break;
                case PowerUpKind.Invisibility:
                    vp.ApplyInvisibility(invisibilityDuration);
                    break;
            }

            var go = new GameObject("PowerUpPop");
            go.transform.position = home;
            var s = SpriteFactory.NewRenderer(go, SpriteFactory.Glow(), 6, GlowColor());
            go.AddComponent<FadeAway>().Init(s, 0.35f, 2.4f);

            Destroy(gameObject);
        }

        Color GlowColor()
        {
            switch (kind)
            {
                case PowerUpKind.Speed:    return new Color(1f, 0.85f, 0.25f, 0.85f);
                case PowerUpKind.HighJump: return new Color(0.45f, 1f, 0.55f, 0.85f);
                default:                   return new Color(0.6f, 0.5f, 1f, 0.85f);
            }
        }
    }
}
