using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VoiceRunner
{
    /// <summary>
    /// Captures the microphone, tracks a rolling noise floor and turns loudness into
    /// game events: a "burst" (sharp rise in volume) and a "hold" (still shouting).
    /// Burst -> jump. Burst while airborne -> double jump. Hold -> higher jump.
    /// Space bar mirrors the same signals so the game is testable without a mic.
    /// </summary>
    public class MicVoiceInput : MonoBehaviour
    {
        [Header("Capture")]
        public int sampleRate = 44100;
        public int window = 1024;

        [Header("Sensitivity")]
        [Tooltip("dB above the measured noise floor where level reads 0.")]
        public float floorHeadroom = 6f;
        [Tooltip("dB above the noise floor where level reads 1.")]
        public float dynamicRange = 22f;
        [Tooltip("Normalised level needed to fire a burst.")]
        public float triggerLevel = 0.45f;
        [Tooltip("Level the signal must drop below before another burst can fire.")]
        public float releaseLevel = 0.22f;
        [Tooltip("Level that still counts as 'holding' the shout.")]
        public float holdLevel = 0.30f;
        public float burstCooldown = 0.10f;

        [Header("Runtime (read only)")]
        public float level;          // 0..1 normalised loudness
        public float db;             // raw dBFS
        public float noiseFloorDb = -60f;
        public bool isHolding;
        public bool micReady;
        public string micStatus = "starting...";

        /// <summary>Fired the instant the player's voice spikes.</summary>
        public event Action OnBurst;

        AudioClip clip;
        string device;
        float[] buffer;
        bool armed = true;
        float lastBurstTime = -99f;
        float calibrateEndTime;
        int lastSamplePos;

        const float SilenceDb = -80f;

        void Awake()
        {
            buffer = new float[window];
            DontDestroyOnLoad(gameObject);
        }

        void Start() { StartMic(); }

        public void StartMic()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                micReady = false;
                micStatus = "no microphone found - SPACE still works";
                return;
            }

            device = Microphone.devices[0];
            int min, max;
            Microphone.GetDeviceCaps(device, out min, out max);
            int rate = sampleRate;
            if (max > 0) rate = Mathf.Clamp(rate, Mathf.Max(min, 8000), max);

            clip = Microphone.Start(device, true, 1, rate);
            if (clip == null)
            {
                micReady = false;
                micStatus = "mic blocked (check OS privacy settings)";
                return;
            }

            micReady = true;
            micStatus = device;
            noiseFloorDb = -50f;
            calibrateEndTime = Time.unscaledTime + 1.0f;
        }

        public bool IsCalibrating => micReady && Time.unscaledTime < calibrateEndTime;
        public float CalibrationProgress =>
            micReady ? Mathf.Clamp01(1f - (calibrateEndTime - Time.unscaledTime) / 1.0f) : 1f;

        void Update()
        {
            SampleMic();
            HandleKeyboard();
        }

        void SampleMic()
        {
            if (!micReady || clip == null) { level = Mathf.Lerp(level, 0f, Time.deltaTime * 10f); return; }

            int pos = Microphone.GetPosition(device);
            if (pos < window || pos == lastSamplePos) return;
            lastSamplePos = pos;

            clip.GetData(buffer, pos - window);

            double sum = 0.0;
            for (int i = 0; i < window; i++) { float s = buffer[i]; sum += s * s; }
            float rms = Mathf.Sqrt((float)(sum / window));
            db = 20f * Mathf.Log10(Mathf.Max(rms, 1e-7f));
            if (db < SilenceDb) db = SilenceDb;

            // Noise floor: drops fast toward quiet, creeps up slowly so a noisy room
            // does not permanently arm the trigger.
            float adapt = db < noiseFloorDb ? 12f : 0.35f;
            if (IsCalibrating) adapt = 8f;
            noiseFloorDb = Mathf.Lerp(noiseFloorDb, db, Time.deltaTime * adapt);
            noiseFloorDb = Mathf.Clamp(noiseFloorDb, -75f, -20f);

            float lo = noiseFloorDb + floorHeadroom;
            float hi = lo + dynamicRange;
            float raw = Mathf.InverseLerp(lo, hi, db);

            // Fast attack, slower release: keeps the meter honest but stops flicker.
            level = raw > level ? raw : Mathf.Lerp(level, raw, Time.deltaTime * 14f);

            if (IsCalibrating) return;

            isHolding = level >= holdLevel;

            if (level < releaseLevel) armed = true;
            if (armed && level >= triggerLevel && Time.time - lastBurstTime >= burstCooldown)
            {
                armed = false;
                lastBurstTime = Time.time;
                OnBurst?.Invoke();
            }
        }

        void HandleKeyboard()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return;
            bool down = kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
            bool held = kb.spaceKey.isPressed || kb.upArrowKey.isPressed;
#else
            bool down = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow);
            bool held = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow);
#endif
            if (!micReady || IsCalibrating) isHolding = false;
            if (held) { isHolding = true; level = Mathf.Max(level, 0.85f); }
            if (down) OnBurst?.Invoke();
        }

        void OnDestroy()
        {
            if (micReady && !string.IsNullOrEmpty(device)) Microphone.End(device);
        }
    }
}
