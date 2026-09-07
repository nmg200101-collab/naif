using UnityEngine;

namespace RDA.V50.Core
{
    public static class RDAQualityFoundation
    {
        public static void ApplyMobileBaseline()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;
        }
    }
}