using System;
using System.IO;
using UnityEngine;

namespace RealDrivingAcademy.Persistence
{
    [DefaultExecutionOrder(-900)]
    public sealed class RdaPersistenceService : MonoBehaviour
    {
        const string SaveFileName = "rda_save_v1.json";

        [SerializeField] RdaSaveData data = new RdaSaveData();

        public RdaSaveData Data => data;
        public RdaUserSettings Settings => data.settings;
        public RdaProgressData Progress => data.progress;
        public string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public event Action SettingsChanged;
        public event Action ProgressChanged;
        public event Action SaveCompleted;

        void Awake()
        {
            LoadOrCreate();
            ApplyRuntimeSettings();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveNow();
        }

        void OnApplicationQuit()
        {
            SaveNow();
        }

        public void LoadOrCreate()
        {
            data = new RdaSaveData();

            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    var loaded = JsonUtility.FromJson<RdaSaveData>(json);
                    if (loaded != null)
                        data = loaded;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"RDA save load failed. Defaults will be used. {exception.Message}");
            }

            data.Sanitize();
        }

        public bool SaveNow()
        {
            try
            {
                data.Sanitize();

                string directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                string tempPath = SavePath + ".tmp";
                File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));

                if (File.Exists(SavePath))
                    File.Delete(SavePath);

                File.Move(tempPath, SavePath);
                SaveCompleted?.Invoke();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"RDA save failed: {exception.Message}");
                return false;
            }
        }

        public void ResetAllData()
        {
            data = new RdaSaveData();
            data.Sanitize();
            ApplyRuntimeSettings();
            SaveNow();
            SettingsChanged?.Invoke();
            ProgressChanged?.Invoke();
        }

        public void SetMasterVolume(float value)
        {
            Settings.masterVolume = value;
            Settings.Sanitize();
            ApplyRuntimeSettings();
            SaveNow();
            SettingsChanged?.Invoke();
        }

        public void SetMusicVolume(float value)
        {
            Settings.musicVolume = value;
            Settings.Sanitize();
            SaveNow();
            SettingsChanged?.Invoke();
        }

        public void SetSfxVolume(float value)
        {
            Settings.sfxVolume = value;
            Settings.Sanitize();
            SaveNow();
            SettingsChanged?.Invoke();
        }

        public void SetSteeringSensitivity(float value)
        {
            Settings.steeringSensitivity = value;
            Settings.Sanitize();
            SaveNow();
            SettingsChanged?.Invoke();
        }

        public void SetCameraSensitivity(float value)
        {
            Settings.cameraSensitivity = value;
            Settings.Sanitize();
            SaveNow();
            SettingsChanged?.Invoke();
        }

        public void SetLanguage(string languageCode)
        {
            Settings.language = languageCode;
            Settings.Sanitize();
            SaveNow();
            SettingsChanged?.Invoke();
        }

        public void SetQualityLevel(int level)
        {
            Settings.qualityLevel = level;
            ApplyRuntimeSettings();
            SaveNow();
            SettingsChanged?.Invoke();
        }

        public void SetHapticsEnabled(bool enabled)
        {
            Settings.hapticsEnabled = enabled;
            SaveNow();
            SettingsChanged?.Invoke();
        }

        public bool CompleteLesson(string lessonId)
        {
            bool changed = Progress.MarkLessonComplete(lessonId);
            if (!changed)
                return false;

            SaveNow();
            ProgressChanged?.Invoke();
            return true;
        }

        public void SetSelectedVehicle(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
                return;

            Progress.selectedVehicleId = vehicleId;
            SaveNow();
            ProgressChanged?.Invoke();
        }

        public void ApplyRuntimeSettings()
        {
            AudioListener.volume = Settings.masterVolume;

            int qualityCount = QualitySettings.names == null ? 0 : QualitySettings.names.Length;
            if (Settings.qualityLevel >= 0 && Settings.qualityLevel < qualityCount)
                QualitySettings.SetQualityLevel(Settings.qualityLevel, true);
        }
    }
}
