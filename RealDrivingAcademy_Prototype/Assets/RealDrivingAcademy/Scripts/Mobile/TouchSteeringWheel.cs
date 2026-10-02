using UnityEngine;
using UnityEngine.EventSystems;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.Mobile
{
    public class TouchSteeringWheel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public CarInputState input;
        public RectTransform wheelGraphic;
        [Range(90f, 540f)] public float maxVisualDegrees = 250f;
        [Range(0.5f, 3f)] public float sensitivity = 1.25f;
        public bool autoCenter = true;
        public float centerSpeed = 2.5f;

        RectTransform rect;
        float steer;
        bool dragging;

        void Awake()
        {
            rect = transform as RectTransform;
            if (wheelGraphic == null) wheelGraphic = rect;
        }

        void Update()
        {
            if (!dragging && autoCenter)
            {
                steer = Mathf.MoveTowards(steer, 0f, centerSpeed * Time.unscaledDeltaTime);
                Apply();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            dragging = true;
            UpdateSteer(eventData);
        }

        public void OnDrag(PointerEventData eventData) => UpdateSteer(eventData);

        public void OnPointerUp(PointerEventData eventData)
        {
            dragging = false;
            if (!autoCenter) Apply();
        }

        void UpdateSteer(PointerEventData data)
        {
            if (rect == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out Vector2 local);
            float radius = Mathf.Max(1f, rect.rect.width * 0.5f);
            steer = Mathf.Clamp((local.x / radius) * sensitivity, -1f, 1f);
            Apply();
        }

        void Apply()
        {
            if (input != null) input.SetSteering(steer);
            if (wheelGraphic != null)
                wheelGraphic.localRotation = Quaternion.Euler(0f, 0f, -steer * maxVisualDegrees);
        }
    }
}
