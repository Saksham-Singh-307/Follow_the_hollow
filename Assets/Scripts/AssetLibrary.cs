using UnityEngine;

namespace VoiceRunner
{
    /// <summary>
    /// Real art for every piece the level is built from. Every field is optional: leave a
    /// slot empty and SpriteFactory keeps drawing that piece in code, exactly like before.
    /// Assign a Sprite and every future chunk that uses that piece switches to your art —
    /// nothing else about the game (layout, difficulty, physics) changes.
    ///
    /// How to hook this up:
    ///   1. Import your art into the project (drag PNGs into Assets).
    ///   2. Select each one, set Texture Type = Sprite (2D and UI).
    ///   3. Set Pixels Per Unit so ONE TILE = ONE WORLD UNIT. Ground/platform/wall/block
    ///      pieces are all placed on a 1x1 grid, so a 32x32 tile needs PPU 32, a 64x64
    ///      tile needs PPU 64, etc. Getting this wrong is the #1 cause of "my art is huge/tiny".
    ///   4. Filter Mode: Point for crisp pixel art, Bilinear for painted/hi-res art.
    ///   5. Find the GameBootstrap component in the scene and drag each sprite into the
    ///      matching slot below (they show up grouped, same order as here).
    ///
    /// Two pairs are deliberately meant to share one sprite: normal blocks + fake blocks
    /// use "questionBlockSprite" (the fake one is supposed to be visually indistinguishable),
    /// and real coins + fake coins use "coinSprite" for the same reason. Give them separate
    /// sprites only if you want the deception to be easier to spot.
    /// </summary>
    [System.Serializable]
    public class AssetLibrary
    {
        [Header("Ground & platforms (tile — repeats once per world unit)")]
        public Sprite groundTopTile;    // the walkable surface of solid ground
        public Sprite groundFillTile;   // the ground below the surface
        public Sprite platformTile;     // floating platforms (static + moving)
        public Sprite wallTile;         // vertical walls you have to clear

        [Header("Blocks (1x1)")]
        public Sprite blockSprite;         // plain solid block
        public Sprite questionBlockSprite; // baited block — also used for fake blocks on purpose
        public Sprite camoBlockSprite;     // "invisible" block that blends into the background
        public Sprite fallingBlockSprite;  // crumbles a moment after you land on it
        public Sprite crusherBodySprite;   // the slamming crusher's body

        [Header("Hazards")]
        public Sprite spikesSprite;      // floor spikes + popup spikes
        public Sprite spikesWideSprite;  // wide spikes under the crusher (falls back to spikesSprite)
        public Sprite chomperSprite;     // roaming / leaping enemy

        [Header("Pickups")]
        public Sprite coinSprite; // real coins — also used for disguised fake coins on purpose

        [Header("Characters")]
        public Sprite catSprite;      // the player
        public Sprite hollowSprite;   // the chaser
        public Sprite auraGlowSprite; // soft glow behind the chaser

        [Header("Background (optional)")]
        public Sprite[] backgroundBuildingSprites; // parallax skyline shapes — add several for variety, one is picked at random per building
    }
}
