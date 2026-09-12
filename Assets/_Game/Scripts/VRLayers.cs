using UnityEngine;

namespace VoiceRunner
{
    /// <summary>Layer lookups resolved once, with safe fallbacks if the layers are missing.</summary>
    public static class VRLayers
    {
        static int ground = -1, hazard = -1, player = -1;

        static int Resolve(ref int cached, string name, int fallback)
        {
            if (cached >= 0) return cached;
            int l = LayerMask.NameToLayer(name);
            cached = l < 0 ? fallback : l;
            return cached;
        }

        public static int Ground => Resolve(ref ground, "VRGround", 6);
        public static int Hazard => Resolve(ref hazard, "VRHazard", 7);
        public static int Player => Resolve(ref player, "VRPlayer", 8);

        public static LayerMask GroundMask => 1 << Ground;
        public static LayerMask HazardMask => 1 << Hazard;
    }

    public enum DeathCause { Spikes, Pit, Caught, Chomped, Ambushed }
}
