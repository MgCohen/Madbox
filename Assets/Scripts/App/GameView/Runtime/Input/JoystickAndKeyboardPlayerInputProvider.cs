using Madbox.App.GameView.Player;
using UnityEngine;

namespace Madbox.App.GameView.Input
{
    /// <summary>
    /// Resolves <see cref="PlayerInputContext"/> from a <see cref="VirtualJoystickInput"/> when active, otherwise from keyboard axes (WASD / arrows).
    /// </summary>
    public sealed class JoystickAndKeyboardPlayerInputProvider : PlayerInputProvider
    {
        [SerializeField]
        private VirtualJoystickInput joystick;

        public override PlayerInputContext GetInputContext()
        {
            if (joystick != null && joystick.Direction.sqrMagnitude > 0.0001f)
            {
                return new PlayerInputContext(joystick.Direction);
            }

            float x = UnityEngine.Input.GetAxisRaw("Horizontal");
            float y = UnityEngine.Input.GetAxisRaw("Vertical");
            Vector2 kb = new Vector2(x, y);
            Vector2 move = kb.sqrMagnitude > 1f ? kb.normalized : kb;
            return new PlayerInputContext(move);
        }
    }
}
