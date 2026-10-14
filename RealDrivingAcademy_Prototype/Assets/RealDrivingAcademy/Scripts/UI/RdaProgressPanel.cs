using UnityEngine;
using UnityEngine.UI;
using RealDrivingAcademy.Core;

namespace RealDrivingAcademy.UI
{
    public class RdaProgressPanel : MonoBehaviour
    {
        public Text summaryText;

        void OnEnable()
        {
            Refresh();

            var persistence = RdaGameManager.Instance != null
                ? RdaGameManager.Instance.Persistence
                : null;

            if (persistence != null)
                persistence.ProgressChanged += Refresh;
        }

        void OnDisable()
        {
            var persistence = RdaGameManager.Instance != null
                ? RdaGameManager.Instance.Persistence
                : null;

            if (persistence != null)
                persistence.ProgressChanged -= Refresh;
        }

        public void Refresh()
        {
            if (summaryText == null) return;

            var persistence = RdaGameManager.Instance != null
                ? RdaGameManager.Instance.Persistence
                : null;

            if (persistence == null)
            {
                summaryText.text = "Progress data unavailable.";
                return;
            }

            var progress = persistence.Progress;
            int completedLessons = progress.completedLessonIds != null
                ? progress.completedLessonIds.Count
                : 0;

            bool basicComplete = progress.IsLessonComplete(RdaTrainingMenuPanel.BasicLessonId);

            summaryText.text =
                "Learner Progress\n\n" +
                "Completed lessons: " + completedLessons + "\n" +
                "Basic driving lesson: " + (basicComplete ? "Completed" : "Not completed") + "\n" +
                "Best practical score: " + progress.bestPracticalScore + "/100\n" +
                "Best theory score: " + progress.bestTheoryScore + "/100\n" +
                "Selected vehicle: " + progress.selectedVehicleId;
        }
    }
}
