using UnityEngine;
using UnityEngine.EventSystems;

namespace Madbox.App.GameView.Input
{
    /// <summary>
    /// Receives pointer events on a full-screen touch area, positions the joystick, updates <see cref="VirtualJoystickInput"/>,
    /// and performs optional swipe detection (log-only for now).
    /// </summary>
    public sealed class TouchInputRouter : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField]
        private RectTransform touchArea;

        [SerializeField]
        private JoystickVisualController joystickVisuals;

        [SerializeField]
        private VirtualJoystickInput joystick;

        [SerializeField]
        [Tooltip("When zero or negative, uses half of the smaller joystick root axis.")]
        private float maxRadiusOverride;

        [SerializeField]
        private float minSwipeDistancePixels = 80f;

        [SerializeField]
        private float maxSwipeDurationSeconds = 0.5f;

        private int activePointerId = -1;

        private Vector2 pointerDownScreen;

        private float pointerDownTime;

        private bool joystickDragOccurred;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId >= 0) return;
            activePointerId = eventData.pointerId;
            pointerDownScreen = eventData.position;
            pointerDownTime = Time.unscaledTime;
            joystickDragOccurred = false;
            UpdateJoystick(eventData, placeJoystickRoot: true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            UpdateJoystick(eventData, placeJoystickRoot: false);
        }

        private void UpdateJoystick(PointerEventData eventData, bool placeJoystickRoot)
        {
            if (touchArea == null || joystickVisuals == null || joystick == null) return;
            JoystickDragMath.ApplyPointerDrag(touchArea, joystickVisuals, joystick, maxRadiusOverride, ref joystickDragOccurred, eventData, placeJoystickRoot);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            FinishPointer(eventData);
        }

        private void FinishPointer(PointerEventData eventData)
        {
            if (!joystickDragOccurred)
            {
                float dt = Time.unscaledTime - pointerDownTime;
                float dist = Vector2.Distance(eventData.position, pointerDownScreen);
                if (dt <= maxSwipeDurationSeconds && dist >= minSwipeDistancePixels) Debug.Log("TouchInputRouter: swipe detected (no gameplay binding yet).");
            }
            joystick.Clear();
            if (joystickVisuals != null && joystickVisuals.InnerStick != null) joystickVisuals.InnerStick.anchoredPosition = Vector2.zero;
            activePointerId = -1;
        }

        private static class JoystickDragMath
        {
            public static void ApplyPointerDrag(RectTransform touchArea, JoystickVisualController visuals, VirtualJoystickInput joystickInput, float maxRadiusOverride, ref bool dragOccurred, PointerEventData eventData, bool placeJoystickRoot)
            {
                RectTransform root = visuals.StickRoot;
                RectTransform inner = visuals.InnerStick;
                if (root == null || inner == null) return;
                Camera cam = GetCanvasCamera(touchArea);
                if (placeJoystickRoot)
                {
                    PositionRootAtPointer(touchArea, eventData, root, cam);
                    ClampChildInsideParent(touchArea, root);
                }
                Vector2 dir = ComputeDirection(eventData, root, inner, cam, maxRadiusOverride);
                joystickInput.SetDirection(dir);
                if (dir.sqrMagnitude > 0.0001f) dragOccurred = true;
            }

            private static Camera GetCanvasCamera(RectTransform touchArea)
            {
                Canvas canvas = touchArea != null ? touchArea.GetComponentInParent<Canvas>() : null;
                if (canvas == null) return null;
                return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            }

            private static void PositionRootAtPointer(RectTransform touchArea, PointerEventData eventData, RectTransform root, Camera cam)
            {
                Vector3 worldPoint;
                RectTransformUtility.ScreenPointToWorldPointInRectangle(touchArea, eventData.position, cam, out worldPoint);
                root.position = worldPoint;
            }

            private static void ClampChildInsideParent(RectTransform parent, RectTransform child)
            {
                Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, child);
                Rect pr = parent.rect;
                child.anchoredPosition += BuildCorrection(pr, bounds);
            }

            private static Vector2 BuildCorrection(Rect parentRect, Bounds bounds)
            {
                Vector2 correction = Vector2.zero;
                if (bounds.min.x < parentRect.xMin) correction.x += parentRect.xMin - bounds.min.x;
                if (bounds.max.x > parentRect.xMax) correction.x -= bounds.max.x - parentRect.xMax;
                if (bounds.min.y < parentRect.yMin) correction.y += parentRect.yMin - bounds.min.y;
                if (bounds.max.y > parentRect.yMax) correction.y -= bounds.max.y - parentRect.yMax;
                return correction;
            }

            private static Vector2 ComputeDirection(PointerEventData eventData, RectTransform root, RectTransform inner, Camera cam, float maxRadiusOverride)
            {
                Vector2 localInRoot;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, eventData.position, cam, out localInRoot);
                Vector2 center = root.rect.center;
                Vector2 offset = localInRoot - center;
                float maxRadius = ResolveMaxRadius(root, maxRadiusOverride);
                if (offset.sqrMagnitude > maxRadius * maxRadius) offset = offset.normalized * maxRadius;
                inner.anchoredPosition = offset;
                return maxRadius > 0.0001f ? offset / maxRadius : Vector2.zero;
            }

            private static float ResolveMaxRadius(RectTransform root, float maxRadiusOverride)
            {
                if (maxRadiusOverride > 0f) return maxRadiusOverride;
                Rect r = root.rect;
                return Mathf.Min(r.width, r.height) * 0.5f;
            }
        }
    }
}
