using System;
using System.Collections.Generic;

namespace RealDrivingAcademy.Persistence
{
    [Serializable]
    public sealed class RdaProgressData
    {
        public string selectedVehicleId = "training_sedan";
        public List<string> completedLessonIds = new List<string>();
        public int bestTheoryScore;
        public int bestPracticalScore;

        public bool IsLessonComplete(string lessonId)
        {
            return !string.IsNullOrWhiteSpace(lessonId)
                && completedLessonIds != null
                && completedLessonIds.Contains(lessonId);
        }

        public bool MarkLessonComplete(string lessonId)
        {
            if (string.IsNullOrWhiteSpace(lessonId))
                return false;

            if (completedLessonIds == null)
                completedLessonIds = new List<string>();

            if (completedLessonIds.Contains(lessonId))
                return false;

            completedLessonIds.Add(lessonId);
            return true;
        }
    }
}
