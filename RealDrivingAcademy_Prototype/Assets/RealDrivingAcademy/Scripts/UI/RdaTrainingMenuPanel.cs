using UnityEngine;
using UnityEngine.UI;
using RealDrivingAcademy.Core;

namespace RealDrivingAcademy.UI
{
    public class RdaTrainingMenuPanel : MonoBehaviour
    {
        public const string BasicLessonId = "training_basic_01";
        public Text statusText;

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
            if (statusText == null) return;

            var persistence = RdaGameManager.Instance != null
                ? RdaGameManager.Instance.Persistence
                : null;

            if (persistence == null)
            {
                statusText.text = "Basic Driving Lesson\nProgress unavailable.";
                return;
            }

            var progress = persistence.Progress;
            bool completed = progress.IsLessonComplete(BasicLessonId);

            statusText.text =
                "Basic Driving Lesson\n" +
                (completed ? "Status: Completed" : "Status: Not completed") + "\n" +
                "Best practical score: " + progress.bestPracticalScore + "/100";
        }
    }
}
