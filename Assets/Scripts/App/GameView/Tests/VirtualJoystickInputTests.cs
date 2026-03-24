using Madbox.App.GameView.Input;
using Madbox.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Madbox.App.GameView.Tests
{
    public sealed class VirtualJoystickInputTests
    {
        [Test]
        public void SetDirection_BelowDeadZone_ProducesZeroDirection()
        {
            GameObject go = new GameObject("vj");
            VirtualJoystickInput vj = go.AddComponent<VirtualJoystickInput>();

            vj.SetDirection(new Vector2(0.01f, 0f));

            Assert.That(vj.Direction, Is.EqualTo(Vector2.zero));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetDirection_AboveDeadZone_PreservesVector()
        {
            GameObject go = new GameObject("vj");
            VirtualJoystickInput vj = go.AddComponent<VirtualJoystickInput>();

            Vector2 input = new Vector2(0.5f, 0.5f);
            vj.SetDirection(input);

            Assert.That(vj.Direction.x, Is.EqualTo(input.x).Within(0.0001f));
            Assert.That(vj.Direction.y, Is.EqualTo(input.y).Within(0.0001f));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void VirtualJoystickPlayerInputProvider_ForwardsJoystickDirection()
        {
            GameObject vjGo = new GameObject("vj");
            VirtualJoystickInput vj = vjGo.AddComponent<VirtualJoystickInput>();

            GameObject providerGo = new GameObject("provider");
            VirtualJoystickPlayerInputProvider provider = providerGo.AddComponent<VirtualJoystickPlayerInputProvider>();

            SerializedObject so = new SerializedObject(provider);
            so.FindProperty("joystick").objectReferenceValue = vj;
            so.ApplyModifiedPropertiesWithoutUndo();

            vj.SetDirection(new Vector2(1f, 0f));

            PlayerInputContext ctx = provider.GetInputContext();
            Assert.That(ctx.MoveDirection.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(ctx.MoveDirection.y, Is.EqualTo(0f).Within(0.0001f));

            Object.DestroyImmediate(providerGo);
            Object.DestroyImmediate(vjGo);
        }

        [Test]
        public void JoystickAndKeyboardPlayerInputProvider_UsesSceneJoystickWhenReferenceMissing()
        {
            GameObject vjGo = new GameObject("vj");
            VirtualJoystickInput vj = vjGo.AddComponent<VirtualJoystickInput>();
            vj.SetDirection(new Vector2(0.8f, 0.1f));

            GameObject providerGo = new GameObject("provider");
            JoystickAndKeyboardPlayerInputProvider provider = providerGo.AddComponent<JoystickAndKeyboardPlayerInputProvider>();

            PlayerInputContext ctx = provider.GetInputContext();
            Assert.That(ctx.MoveDirection.x, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(ctx.MoveDirection.y, Is.EqualTo(0.1f).Within(0.0001f));

            Object.DestroyImmediate(providerGo);
            Object.DestroyImmediate(vjGo);
        }
    }
}
