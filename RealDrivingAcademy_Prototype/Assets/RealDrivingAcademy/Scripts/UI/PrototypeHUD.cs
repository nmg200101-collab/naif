using UnityEngine;
using UnityEngine.UI;
using RealDrivingAcademy.Vehicle;
using RealDrivingAcademy.Training;

namespace RealDrivingAcademy.UI
{
    public class PrototypeHUD : MonoBehaviour
    {
        public RealisticCarController car;
        public DrivingViolationSystem violations;
        public DrivingLessonManager lesson;
        public Text speedText;
        public Text rpmText;
        public Text gearText;
        public Text scoreText;
        public Text lessonText;
        public Text lessonProgressText;
        public Text warningText;
        public Text systemStatusText;
        public Text driveDebugText;
        public bool showDriveDebug;

        CarInputState input;
        CarSystems systems;

        void Awake()
        {
            if (driveDebugText != null)
                driveDebugText.gameObject.SetActive(showDriveDebug);

            if (car != null)
            {
                input = car.GetComponent<CarInputState>();
                systems = car.GetComponent<CarSystems>();
            }
        }

        void OnEnable()
        {
            if (lesson != null) lesson.OnInstructionChanged += SetLesson;
            if (violations != null) violations.OnViolation += ShowViolation;
        }

        void OnDisable()
        {
            if (lesson != null) lesson.OnInstructionChanged -= SetLesson;
            if (violations != null) violations.OnViolation -= ShowViolation;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3) && driveDebugText != null)
            {
                showDriveDebug = !showDriveDebug;
                driveDebugText.gameObject.SetActive(showDriveDebug);
            }

            if (car != null)
            {
                if (speedText != null) speedText.text = Mathf.RoundToInt(car.speedKph) + " km/h";
                if (rpmText != null) rpmText.text = Mathf.RoundToInt(car.engineRpm) + " RPM";
                if (gearText != null) gearText.text = car.selectedGear < 0 ? "R" : car.selectedGear == 0 ? "N" : car.selectedGear.ToString();
            }
            if (scoreText != null && violations != null) scoreText.text = "Score: " + violations.score;

            if (lessonProgressText != null && lesson != null)
            {
                int shownStep = lesson.IsCompleted ? lesson.TotalSteps : Mathf.Min(lesson.TotalSteps, lesson.CompletedSteps + 1);
                lessonProgressText.text = lesson.IsCompleted
                    ? "Lesson 100%"
                    : "Lesson " + shownStep + "/" + lesson.TotalSteps;
            }

            if (systemStatusText != null && car != null)
            {
                if (input == null) input = car.GetComponent<CarInputState>();
                if (systems == null) systems = car.GetComponent<CarSystems>();

                string hb = input != null && input.handbrake ? "HB" : "";
                string left = systems != null && systems.leftIndicator ? "LEFT" : "";
                string right = systems != null && systems.rightIndicator ? "RIGHT" : "";
                string hazards = systems != null && systems.hazardLights ? "HAZ" : "";
                string lights = systems != null && (systems.lowBeams || systems.highBeams) ? "LIGHT" : "";
                string abs = car.absActive ? "ABS" : "";
                string tcs = car.tractionControlActive ? "TCS" : "";

                systemStatusText.text = string.Join("  ", new[] { hb, left, right, hazards, lights, abs, tcs });
            }

            if (driveDebugText != null && car != null)
            {
                if (input == null) input = car.GetComponent<CarInputState>();
                if (systems == null) systems = car.GetComponent<CarSystems>();

                int grounded = 0;
                if (car.frontAxle != null)
                {
                    if (car.frontAxle.leftCollider != null && car.frontAxle.leftCollider.isGrounded) grounded++;
                    if (car.frontAxle.rightCollider != null && car.frontAxle.rightCollider.isGrounded) grounded++;
                }
                if (car.rearAxle != null)
                {
                    if (car.rearAxle.leftCollider != null && car.rearAxle.leftCollider.isGrounded) grounded++;
                    if (car.rearAxle.rightCollider != null && car.rearAxle.rightCollider.isGrounded) grounded++;
                }

                float motorTorque = 0f;
                if (car.frontAxle != null)
                {
                    if (car.frontAxle.leftCollider != null) motorTorque += car.frontAxle.leftCollider.motorTorque;
                    if (car.frontAxle.rightCollider != null) motorTorque += car.frontAxle.rightCollider.motorTorque;
                }
                if (car.rearAxle != null)
                {
                    if (car.rearAxle.leftCollider != null) motorTorque += car.rearAxle.leftCollider.motorTorque;
                    if (car.rearAxle.rightCollider != null) motorTorque += car.rearAxle.rightCollider.motorTorque;
                }

                string ignition = systems != null && systems.ignitionOn ? "ON" : "OFF";
                string handbrake = input != null && input.handbrake ? "ON" : "OFF";
                float gas = input != null ? input.throttle : -1f;
                float brake = input != null ? input.brake : -1f;
                string gear = car.selectedGear < 0 ? "R" : car.selectedGear == 0 ? "N" : "D";

                driveDebugText.text =
                    $"IGN:{ignition}  GEAR:{gear}  HB:{handbrake}  GAS:{gas:0.00}  BRAKE:{brake:0.00}  GROUND:{grounded}/4  TORQUE:{motorTorque:0}";
            }
        }

        void SetLesson(string text)
        {
            if (lessonText != null) lessonText.text = text;
        }

        void ShowViolation(ViolationRecord record)
        {
            if (warningText != null) warningText.text = record.message + "  -" + record.points;
        }
    }
}
