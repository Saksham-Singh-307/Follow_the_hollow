using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// The Hollow. It does not jump, climb or path-find - it simply never stops coming.
    /// While the cat runs clean it loses a little ground; the moment the cat is wedged
    /// against a wall or bounces off a trap, it closes.
    /// </summary>
    public class ChaserHollow : MonoBehaviour
    {
        [Header("Chase")]
        public float baseSpeedFactor = 0.965f;   // relative to the player's run speed
        public float rampPerSecond = 0.004f;     // it learns your rhythm
        public float maxSpeedFactor = 1.12f;
        public float startGap = 13f;
        public float catchDistance = 1.0f;
        public float maxGap = 20f;

        [Header("Refs")]
        public VoicePlayer player;
        public GameManager game;

        /// <summary>0 = comfortably ahead, 1 = breathing down your neck.</summary>
        public float Pressure { get; private set; }
        public float Gap { get; private set; }

        float speedFactor;
        float runTime;
        Transform art;
        SpriteRenderer sr;
        Transform glow;

        void Awake()
        {
            art = transform.Find("Art");
            if (art != null) sr = art.GetComponent<SpriteRenderer>();
            glow = transform.Find("Glow");
        }

        public void ResetForRun(float playerX, float y)
        {
            speedFactor = baseSpeedFactor;
            runTime = 0f;
            Pressure = 0f;
            Gap = startGap;
            transform.position = new Vector3(playerX - startGap, y, 0f);
        }

        void Update()
        {
            if (player == null || game == null) return;

            Vector3 p = player.transform.position;
            Gap = p.x - transform.position.x;

            if (game.State == GameState.Playing)
            {
                runTime += Time.deltaTime;
                speedFactor = Mathf.Min(baseSpeedFactor + runTime * rampPerSecond, maxSpeedFactor);

                float speed = player.runSpeed * speedFactor;
                // If it falls too far behind it stops being a threat, so it surges.
                if (Gap > maxGap) speed = player.runSpeed * 1.45f;
                transform.position += Vector3.right * speed * Time.deltaTime;

                Pressure = Mathf.Clamp01(1f - (Gap - catchDistance) / (startGap - catchDistance));

                if (Gap <= catchDistance && !player.IsInvisible) player.Kill(DeathCause.Caught);
            }

            // drift toward the cat's height, always a beat late
            float ty = Mathf.Lerp(transform.position.y, p.y + 0.35f, Time.deltaTime * 2.2f);
            transform.position = new Vector3(transform.position.x, ty, 0f);

            if (art != null)
            {
                float wob = 1f + Mathf.Sin(Time.time * 6f) * 0.06f;
                art.localScale = new Vector3(2.4f * wob, 2.4f / wob, 1f);
                art.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.3f) * 5f);
            }
            if (glow != null)
            {
                float s = 5.5f + Pressure * 3.5f + Mathf.Sin(Time.time * 3f) * 0.3f;
                glow.localScale = new Vector3(s, s, 1f);
                var gsr = glow.GetComponent<SpriteRenderer>();
                if (gsr != null) gsr.color = new Color(0.35f, 0.05f, 0.30f, 0.18f + Pressure * 0.30f);
            }
            if (sr != null)
                sr.color = Color.Lerp(Color.white, new Color(1f, 0.65f, 0.65f), Pressure);
        }
    }
}
