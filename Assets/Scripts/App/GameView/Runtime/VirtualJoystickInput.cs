using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Madbox.App.GameView
{
    public sealed class VirtualJoystickInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Direction { get; private set; }

        public bool IsActive => Direction.sqrMagnitude > 0f;

        [SerializeField] private RectTransform stickRoot;
        [SerializeField] private RectTransform innerStick;
        [SerializeField] private float deadZone = 0.1f;
        [SerializeField] private float maxRadiusOverride;

        private Vector2 initialInnerPosition;
        private Vector2 initialInnerCenterPosition;
        private Vector2 innerCenterOffset;
        private Vector2 stickCenterOffset;  

        private void Awake()
        {
            ResolveReferences();
            CacheGeometry();
            ResetInput();
        }

        private void ResolveReferences()
        {
            if (stickRoot == null) stickRoot = transform as RectTransform;
            if (innerStick != null) return;
            if (stickRoot == null || stickRoot.childCount == 0) return;
            innerStick = stickRoot.GetChild(0) as RectTransform;
        }

        private void CacheGeometry()
        {
            innerCenterOffset = ResolveCenterOffset(innerStick);
            stickCenterOffset = ResolveCenterOffset(stickRoot);
            initialInnerPosition = innerStick == null ? Vector2.zero : innerStick.anchoredPosition;
            initialInnerCenterPosition = initialInnerPosition + innerCenterOffset;
        }

        private float ResolveMaxRadius()
        {
            if (maxRadiusOverride > 0f) return maxRadiusOverride;
            if (stickRoot == null || innerStick == null) return 0f;
            float rootRadius = stickRoot.rect.width * 0.5f;
            float innerRadius = innerStick.rect.width * 0.5f;
            return Mathf.Max(0f, rootRadius - innerRadius);
        }

        private Vector2 ResolveCenterOffset(RectTransform rectTransform)
        {
            if (rectTransform == null) return Vector2.zero;
            Vector2 size = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;
            return new Vector2((0.5f - pivot.x) * size.x, (0.5f - pivot.y) * size.y);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            MoveStickToPointer(eventData);
            ResetInput();
        }

        private void MoveStickToPointer(PointerEventData eventData)
        {
            stickRoot.transform.position = eventData.pressPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (TryGetLocalPoint(eventData, out Vector2 localPoint) == false) return;
            SetDirectionFromLocalPoint(localPoint);
        }

        private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 localPoint)
        {
            if (stickRoot == null) { localPoint = Vector2.zero; return false; }
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(stickRoot, eventData.position, eventData.pressEventCamera, out localPoint);
        }

        private void SetDirectionFromLocalPoint(Vector2 localPoint)
        {
            Vector2 offset = localPoint - initialInnerCenterPosition;
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
            Vector2 centerPosition = initialInnerCenterPosition + offset;
            UpdateInnerStickPosition(centerPosition - innerCenterOffset);
        }

        private void UpdateInnerStickPosition(Vector2 anchoredPosition)
        {
            if (innerStick == null) return;
            innerStick.anchoredPosition = anchoredPosition;
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
            UpdateInnerStickPosition(initialInnerPosition);
        }
    }
}
