using UnityEngine;
using UnityEngine.EventSystems;

namespace Madbox.App.GameView
{
    public sealed class TouchInputRouter : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private PlayerInputProvider inputProvider;
        [SerializeField] private VirtualJoystickInput joystick;
        [SerializeField] private float minSwipeDistancePixels = 80f;
        [SerializeField] private float maxSwipeDurationSeconds = 0.5f;

        private int? activeJoystickPointerId;
        private Vector2 swipeStartScreen;
        private float swipeStartTime;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (TryIgnorePointerDown(eventData)) return;
            TryBeginJoystickPointer(eventData);
            if (joystick == null || inputProvider == null) return;
            joystick.OnPointerDown(eventData);
            inputProvider.SetJoystickDrag(joystick.Direction, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (activeJoystickPointerId.HasValue == false || eventData.pointerId != activeJoystickPointerId.Value)
            {
                return;
            }

            if (joystick == null || inputProvider == null) return;

            joystick.OnDrag(eventData);
            inputProvider.SetJoystickDrag(joystick.Direction, eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (activeJoystickPointerId.HasValue == false || eventData.pointerId != activeJoystickPointerId.Value)
            {
                return;
            }

            TryDetectSwipe(eventData);

            if (joystick != null)
            {
                joystick.OnPointerUp(eventData);
            }

            inputProvider?.ClearJoystickDrag();
            activeJoystickPointerId = null;
        }

        private bool TryIgnorePointerDown(PointerEventData eventData)
        {
            return activeJoystickPointerId.HasValue && eventData.pointerId != activeJoystickPointerId.Value;
        }

        private void TryBeginJoystickPointer(PointerEventData eventData)
        {
            if (activeJoystickPointerId.HasValue) return;
            activeJoystickPointerId = eventData.pointerId;
            swipeStartScreen = eventData.position;
            swipeStartTime = Time.unscaledTime;
        }

        private void TryDetectSwipe(PointerEventData eventData)
        {
            float duration = Time.unscaledTime - swipeStartTime;
            float distance = Vector2.Distance(swipeStartScreen, eventData.position);
            if (distance < minSwipeDistancePixels || duration > maxSwipeDurationSeconds || duration <= 0.0001f)
            {
                return;
            }

            Debug.Log($"[TouchInputRouter] Swipe detected: distance={distance:F1}px duration={duration:F3}s");
        }
    }
}
