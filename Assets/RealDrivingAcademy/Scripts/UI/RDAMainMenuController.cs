using UnityEngine;
using UnityEngine.UI;
using RDA.V50.Core;

namespace RDA.V50.UI
{
    [DisallowMultipleComponent]
    public sealed class RDAMainMenuController : MonoBehaviour
    {
        [SerializeField] private Button backToLoginButton;

        public void Configure(Button backButton)
        {
            backToLoginButton = backButton;
        }

        private void OnEnable()
        {
            if (backToLoginButton) backToLoginButton.onClick.AddListener(BackToLogin);
        }

        private void OnDisable()
        {
            if (backToLoginButton) backToLoginButton.onClick.RemoveListener(BackToLogin);
        }

        private static void BackToLogin()
        {
            RDAAppSession.SaveNow();
            RDASceneFlow.LoadLogin();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) RDAAppSession.SaveNow();
        }
    }
}
