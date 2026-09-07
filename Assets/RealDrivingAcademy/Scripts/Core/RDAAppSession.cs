using RDA.V50.Save;

namespace RDA.V50.Core
{
    public static class RDAAppSession
    {
        public static RDAPlayerProgress Progress { get; private set; }
        public static bool Initialized => Progress != null;

        public static void Initialize()
        {
            if (Progress == null) Progress = RDASaveSystem.Load();
        }

        public static void ContinueAsGuest()
        {
            Initialize();
            Progress.guest = true;
            Progress.lastScene = RDASceneFlow.MainMenu;
            RDASaveSystem.Save(Progress);
            RDASceneFlow.LoadMainMenu();
        }

        public static void SaveNow()
        {
            Initialize();
            RDASaveSystem.Save(Progress);
        }
    }
}