using UnityEngine;
using UnityEngine.UI;
using RealDrivingAcademy.Core;

namespace RealDrivingAcademy.Training
{
    public class TrainingResultController : MonoBehaviour
    {
        public string lessonId = "training_basic_01";
        [Range(0, 100)] public int passingScore = 70;

        public DrivingLessonManager lesson;
        public DrivingViolationSystem violations;
        public Text resultText;

        public bool completed;
        public bool passed;
        public int finalScore;
        public string finalGrade;

        void OnEnable()
        {
            if (lesson != null)
                lesson.OnLessonCompleted += HandleLessonCompleted;
        }

        void OnDisable()
        {
            if (lesson != null)
                lesson.OnLessonCompleted -= HandleLessonCompleted;
        }

        void HandleLessonCompleted()
        {
            if (completed) return;
            completed = true;

            finalScore = violations != null ? violations.score : 100;
            finalGrade = violations != null ? violations.Grade : "A";
            passed = finalScore >= passingScore;

            var persistence = RdaGameManager.Instance != null
                ? RdaGameManager.Instance.Persistence
                : null;

            if (persistence != null)
                persistence.RecordPracticalResult(lessonId, finalScore, passed);

            if (resultText != null)
            {
                resultText.text = passed
                    ? "Lesson Passed  Score: " + finalScore + "  Grade: " + finalGrade
                    : "Lesson Complete  Score: " + finalScore + "  Grade: " + finalGrade + "  Try Again";
            }
        }
    }
}
