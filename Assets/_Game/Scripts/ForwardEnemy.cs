using UnityEngine;
using System.Collections;

namespace VoiceRunner
{
    /// <summary>
    /// Spawns enemies that approach from ahead of the player - the opposite direction
    /// from the Hollow, which chases from behind. Touching one is lethal; the player's
    /// laser (LaserShooter) destroys them first. Attach via GameBootstrap.
    /// </summary>
    public class ForwardEnemySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        public float spawnDistanceAhead = 15f;    // Distance in front of player to spawn
        public float minSpawnInterval = 2.6f;     // Minimum time between enemy spawns
        public float maxSpawnInterval = 5.0f;     // Maximum time between enemy spawns

        [Header("Enemy Attributes")]
        public float baseSpeed = 3.6f;            // Slowest approach speed (low difficulty)
        public float maxSpeed = 6.5f;             // Fastest approach speed (max difficulty)
        public Vector2 enemySize = new Vector2(0.8f, 0.8f);
        public Color enemyColor = new Color(0.85f, 0.15f, 0.25f); // Crimson red tint

        [Header("Refs")]
        public Transform player;
        public GameManager game;
        public LevelDirector director;

        void Start()
        {
            StartCoroutine(SpawnRoutine());
        }

        IEnumerator SpawnRoutine()
        {
            while (true)
            {
                // Locate player and game manager dynamically if not assigned
                if (player == null)
                {
                    var p = FindAnyObjectByType<VoicePlayer>();
                    if (p != null) player = p.transform;
                }
                if (game == null) game = FindAnyObjectByType<GameManager>();

                if (player != null && game != null && game.State == GameState.Playing)
                    SpawnIncomingEnemy();

                float d = director != null ? director.Difficulty : 0f;
                // Enemies come thicker and faster as the run's difficulty ramps up.
                float interval = Random.Range(minSpawnInterval, maxSpawnInterval) * Mathf.Lerp(1f, 0.55f, d);
                yield return new WaitForSeconds(interval);
            }
        }

        void SpawnIncomingEnemy()
        {
            float d = director != null ? director.Difficulty : 0f;
            float speed = Mathf.Lerp(baseSpeed, maxSpeed, d);

            // The cat always runs to the right, so "ahead" is always +X.
            Vector3 spawnPos = player.position + new Vector3(spawnDistanceAhead, 0f, 0f);
            spawnPos.y = ChunkBuilder.GroundTopY + (enemySize.y * 0.5f);

            GameObject enemyNode = new GameObject("ForwardEnemy");
            enemyNode.transform.position = spawnPos;
            enemyNode.layer = VRLayers.Hazard;

            BoxCollider2D col = enemyNode.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = enemySize;

            GameObject art = new GameObject("Art");
            art.transform.SetParent(enemyNode.transform, false);
            Sprite proceduralSprite = SpriteFactory.Block(enemyColor, true);
            SpriteFactory.NewRenderer(art, proceduralSprite, 3, Color.white);
            art.transform.localScale = new Vector3(enemySize.x, enemySize.y, 1f);

            ForwardEnemyMover mover = enemyNode.AddComponent<ForwardEnemyMover>();
            mover.speed = speed;
            mover.player = player;
            mover.game = game;
        }
    }

    /// <summary>
    /// Walks toward the player from ahead. Lethal on contact; can be destroyed early
    /// by the player's laser via Zap().
    /// </summary>
    public class ForwardEnemyMover : MonoBehaviour
    {
        public float speed = 4f;
        public Transform player;
        public GameManager game;

        void Update()
        {
            // Clean up cleanly instead of drifting through a death screen or reset.
            if (game != null && game.State != GameState.Playing)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += Vector3.left * speed * Time.deltaTime;

            // Self-destroy once it has drifted well behind the player without being hit.
            if (player != null && transform.position.x < player.position.x - 10f)
                Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var vp = other.GetComponent<VoicePlayer>();
            if (vp != null) vp.Kill(DeathCause.Ambushed);
        }

        /// <summary>Called by LaserShooter when a beam catches this enemy.</summary>
        public void Zap()
        {
            var fx = new GameObject("EnemyZapFX");
            fx.transform.position = transform.position;
            var s = SpriteFactory.NewRenderer(fx, SpriteFactory.Glow(), 5, new Color(0.4f, 1f, 0.85f, 0.85f));
            fx.AddComponent<FadeAway>().Init(s, 0.3f, 2.4f);

            var gm = game != null ? game : GameManager.Instance;
            if (gm != null) gm.AddCoin();

            Destroy(gameObject);
        }
    }
}
