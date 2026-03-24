using UnityEngine;

namespace Madbox.App.GameView.Input
{
    /// <summary>
    /// UI joystick output; other systems read <see cref="Direction"/> (normalized inside dead zone).
    /// </summary>
    public sealed class VirtualJoystickInput : MonoBehaviour
    {
        [SerializeField]
        [Range(0f, 1f)]
        private float deadZone = 0.05f;

        private Vector2 direction;

        public Vector2 Direction => direction;

        public void SetDirection(Vector2 value)
        {
            if (value.sqrMagnitude < deadZone * deadZone)
            {
                direction = Vector2.zero;
                return;
            }

            direction = value.sqrMagnitude > 1f ? value.normalized : value;
        }

        public void Clear()
        {
            direction = Vector2.zero;
        }
    }
}
