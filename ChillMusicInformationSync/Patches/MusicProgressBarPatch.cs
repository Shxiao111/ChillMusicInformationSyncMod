using ChillMusicInformationSync.SMTC;
using HarmonyLib;


namespace ChillMusicInformationSync.Patches
{
    [HarmonyPatch(typeof(MusicService), "GetCurrentMusicProgress")]
    public static class MusicServiceProgressPatch
    {
        public static bool Prefix(ref float __result)
        {
            if (SMTCStatus.IsControlledByMod)
            {
                float progress = SMTCStatus.CurrentProgress;
                __result = (float.IsNaN(progress) || progress < 0f) ? 0f : progress;
                return false;
            }
            return true;
        }
    }
}

