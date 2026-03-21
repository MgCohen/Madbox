using UnityEngine;
using UnityEngine.EventSystems;

namespace Madbox.App.GameView
{
    public sealed class VirtualJoystickInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Direction { get; private set; }

        public bool IsActive => Direction.sqrMagnitude > 0f;

        [SerializeField] private JoystickVisualController visualController;
        [SerializeField] private float deadZone = 0.1f;
        [SerializeField] private float maxRadiusOverride;

        private void Awake()
        {
            ResolveVisualController();
            ResetInput();
        }

        private void ResolveVisualController()
        {
            if (visualController == null)
            {
                visualController = GetComponent<JoystickVisualController>();
            }

            if (visualController == null)
            {
                visualController = gameObject.AddComponent<JoystickVisualController>();
            }

            visualController.AutoWire(transform as RectTransform);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            visualController.MoveRootToPointer(eventData);
            ResetInput();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (visualController == null) return;
            if (TryGetLocalPoint(eventData, out Vector2 localPoint) == false) return;
            SetDirectionFromLocalPoint(localPoint);
        }

        private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 localPoint)
        {
            if (visualController == null)
            {
                localPoint = Vector2.zero;
                return false;
            }

            return visualController.TryGetLocalPointInRoot(eventData, out localPoint);
        }

        private void SetDirectionFromLocalPoint(Vector2 localPoint)
        {
            Vector2 offset = localPoint;
            float radius = ResolveMaxRadius();
            Vector2 clampedOffset = Vector2.ClampMagnitude(offset, radius);
            Vector2 direction = radius <= 0f ? Vector2.zero : clampedOffset / radius;
            direction = ApplyDeadZone(direction);
            ApplyDirection(direction);
        }

        private void ApplyDirection(Vector2 direction)
        {
            Direction = direction;
            Vector2 offset = direction * ResolveMaxRadius();
            visualController.SetInnerOffset(offset);
        }

        private float ResolveMaxRadius()
        {
            if (visualController == null) return 0f;
            return visualController.ResolveMaxRadius(maxRadiusOverride);
        }

        private Vector2 ApplyDeadZone(Vector2 direction)
        {
            return direction.magnitude < deadZone ? Vector2.zero : direction;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetInput();
        }

        private void ResetInput()
        {
            Direction = Vector2.zero;
            visualController?.ResetInnerToInitialPosition();
        }
    }
}
