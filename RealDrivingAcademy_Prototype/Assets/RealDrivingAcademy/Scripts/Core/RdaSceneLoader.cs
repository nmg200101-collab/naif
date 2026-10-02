using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealDrivingAcademy.Core
{
    public sealed class RdaSceneLoader : MonoBehaviour
    {
        public bool IsLoading { get; private set; }
        public event Action<string> SceneLoadStarted;
        public event Action<string> SceneLoadCompleted;

        public bool CanLoad(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName)
                && Application.CanStreamedLevelBeLoaded(sceneName);
        }

        public bool TryLoad(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"RDA scene load ignored because another load is active: {sceneName}");
                return false;
            }

            if (!CanLoad(sceneName))
            {
                Debug.LogError($"RDA scene is not available in Build Settings: {sceneName}");
                return false;
            }

            StartCoroutine(LoadRoutine(sceneName));
            return true;
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            SceneLoadStarted?.Invoke(sceneName);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                IsLoading = false;
                Debug.LogError($"Unity could not start loading scene: {sceneName}");
                yield break;
            }

            while (!operation.isDone)
                yield return null;

            IsLoading = false;
            SceneLoadCompleted?.Invoke(sceneName);
        }
    }
}
