using Madbox.App.GameView.Player;
using UnityEngine;

namespace Madbox.App.GameView.Input
{
    /// <summary>
    /// Scene wiring: forwards <see cref="VirtualJoystickInput.Direction"/> into <see cref="PlayerInputContext"/>.
    /// </summary>
    public sealed class VirtualJoystickPlayerInputProvider : PlayerInputProvider
    {
        [SerializeField]
        private VirtualJoystickInput joystick;

        public override PlayerInputContext GetInputContext()
        {
            Vector2 move = joystick != null ? joystick.Direction : Vector2.zero;
            return new PlayerInputContext(move);
        }
    }
}
