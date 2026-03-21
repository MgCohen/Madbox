using UnityEngine;
using UnityEngine.EventSystems;

namespace Madbox.App.GameView
{
    public struct InputContext
    {
        public Vector2 JoystickDrag;
        public PointerEventData PointerEventData;

        public bool HasJoystickInput()
        {
            return JoystickDrag.sqrMagnitude > 0.0001f;
        }
    }

    public interface IInputContextProvider
    {
        InputContext Current { get; }
        void SetJoystickDrag(Vector2 drag, PointerEventData eventData);
        void ClearJoystickDrag();
        void EndFrame();
    }
}
