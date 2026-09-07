using System;
using UnityEngine;

namespace RDA.V50.CameraSystem
{
    public enum RDACameraMode { Driver, CenterInterior, Hood, ChaseNear, ChaseFar, Cinematic }

    [Serializable]
    public sealed class RDACameraPoint
    {
        public RDACameraMode mode;
        public Transform point;
    }

    public sealed class RDACameraRig : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private RDACameraPoint[] points;
        public RDACameraMode CurrentMode { get; private set; }

        public bool SetMode(RDACameraMode mode)
        {
            if (!targetCamera || points == null) return false;
            foreach (var item in points)
            {
                if (item == null || item.mode != mode || !item.point) continue;
                targetCamera.transform.SetPositionAndRotation(item.point.position, item.point.rotation);
                targetCamera.transform.SetParent(item.point, true);
                CurrentMode = mode;
                return true;
            }
            Debug.LogWarning($"[RDA] Camera point missing for {mode}.");
            return false;
        }
    }
}