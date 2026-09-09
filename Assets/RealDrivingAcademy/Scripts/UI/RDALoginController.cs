using UnityEngine;
using UnityEngine.UI;
using RDA.V50.Core;

namespace RDA.V50.UI
{
    [DisallowMultipleComponent]
    public sealed class RDALoginController : MonoBehaviour
    {
        [SerializeField] private Button continueAsGuestButton;
        [SerializeField] private Button exitButton;

        public void Configure(Button guestButton, Button quitButton)
        {
            continueAsGuestButton = guestButton;
            exitButton = quitButton;
        }

        private void OnEnable()
        {
            if (continueAsGuestButton) continueAsGuestButton.onClick.AddListener(ContinueAsGuest);
            if (exitButton) exitButton.onClick.AddListener(ExitApplication);
        }

        private void OnDisable()
        {
            if (continueAsGuestButton) continueAsGuestButton.onClick.RemoveListener(ContinueAsGuest);
            if (exitButton) exitButton.onClick.RemoveListener(ExitApplication);
        }

        private static void ContinueAsGuest() => RDAAppSession.ContinueAsGuest();
        private static void ExitApplication() => Application.Quit();
    }
}
