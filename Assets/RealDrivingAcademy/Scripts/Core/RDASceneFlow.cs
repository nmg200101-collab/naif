using UnityEngine;
using UnityEngine.SceneManagement;

namespace RDA.V50.Core
{
    public static class RDASceneFlow
    {
        public const string Boot = "RDA_Boot";
        public const string Login = "RDA_Login";
        public const string MainMenu = "RDA_MainMenu";

        public static bool Load(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[RDA] SceneFlow rejected an empty scene name.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[RDA] Scene is not available in Build Settings: {sceneName}");
                return false;
            }

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            return true;
        }

        public static bool LoadLogin() => Load(Login);
        public static bool LoadMainMenu() => Load(MainMenu);
    }
}
