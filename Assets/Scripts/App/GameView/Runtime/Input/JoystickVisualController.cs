using UnityEngine;

namespace Madbox.App.GameView.Input
{
    /// <summary>
    /// Holds references to the joystick root and inner stick for layout; routing logic lives in <see cref="TouchInputRouter"/>.
    /// </summary>
    public sealed class JoystickVisualController : MonoBehaviour
    {
        public RectTransform StickRoot => stickRoot;

        public RectTransform InnerStick => innerStick;

        [SerializeField]
        private RectTransform stickRoot;

        [SerializeField]
        private RectTransform innerStick;

        private Vector2 defaultStickRootAnchoredPosition;

        private bool defaultLayoutCaptured;

        private void Awake()
        {
            CaptureDefaultLayoutIfNeeded();
        }

        /// <summary>
        /// Records <see cref="StickRoot"/> layout once (prefab / scene position) so it can be restored after pointer-driven placement.
        /// </summary>
        public void CaptureDefaultLayoutIfNeeded()
        {
            if (stickRoot == null || defaultLayoutCaptured)
            {
                return;
            }

            defaultStickRootAnchoredPosition = stickRoot.anchoredPosition;
            defaultLayoutCaptured = true;
        }

        /// <summary>
        /// Restores the joystick root and inner stick to their layout positions (before pointer-driven placement).
        /// </summary>
        public void ResetToDefaultLayout()
        {
            if (innerStick != null)
            {
                innerStick.anchoredPosition = Vector2.zero;
            }

            if (stickRoot != null && defaultLayoutCaptured)
            {
                stickRoot.anchoredPosition = defaultStickRootAnchoredPosition;
            }
        }
    }
}
