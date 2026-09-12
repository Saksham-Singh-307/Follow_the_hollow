using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VoiceRunner
{
    public enum GameState { Calibrating, Ready, Playing, Dead }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Refs")]
        public VoicePlayer player;
        public ChaserHollow chaser;
        public LevelDirector director;
        public MicVoiceInput voice;
        public RunnerCamera cam;

        [Header("Rules")]
        public float killY = -8f;
        public float restartLockout = 0.6f;
        public Vector3 spawn = new Vector3(2f, 2f, 0f);

        public GameState State { get; private set; } = GameState.Calibrating;
        public float Distance { get; private set; }
        public float BestDistance { get; private set; }
        public int Coins { get; private set; }
        public int BestCoins { get; private set; }
        public int Runs { get; private set; }
        public DeathCause LastCause { get; private set; }
        public float TimeInState => Time.time - stateEnteredAt;

        float stateEnteredAt;
        float startX;
        bool restartQueued;

        void Awake()
        {
            Instance = this;
            Physics2D.queriesHitTriggers = false;   // ground checks must ignore hazards
            BestDistance = PlayerPrefs.GetFloat("vr_best_distance", 0f);
            BestCoins = PlayerPrefs.GetInt("vr_best_coins", 0);
        }

        void Start() { SetState(GameState.Calibrating); }

        void SetState(GameState s)
        {
            State = s;
            stateEnteredAt = Time.time;
        }

        public void BeginRun()
        {
            Runs++;
            Coins = 0;
            restartQueued = false;

            int seed = Random.Range(1, int.MaxValue);   // a brand new level every run
            startX = spawn.x;

            if (director != null) director.ResetLevel(seed, -6f);
            if (player != null) player.ResetForRun(spawn);
            if (chaser != null) chaser.ResetForRun(spawn.x, spawn.y);
            if (cam != null) cam.Snap();

            Distance = 0f;
            SetState(GameState.Playing);
        }

        void Update()
        {
            switch (State)
            {
                case GameState.Calibrating:
                    if (voice == null || !voice.micReady || !voice.IsCalibrating)
                        if (TimeInState > 0.8f) SetState(GameState.Ready);
                    break;

                case GameState.Ready:
                    if (AnyStartInput()) BeginRun();
                    break;

                case GameState.Playing:
                    if (player != null)
                    {
                        Distance = Mathf.Max(Distance, player.transform.position.x - startX);
                        player.CheckPit(killY);
                    }
                    break;

                case GameState.Dead:
                    if (restartQueued && TimeInState > restartLockout) BeginRun();
                    else if (TimeInState > restartLockout && AnyStartInput()) BeginRun();
                    break;
            }
        }

        bool AnyStartInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame ||
                               kb.rKey.wasPressedThisFrame)) return true;
#else
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.R)) return true;
#endif
            return voice != null && voice.level > voice.triggerLevel;
        }

        public void RequestRestart()
        {
            if (State == GameState.Dead && TimeInState > restartLockout) restartQueued = true;
        }

        public void AddCoin() { Coins++; }

        public void PlayerDied(DeathCause cause)
        {
            if (State != GameState.Playing) return;
            LastCause = cause;
            SetState(GameState.Dead);

            if (cam != null) cam.Kick(cause == DeathCause.Caught ? 0.45f : 0.28f);
            if (director != null) director.NotifyDeath();

            if (Distance > BestDistance)
            {
                BestDistance = Distance;
                PlayerPrefs.SetFloat("vr_best_distance", BestDistance);
            }
            if (Coins > BestCoins)
            {
                BestCoins = Coins;
                PlayerPrefs.SetInt("vr_best_coins", BestCoins);
            }
            PlayerPrefs.Save();
        }

        public string CauseText()
        {
            switch (LastCause)
            {
                case DeathCause.Caught:  return "THE HOLLOW GOT YOU";
                case DeathCause.Pit:     return "INTO THE VOID";
                case DeathCause.Chomped: return "THAT WAS NOT A COIN";
                case DeathCause.Ambushed: return "HIT HEAD-ON";
                default:                 return "SPIKED";
            }
        }
    }
}
