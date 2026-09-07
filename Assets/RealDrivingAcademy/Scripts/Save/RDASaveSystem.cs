using System;
using System.IO;
using UnityEngine;

namespace RDA.V50.Save
{
    [Serializable]
    public sealed class RDAPlayerProgress
    {
        public int schemaVersion = 1;
        public bool guest = true;
        public int totalScore;
        public int completedLessons;
        public string lastScene = "RDA_MainMenu";
    }

    public static class RDASaveSystem
    {
        private const string FileName = "rda_progress_v1.json";
        private static string PathName => Path.Combine(Application.persistentDataPath, FileName);

        public static RDAPlayerProgress Load()
        {
            try
            {
                if (!File.Exists(PathName)) return new RDAPlayerProgress();
                var data = JsonUtility.FromJson<RDAPlayerProgress>(File.ReadAllText(PathName));
                return data ?? new RDAPlayerProgress();
            }
            catch (Exception e)
            {
                Debug.LogError($"[RDA] Save load failed: {e.Message}");
                return new RDAPlayerProgress();
            }
        }

        public static bool Save(RDAPlayerProgress data)
        {
            if (data == null) return false;
            try
            {
                var temp = PathName + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (File.Exists(PathName)) File.Delete(PathName);
                File.Move(temp, PathName);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RDA] Save write failed: {e.Message}");
                return false;
            }
        }
    }
}