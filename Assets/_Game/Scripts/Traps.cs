using UnityEngine;

namespace VoiceRunner
{
    /// <summary>Base for anything that needs to know where the cat is.</summary>
    public abstract class TrapBase : MonoBehaviour
    {
        protected Transform Player
        {
            get
            {
                var gm = GameManager.Instance;
                return (gm != null && gm.player != null) ? gm.player.transform : null;
            }
        }

        protected float DistToPlayerX
        {
            get
            {
                var p = Player;
                return p == null ? 999f : p.position.x - transform.position.x;
            }
        }
    }

    /// <summary>Looks like a solid block. Stand on it and it evaporates.</summary>
    public class FakeBlock : TrapBase
    {
        public float delay = 0.10f;
        SpriteRenderer sr;
        Collider2D col;
        bool triggered;
        float t;
        Vector3 home;

        void Awake()
        {
            sr = GetComponentInChildren<SpriteRenderer>();
            col = GetComponent<Collider2D>();
            home = transform.position;
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (triggered) return;
            if (c.collider.GetComponent<VoicePlayer>() == null) return;
            triggered = true;
        }

        void Update()
        {
            if (!triggered) return;
            t += Time.deltaTime;
            transform.position = home + new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.04f, 0.04f), 0f);
            if (sr != null) sr.color = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f, 0f), t / delay);
            if (t >= delay)
            {
                if (col != null) col.enabled = false;
                if (sr != null) sr.enabled = false;
                Destroy(gameObject, 0.2f);
            }
        }
    }

    /// <summary>Nothing there... until you are directly under it, mid-jump.</summary>
    public class InvisibleBlock : TrapBase
    {
        public float triggerWindow = 0.75f;
        SpriteRenderer sr;
        Collider2D col;
        bool shown;
        float pop;

        void Awake()
        {
            sr = GetComponentInChildren<SpriteRenderer>();
            col = GetComponent<Collider2D>();
            if (sr != null) sr.enabled = false;
            if (col != null) col.enabled = false;
        }

        void Update()
        {
            if (shown)
            {
                pop = Mathf.Min(pop + Time.deltaTime * 8f, 1f);
                if (sr != null) sr.transform.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, pop);
                return;
            }

            var p = Player;
            if (p == null) return;
            var gm = GameManager.Instance;
            bool rising = gm != null && gm.player != null && !gm.player.IsGrounded;
            if (!rising) return;
            if (Mathf.Abs(p.position.x - transform.position.x) > triggerWindow) return;
            if (p.position.y > transform.position.y - 0.35f) return;

            shown = true;
            if (sr != null) sr.enabled = true;
            if (col != null) col.enabled = true;
        }
    }

    /// <summary>Spikes buried in the floor that shoot up as you approach.</summary>
    public class PopupSpike : TrapBase
    {
        public float armDistance = 3.5f;
        public float hiddenY = -1.1f;
        public float shownY = -0.15f;
        public float riseSpeed = 14f;

        SpriteRenderer sr;
        Collider2D col;
        Transform art;
        bool armed;

        void Awake()
        {
            art = transform.GetChild(0);
            sr = art.GetComponent<SpriteRenderer>();
            col = GetComponent<Collider2D>();
            art.localPosition = new Vector3(0f, hiddenY, 0f);
            if (col != null) col.enabled = false;
        }

        void Update()
        {
            float d = -DistToPlayerX; // positive while the player is still to the left
            if (!armed && d < armDistance && d > -2f) armed = true;

            float target = armed ? shownY : hiddenY;
            var lp = art.localPosition;
            lp.y = Mathf.MoveTowards(lp.y, target, riseSpeed * Time.deltaTime);
            art.localPosition = lp;

            if (col != null)
            {
                col.enabled = lp.y > hiddenY + 0.45f;
                col.offset = new Vector2(0f, lp.y + 0.15f);
            }
        }
    }

    /// <summary>Holds your weight just long enough to matter.</summary>
    public class FallingBlock : TrapBase
    {
        public float holdTime = 0.32f;
        Rigidbody2D rb;
        SpriteRenderer sr;
        bool armed;
        float t;
        Vector3 home;

        void Awake()
        {
            sr = GetComponentInChildren<SpriteRenderer>();
            home = transform.position;
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (armed) return;
            if (c.collider.GetComponent<VoicePlayer>() == null) return;
            armed = true;
        }

        void Update()
        {
            if (!armed) return;
            t += Time.deltaTime;
            if (t < holdTime)
            {
                transform.position = home + new Vector3(Random.Range(-0.06f, 0.06f), 0f, 0f);
                if (sr != null) sr.color = Color.Lerp(Color.white, new Color(1f, 0.6f, 0.4f), t / holdTime);
            }
            else if (rb.bodyType == RigidbodyType2D.Kinematic)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 3.5f;
                Destroy(gameObject, 3f);
            }
        }
    }

    /// <summary>Marks a lethal object. Fake coins carry this too.</summary>
    public class Chomper : TrapBase
    {
        public bool disguisedAsCoin;
        public bool leaps;
        public float leapRange = 5f;
        public float leapSpeed = 9f;

        SpriteRenderer sr;
        bool revealed;
        bool leaping;
        Vector3 vel;
        float life;

        void Awake() { sr = GetComponentInChildren<SpriteRenderer>(); }

        void Update()
        {
            var p = Player;
            if (p == null) return;

            if (disguisedAsCoin && !revealed)
            {
                // bob like an innocent pickup
                transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 4f) * 45f, 0f);
                if (Mathf.Abs(p.position.x - transform.position.x) < 0.9f &&
                    Mathf.Abs(p.position.y - transform.position.y) < 1.1f)
                    Reveal();
                return;
            }

            if (leaps)
            {
                float dx = p.position.x - transform.position.x;
                if (!leaping && dx > -leapRange && dx < 0.5f)
                {
                    leaping = true;
                    vel = new Vector3(-leapSpeed * 0.35f, leapSpeed, 0f);
                }
                if (leaping)
                {
                    vel.y -= 26f * Time.deltaTime;
                    transform.position += vel * Time.deltaTime;
                    transform.Rotate(0f, 0f, -360f * Time.deltaTime);
                    life += Time.deltaTime;
                    if (life > 4f) Destroy(gameObject);
                }
            }
        }

        public void Reveal()
        {
            revealed = true;
            transform.localRotation = Quaternion.identity;
            if (sr != null)
            {
                sr.sprite = SpriteFactory.Chomper();
                sr.transform.localScale = Vector3.one * 1.25f;
            }
        }
    }

    /// <summary>A real pickup. Rare enough to be tempting.</summary>
    public class Coin : MonoBehaviour
    {
        SpriteRenderer sr;
        void Awake() { sr = GetComponentInChildren<SpriteRenderer>(); }

        void Update()
        {
            transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 5f + transform.position.x) * 55f, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<VoicePlayer>() == null) return;
            var gm = GameManager.Instance;
            if (gm != null) gm.AddCoin();
            var go = new GameObject("CoinPop");
            go.transform.position = transform.position;
            var s = SpriteFactory.NewRenderer(go, SpriteFactory.Coin(), 6);
            go.AddComponent<FadeAway>().Init(s, 0.3f, 1.8f);
            Destroy(gameObject);
        }
    }

    /// <summary>Ceiling block that lets go right as you run underneath.</summary>
    public class Crusher : TrapBase
    {
        public float armDistance = 3.6f;
        public float fallSpeed = 0f;
        bool falling;
        float life;

        void Update()
        {
            if (!falling)
            {
                float d = -DistToPlayerX;
                if (d < armDistance && d > -1f) falling = true;
                return;
            }
            fallSpeed += 30f * Time.deltaTime;
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            life += Time.deltaTime;
            if (life > 4f) Destroy(gameObject);
        }
    }

    /// <summary>Platform that drifts up and down. Timing test, not a lie.</summary>
    public class MovingPlatform : MonoBehaviour
    {
        public float amplitude = 1.6f;
        public float speed = 1.4f;
        Vector3 home;
        float phase;
        Rigidbody2D rb;

        void Awake()
        {
            home = transform.position;
            phase = Random.value * 6.28f;
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        void FixedUpdate()
        {
            Vector3 p = home + Vector3.up * Mathf.Sin(Time.time * speed + phase) * amplitude;
            if (rb != null) rb.MovePosition(p); else transform.position = p;
        }
    }
}
