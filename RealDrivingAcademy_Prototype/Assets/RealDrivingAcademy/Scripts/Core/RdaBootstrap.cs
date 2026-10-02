using UnityEngine;

namespace RealDrivingAcademy.Core
{
    public static class RdaBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureCoreExists()
        {
            if (Object.FindObjectOfType<RdaGameManager>() != null)
                return;

            var root = new GameObject("[RDA] Game Manager");
            root.AddComponent<RdaGameManager>();
        }
    }
}
