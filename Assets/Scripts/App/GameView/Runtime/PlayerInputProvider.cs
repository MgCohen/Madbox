using UnityEngine;
using UnityEngine.EventSystems;

namespace Madbox.App.GameView
{
    public sealed class PlayerInputProvider : MonoBehaviour, IInputContextProvider
    {
        public InputContext Current => current;

        private InputContext current;

        public void SetJoystickDrag(Vector2 drag, PointerEventData eventData)
        {
            current.JoystickDrag = Vector2.ClampMagnitude(drag, 1f);
            current.PointerEventData = eventData;
        }

        public void ClearJoystickDrag()
        {
            current.JoystickDrag = Vector2.zero;
            current.PointerEventData = null;
        }

        public void EndFrame()
        {
        }
    }
}
