using UnityEngine;
using UnityEngine.SceneManagement;

namespace RDA.V50.Core
{
    public sealed class RDABootstrap : MonoBehaviour
    {
        [SerializeField] private string loginScene = "RDA_Login";
        [SerializeField] private float splashSeconds = 2.25f;
        private float t;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Update()
        {
            t += Time.unscaledDeltaTime;
            if (t >= splashSeconds)
                SceneManager.LoadScene(loginScene);
        }
    }
}
