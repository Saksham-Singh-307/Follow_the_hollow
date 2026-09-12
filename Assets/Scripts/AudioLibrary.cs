using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// Real audio for every event the game produces. Every field is optional: leave a
    /// slot empty and that moment just stays silent — exactly like an empty AssetLibrary
    /// sprite slot keeps drawing the procedural shape instead of your art. Assign an
    /// AudioClip and every future occurrence of that event plays it; nothing else about
    /// the game (timing, difficulty, physics) changes.
    ///
    /// How to hook this up:
    ///   1. Import your audio into the project (drag WAV/MP3/OGG files into Assets).
    ///   2. Find the GameBootstrap component in the scene and drag each clip into the
    ///      matching slot below (they show up grouped, same order as here).
    ///   3. Everything except backgroundMusic plays once as a one-shot the instant its
    ///      event happens. backgroundMusic loops for the whole session.
    /// </summary>
    [System.Serializable]
    public class AudioLibrary
    {
        [Header("Music (loops for the whole session)")]
        public AudioClip backgroundMusic;
        [Range(0f, 1f)] public float musicVolume = 0.5f;

        [Header("Movement")]
        public AudioClip jumpSound;         // ground jump
        public AudioClip doubleJumpSound;   // mid-air second jump
        public AudioClip landSound;         // touching ground again

        [Header("Player death — one slot per cause")]
        public AudioClip deathSpikesSound;    // ran into static or popup spikes
        public AudioClip deathPitSound;       // fell into the void
        public AudioClip deathCaughtSound;    // the Hollow caught you
        public AudioClip deathChompedSound;   // a chomper / fake coin got you
        public AudioClip deathAmbushedSound;  // a forward enemy hit you head-on

        [Header("Blocks & traps")]
        public AudioClip fakeBlockBreakSound;       // block evaporates the instant you land on it
        public AudioClip fallingBlockBreakSound;    // block gives way and drops after the delay
        public AudioClip invisibleBlockRevealSound; // hidden block pops into view under you
        public AudioClip popupSpikeSound;           // buried spikes launching up as you approach
        public AudioClip crusherFallSound;          // ceiling crusher letting go
        public AudioClip chomperRevealSound;        // disguised chomper/fake coin bares its teeth

        [Header("Pickups")]
        public AudioClip coinSound;
        public AudioClip speedPowerUpSound;
        public AudioClip highJumpPowerUpSound;
        public AudioClip invisibilityPowerUpSound;

        [Header("Flow / UI")]
        public AudioClip runStartSound;  // the run begins (calibration -> ready -> playing)
        public AudioClip gameOverSound;  // any death, right as the run ends
    }
}
