using BepInEx.Logging;
using HarmonyLib;
using Bulbul;
using ChillMusicInformationSync.SMTC;
using ChillMusicInformationSync.UISync;

namespace ChillMusicInformationSync.Patches
{
    internal static class SMTCScenarioHelper
    {
        private static ManualLogSource _log;
        private static bool _wasControlledByMod;
        private static bool _wasPlayingBeforeScenario;

        internal static void InitLogger(ManualLogSource log)
        {
            _log = log;
        }

        internal static void DisconnectSMTC()
        {
            if (!SMTCStatus.IsControlledByMod)
            {
                _log?.LogDebug("[ScenarioPatch] IsControlledByMod=false, 跳过断开");
                return;
            }

            _log?.LogInfo("[ScenarioPatch] 情景开始，断开SMTC...");

            _wasControlledByMod = true;
            _wasPlayingBeforeScenario = SMTCStatus.IsPlaying;
            SMTCStatus.IsSwitchingToBuiltIn = true;
            SMTCStatus.IsControlledByMod = false;
            SMTCStatus.IsPlaying = false;
            if (_wasPlayingBeforeScenario)
            {
                SMTCImport.SMTC_Pause();
            }

            _log?.LogInfo("[ScenarioPatch] SMTC已断开");
        }

        internal static void TryReconnectSMTC()
        {
            if (!_wasControlledByMod)
            {
                _log?.LogDebug("[ScenarioPatch] 断开前未被控制，跳过恢复");
                return;
            }
            _wasControlledByMod = false;
            bool shouldResume = _wasPlayingBeforeScenario;
            _wasPlayingBeforeScenario = false;

            _log?.LogInfo("[ScenarioPatch] 情景结束，恢复SMTC控制权");

            SMTCStatus.IsSwitchingToBuiltIn = false;
            SMTCStatus.IsControlledByMod = true;
            if (shouldResume)
            {
                SMTCImport.SMTC_Play();
                SMTCStatus.IsPlaying = true;
            }
            else
            {
                SMTCStatus.IsPlaying = SMTCImport.Instance.IsPlaying();
            }

            SyncUIIfControlled();
        }

        internal static void SyncUIIfControlled()
        {
            if (!SMTCStatus.IsControlledByMod || !MusicUISync.IsMusicUIActiveInHierarchy())
            {
                return;
            }

            string title = SMTCImport.Instance.GetTitle();
            string artist = SMTCImport.Instance.GetArtist();
            SMTCStatus.CurrentTitle = title;
            SMTCStatus.CurrentArtist = artist;

            if (!string.IsNullOrEmpty(title))
            {
                MusicUISync.changeMusicTrigger(title, artist, MusicChangeKind.Manual);
            }

            MusicCoverMananger.RefreshCoverInCenterIcons();
            ControlButtonHider.HideOriginalButtons();

            if (SMTCStatus.IsPlaying)
            {
                MusicUISync.PauseGameMusic();
                MusicUISync.SetButtonToPause();
            }
            else
            {
                MusicUISync.SetButtonToPlay();
                MusicUISync.SetGameMainState(false);
            }
        }
    }

    [HarmonyPatch(typeof(RoomGameManager), "ReadyPlayStory")]
    public static class ScenarioReadyPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            SMTCScenarioHelper.DisconnectSMTC();
        }
    }

    [HarmonyPatch(typeof(UIShowManagerForPC), "AdjustUIForPlayScenario")]
    public static class ScenarioUIDisablePatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            SMTCScenarioHelper.DisconnectSMTC();
        }
    }

    [HarmonyPatch(typeof(UIShowManagerForPC), "AdjustUIForEndScenario")]
    public static class ScenarioUIEnablePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            SMTCScenarioHelper.TryReconnectSMTC();
        }
    }

    [HarmonyPatch(typeof(UIShowManagerForPC), "AllUIActivate")]
    public static class ScenarioAllUIActivatePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            SMTCScenarioHelper.SyncUIIfControlled();
        }
    }
}
