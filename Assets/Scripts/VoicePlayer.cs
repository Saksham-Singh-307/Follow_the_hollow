using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// The cat. Runs forward on its own; the only control is the player's voice.
    /// Burst = jump, second burst in the air = double jump, keep shouting = float higher.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class VoicePlayer : MonoBehaviour
    {
        [Header("Run")]
        public float runSpeed = 7.0f;

        [Header("Jump")]
        public float jumpVelocity = 14.0f;
        public float doubleJumpVelocity = 12.5f;
        public float gravityRise = 4.0f;
        public float gravityFall = 6.8f;
        [Tooltip("Gravity multiplier while the player keeps shouting on the way up.")]
        public float holdGravityScale = 0.55f;
        public float maxHoldTime = 0.28f;
        public float coyoteTime = 0.10f;
        public float jumpBuffer = 0.16f;

        [Header("Refs")]
        public MicVoiceInput voice;
        public GameManager game;

        public bool IsGrounded { get; private set; }
        public bool UsedDoubleJump { get; private set; }
        public float StallAmount { get; private set; } // 0 = running free, 1 = fully blocked

        [Header("Power-ups")]
        [Tooltip("How the run speed multiplier eases back to 1 once a speed boost ends.")]
        public float powerUpFadeRate = 6f;

        /// <summary>True while a speed power-up is active.</summary>
        public bool IsSpeedBoosted => speedTimer > 0f;
        public float SpeedBoostTimeLeft => Mathf.Max(0f, speedTimer);
        public float SpeedBoostDuration => speedDurationTotal;

        /// <summary>True while a high-jump power-up is active.</summary>
        public bool IsHighJumping => jumpTimer > 0f;
        public float HighJumpTimeLeft => Mathf.Max(0f, jumpTimer);
        public float HighJumpDuration => jumpDurationTotal;

        /// <summary>True while an invisibility power-up is active - hazards pass straight
        /// through the cat, and the Hollow cannot catch it.</summary>
        public bool IsInvisible => invisTimer > 0f;
        public float InvisibleTimeLeft => Mathf.Max(0f, invisTimer);
        public float InvisibleDuration => invisDurationTotal;

        float speedTimer, speedMul = 1f, speedDurationTotal;
        float jumpTimer, jumpMul = 1f, jumpDurationTotal;
        float invisTimer, invisDurationTotal;

        Rigidbody2D rb;
        BoxCollider2D box;
        SpriteRenderer sr;
        Transform art;
        float lastGroundedTime = -99f;
        float bufferedJumpTime = -99f;
        float holdTimer;
        float lastX;
        bool alive = true;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravityRise;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            box = GetComponent<BoxCollider2D>();

            art = transform.Find("Art");
            if (art != null) sr = art.GetComponent<SpriteRenderer>();
            lastX = transform.position.x;
        }

        void OnEnable()
        {
            if (voice != null) voice.OnBurst += OnVoiceBurst;
        }

        void OnDisable()
        {
            if (voice != null) voice.OnBurst -= OnVoiceBurst;
        }

        public void Bind(MicVoiceInput mic)
        {
            if (voice != null) voice.OnBurst -= OnVoiceBurst;
            voice = mic;
            if (voice != null) voice.OnBurst += OnVoiceBurst;
        }

        /// <summary>Called by a PowerUp pickup. Refreshes the timer rather than stacking if
        /// another of the same kind is grabbed while one is already running.</summary>
        public void ApplySpeedBoost(float multiplier, float duration)
        {
            speedMul = multiplier;
            speedTimer = duration;
            speedDurationTotal = duration;
        }

        public void ApplyHighJump(float multiplier, float duration)
        {
            jumpMul = multiplier;
            jumpTimer = duration;
            jumpDurationTotal = duration;
        }

        public void ApplyInvisibility(float duration)
        {
            invisTimer = duration;
            invisDurationTotal = duration;
        }

        void OnVoiceBurst()
        {
            if (!alive || game == null) return;
            if (game.State == GameState.Dead) { game.RequestRestart(); return; }
            if (game.State != GameState.Playing) return;
            bufferedJumpTime = Time.time;
        }

        public void ResetForRun(Vector3 pos)
        {
            alive = true;
            transform.position = pos;
            transform.rotation = Quaternion.identity;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = gravityRise;
            UsedDoubleJump = false;
            holdTimer = 0f;
            bufferedJumpTime = -99f;
            lastGroundedTime = -99f;
            StallAmount = 0f;
            lastX = pos.x;
            speedTimer = 0f; speedMul = 1f; speedDurationTotal = 0f;
            jumpTimer = 0f; jumpMul = 1f; jumpDurationTotal = 0f;
            invisTimer = 0f; invisDurationTotal = 0f;
            if (art != null) art.localScale = Vector3.one;
            if (sr != null) sr.color = Color.white;
        }

        void Update()
        {
            if (alive)
            {
                if (speedTimer > 0f) { speedTimer -= Time.deltaTime; if (speedTimer <= 0f) { speedTimer = 0f; speedMul = 1f; } }
                if (jumpTimer > 0f) { jumpTimer -= Time.deltaTime; if (jumpTimer <= 0f) { jumpTimer = 0f; jumpMul = 1f; } }
                if (invisTimer > 0f) { invisTimer -= Time.deltaTime; if (invisTimer <= 0f) invisTimer = 0f; }
            }

            if (art != null)
            {
                // squash & stretch driven by vertical speed
                float v = Mathf.Clamp(rb.linearVelocity.y / 16f, -1f, 1f);
                Vector3 target = IsGrounded
                    ? new Vector3(1.08f, 0.92f, 1f)
                    : new Vector3(1f - v * 0.18f, 1f + v * 0.22f, 1f);
                art.localScale = Vector3.Lerp(art.localScale, target, Time.deltaTime * 14f);
                art.localRotation = Quaternion.Lerp(art.localRotation,
                    Quaternion.Euler(0, 0, IsGrounded ? 0f : -rb.linearVelocity.y * 0.8f), Time.deltaTime * 8f);
            }

            if (sr != null && alive)
            {
                float targetAlpha = 1f;
                if (invisTimer > 0f)
                {
                    // Flicker as a fading-soon warning in the last half second.
                    targetAlpha = invisTimer < 0.5f
                        ? 0.25f + (Mathf.Sin(Time.time * 24f) * 0.5f + 0.5f) * 0.35f
                        : 0.4f;
                }
                var c = sr.color;
                c.a = Mathf.MoveTowards(c.a, targetAlpha, Time.deltaTime * powerUpFadeRate);
                sr.color = c;
            }
        }

        void FixedUpdate()
        {
            if (!alive || game == null || game.State != GameState.Playing)
            {
                if (!alive) return;
            }

            CheckGround();

            bool playing = game != null && game.State == GameState.Playing;
            if (playing)
            {
                rb.linearVelocity = new Vector2(runSpeed * speedMul, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            // How much forward progress are we actually making? Being wedged against a
            // block is what lets the Hollow close the gap.
            float actual = (transform.position.x - lastX) / Mathf.Max(Time.fixedDeltaTime, 1e-4f);
            lastX = transform.position.x;
            float ratio = Mathf.Clamp01(1f - actual / Mathf.Max(runSpeed, 0.01f));
            StallAmount = Mathf.Lerp(StallAmount, playing ? ratio : 0f, 0.25f);

            if (playing) HandleJump();
            ApplyGravity();
        }

        void CheckGround()
        {
            Vector2 size = box != null ? box.size : new Vector2(0.8f, 0.9f);
            Vector2 offset = box != null ? box.offset : Vector2.zero;
            Vector2 feet = (Vector2)transform.position + offset + Vector2.down * (size.y * 0.5f);
            var hit = Physics2D.OverlapBox(feet, new Vector2(size.x * 0.92f, 0.14f), 0f, VRLayers.GroundMask);
            bool grounded = hit != null && rb.linearVelocity.y <= 0.5f;

            if (grounded && !IsGrounded) UsedDoubleJump = false;
            IsGrounded = grounded;
            if (grounded) lastGroundedTime = Time.time;
        }

        void HandleJump()
        {
            bool buffered = Time.time - bufferedJumpTime <= jumpBuffer;
            if (!buffered) return;

            bool canGroundJump = IsGrounded || (Time.time - lastGroundedTime <= coyoteTime);
            if (canGroundJump)
            {
                Jump(jumpVelocity * jumpMul);
                lastGroundedTime = -99f;
            }
            else if (!UsedDoubleJump)
            {
                UsedDoubleJump = true;
                Jump(doubleJumpVelocity * jumpMul);
                SpawnPuff();
            }
        }

        void Jump(float v)
        {
            bufferedJumpTime = -99f;
            holdTimer = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, v);
            IsGrounded = false;
        }

        void ApplyGravity()
        {
            bool rising = rb.linearVelocity.y > 0.01f;
            bool holding = voice != null && voice.isHolding;

            if (rising && holding && holdTimer < maxHoldTime)
            {
                holdTimer += Time.fixedDeltaTime;
                rb.gravityScale = gravityRise * holdGravityScale;
            }
            else if (rising)
            {
                rb.gravityScale = gravityRise;
                // shout stopped early -> clip the jump short, Mario style
                if (!holding && holdTimer > 0f)
                {
                    holdTimer = 0f;
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.62f);
                }
            }
            else
            {
                holdTimer = 0f;
                rb.gravityScale = gravityFall;
            }
        }

        void SpawnPuff()
        {
            var go = new GameObject("Puff");
            go.transform.position = transform.position + Vector3.down * 0.4f;
            var s = SpriteFactory.NewRenderer(go, SpriteFactory.Solid(Color.white), 4,
                new Color(1f, 1f, 1f, 0.75f));
            go.AddComponent<FadeAway>().Init(s, 0.35f, 2.2f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!alive || game == null || game.State != GameState.Playing) return;
            if (other.gameObject.layer == VRLayers.Hazard)
            {
                if (IsInvisible) return; // ghosted straight through

                var incoming = other.GetComponent<ForwardEnemyMover>();
                if (incoming != null) { Kill(DeathCause.Ambushed); return; }

                var chomp = other.GetComponent<Chomper>();
                Kill(chomp != null ? DeathCause.Chomped : DeathCause.Spikes);
            }
        }

        public void Kill(DeathCause cause)
        {
            if (!alive) return;
            alive = false;
            if (sr != null) sr.color = new Color(1f, 0.5f, 0.5f);
            rb.linearVelocity = new Vector2(-2f, 9f);
            rb.gravityScale = gravityFall;
            if (game != null) game.PlayerDied(cause);
        }

        public void ReviveFlagOnly() { alive = true; }

        /// <summary>Fell off the world.</summary>
        public void CheckPit(float killY)
        {
            if (alive && transform.position.y < killY) Kill(DeathCause.Pit);
        }
    }

    /// <summary>Tiny helper: fade a sprite out and destroy it.</summary>
    public class FadeAway : MonoBehaviour
    {
        SpriteRenderer sr;
        float life, t, grow;

        public void Init(SpriteRenderer renderer, float lifetime, float growTo)
        {
            sr = renderer; life = lifetime; grow = growTo;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / life);
            if (sr != null)
            {
                var c = sr.color; c.a = (1f - k) * 0.75f; sr.color = c;
                transform.localScale = Vector3.one * Mathf.Lerp(0.4f, grow, k);
            }
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
