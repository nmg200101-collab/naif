using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RDA.V50.Core
{
    [DisallowMultipleComponent]
    public sealed class RDABootstrap : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float splashSeconds = 2.25f;
        private bool transitioning;

        private void Awake()
        {
            if (SceneManager.GetActiveScene().name != RDASceneFlow.Boot)
                Debug.LogWarning("[RDA] Bootstrap is intended for RDA_Boot.");
            RDAQualityFoundation.ApplyMobileBaseline();
        }

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(splashSeconds);
            if (!transitioning)
            {
                transitioning = true;
                RDASceneFlow.LoadLogin();
            }
        }
    }
}