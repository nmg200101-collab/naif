using UnityEngine;
using UnityEngine.SceneManagement;

namespace RDA.V50.Core
{
    public static class RDASceneFlow
    {
        public const string Boot = "RDA_Boot";
        public const string Login = "RDA_Login";
        public const string MainMenu = "RDA_MainMenu";

        public static void Load(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[RDA] SceneFlow rejected an empty scene name.");
                return;
            }
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        public static void LoadLogin() => Load(Login);
        public static void LoadMainMenu() => Load(MainMenu);
    }
}