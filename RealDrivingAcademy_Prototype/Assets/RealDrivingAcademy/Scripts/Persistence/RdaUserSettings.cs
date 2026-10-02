using System;
using UnityEngine;

namespace RealDrivingAcademy.Persistence
{
    [Serializable]
    public sealed class RdaUserSettings
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 0.9f;
        public float steeringSensitivity = 1f;
        public float cameraSensitivity = 1f;
        public string language = "ar";
        public int qualityLevel = -1;
        public bool hapticsEnabled = true;

        public void Sanitize()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            musicVolume = Mathf.Clamp01(musicVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            steeringSensitivity = Mathf.Clamp(steeringSensitivity, 0.25f, 2f);
            cameraSensitivity = Mathf.Clamp(cameraSensitivity, 0.25f, 2f);
            if (string.IsNullOrWhiteSpace(language))
                language = "ar";
        }
    }
}
