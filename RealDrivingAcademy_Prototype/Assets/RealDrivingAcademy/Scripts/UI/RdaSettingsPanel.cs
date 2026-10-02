using UnityEngine;
using UnityEngine.UI;
using RealDrivingAcademy.Core;
using RealDrivingAcademy.Persistence;

namespace RealDrivingAcademy.UI
{
    public sealed class RdaSettingsPanel : MonoBehaviour
    {
        public Slider masterVolume;
        public Slider steeringSensitivity;
        public Slider cameraSensitivity;
        public Toggle haptics;

        bool syncing;

        RdaPersistenceService Persistence
        {
            get
            {
                return RdaGameManager.Instance == null ? null : RdaGameManager.Instance.Persistence;
            }
        }

        void Start()
        {
            SyncFromSave();
        }

        public void SyncFromSave()
        {
            var persistence = Persistence;
            if (persistence == null) return;

            syncing = true;
            if (masterVolume != null) masterVolume.value = persistence.Settings.masterVolume;
            if (steeringSensitivity != null) steeringSensitivity.value = persistence.Settings.steeringSensitivity;
            if (cameraSensitivity != null) cameraSensitivity.value = persistence.Settings.cameraSensitivity;
            if (haptics != null) haptics.isOn = persistence.Settings.hapticsEnabled;
            syncing = false;
        }

        public void SetMasterVolume(float value)
        {
            if (!syncing && Persistence != null) Persistence.SetMasterVolume(value);
        }

        public void SetSteeringSensitivity(float value)
        {
            if (!syncing && Persistence != null) Persistence.SetSteeringSensitivity(value);
        }

        public void SetCameraSensitivity(float value)
        {
            if (!syncing && Persistence != null) Persistence.SetCameraSensitivity(value);
        }

        public void SetHaptics(bool enabled)
        {
            if (!syncing && Persistence != null) Persistence.SetHapticsEnabled(enabled);
        }

        public void ResetSettings()
        {
            if (Persistence == null) return;
            Persistence.ResetAllData();
            SyncFromSave();
        }
    }
}
