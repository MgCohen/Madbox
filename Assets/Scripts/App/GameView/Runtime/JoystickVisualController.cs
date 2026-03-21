using UnityEngine;
using UnityEngine.EventSystems;

namespace Madbox.App.GameView
{
    public sealed class JoystickVisualController : MonoBehaviour
    {
        [SerializeField] private RectTransform stickRoot;
        [SerializeField] private RectTransform innerStick;

        private Vector2 initialInnerPosition;
        private bool initialized;

        public void AutoWire(RectTransform fallbackRoot)
        {
            if (initialized) return;

            if (stickRoot == null)
            {
                stickRoot = fallbackRoot != null ? fallbackRoot : transform as RectTransform;
            }

            if (innerStick == null && stickRoot != null && stickRoot.childCount > 0)
            {
                innerStick = stickRoot.GetChild(0) as RectTransform;
            }

            initialInnerPosition = innerStick == null ? Vector2.zero : innerStick.anchoredPosition;
            initialized = true;
        }

        public void MoveRootToPointer(PointerEventData eventData)
        {
            if (stickRoot == null) return;
            RectTransform parent = stickRoot.parent as RectTransform;
            if (parent == null) return;

            bool gotPoint = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);
            if (gotPoint == false) return;

            stickRoot.anchoredPosition = ClampRootToParentBounds(parent, localPoint);
        }

        public bool TryGetLocalPointInRoot(PointerEventData eventData, out Vector2 localPoint)
        {
            if (stickRoot == null)
            {
                localPoint = Vector2.zero;
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                stickRoot,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint);
        }

        public float ResolveMaxRadius(float maxRadiusOverride)
        {
            if (maxRadiusOverride > 0f) return maxRadiusOverride;
            if (stickRoot == null || innerStick == null) return 0f;

            float rootRadius = stickRoot.rect.width * 0.5f;
            float innerRadius = innerStick.rect.width * 0.5f;
            return Mathf.Max(0f, rootRadius - innerRadius);
        }

        public void SetInnerOffset(Vector2 offset)
        {
            if (innerStick == null) return;
            innerStick.anchoredPosition = initialInnerPosition + offset;
        }

        public void ResetInnerToInitialPosition()
        {
            if (innerStick == null) return;
            innerStick.anchoredPosition = initialInnerPosition;
        }

        private Vector2 ClampRootToParentBounds(RectTransform parent, Vector2 desiredAnchoredPosition)
        {
            float minX = parent.rect.xMin - stickRoot.rect.xMin;
            float maxX = parent.rect.xMax - stickRoot.rect.xMax;
            float minY = parent.rect.yMin - stickRoot.rect.yMin;
            float maxY = parent.rect.yMax - stickRoot.rect.yMax;

            float clampedX = Mathf.Clamp(desiredAnchoredPosition.x, minX, maxX);
            float clampedY = Mathf.Clamp(desiredAnchoredPosition.y, minY, maxY);
            return new Vector2(clampedX, clampedY);
        }
    }
}
