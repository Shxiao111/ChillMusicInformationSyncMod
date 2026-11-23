using ChillMusicInformationSync.SMTC;
using HarmonyLib;
using System;


[HarmonyPatch(typeof(MusicService), "SetMusicProgress", new Type[] { typeof(float) })]
public static class SetMusicProgressPatch
{
    private static double _lastSeekSeconds = -1;
    private static DateTime _lastSeekUtc = DateTime.MinValue;

    public static bool Prefix(float progress)
    {
        if (SMTCStatus.IsControlledByMod)
        {
            try
            {
                (TimeSpan currentPosition, TimeSpan duration) = SMTCImport.Instance.GetTimeline();

                if (duration.TotalSeconds > 0)
                {
                    double clampedProgress = progress < 0f ? 0f : (progress > 1f ? 1f : progress);
                    double newTotalSeconds = duration.TotalSeconds * clampedProgress;
                    DateTime now = DateTime.UtcNow;
                    if (Math.Abs(newTotalSeconds - _lastSeekSeconds) < 0.05 && (now - _lastSeekUtc).TotalMilliseconds < 100)
                    {
                        return false;
                    }

                    _lastSeekSeconds = newTotalSeconds;
                    _lastSeekUtc = now;
                    TimeSpan newPosition = TimeSpan.FromSeconds(newTotalSeconds);
                    SMTCImport.SetTimelinePosition(newPosition);
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        return true;
    }
}