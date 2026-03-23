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
    }
}
