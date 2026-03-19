using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.App.GameView.Tests
{
    public sealed class CharacterVisualBehaviorsTests
    {
        [Test]
        public void Tick_WhenJoystickHasDirection_MovesPlayerForward()
        {
            GameObject hero = CreateHeroObject();
            GameObject joystickObject = CreateJoystickObject();
            PlayerMovementViewBehavior movement = ConfigureMovement(hero, joystickObject, out VirtualJoystickInput joystick);
            Vector2 forward = new Vector2(0f, 1f);
            joystick.SetDirectionForTests(forward);
            movement.Tick(1f);
            Assert.Less(hero.transform.position.z, 0f);
            DestroyTestObjects(hero, joystickObject);
        }

        [Test]
        public void TriggerAttack_WhenAnimatorExists_LocksAttackWindow()
        {
            GameObject hero = CreateHeroWithAnimator();
            PlayerAttackAnimationBehavior attackBehavior = hero.AddComponent<PlayerAttackAnimationBehavior>();
            InvokeNonPublicAwake(attackBehavior);
            attackBehavior.TriggerAttack();
            Assert.IsTrue(attackBehavior.IsAttackLocked);
            Object.DestroyImmediate(hero);
        }

        private GameObject CreateHeroObject()
        {
            return new GameObject("Hero");
        }

        private GameObject CreateJoystickObject()
        {
            return new GameObject("Joystick");
        }

        private PlayerMovementViewBehavior ConfigureMovement(GameObject hero, GameObject joystickObject, out VirtualJoystickInput joystick)
        {
            joystick = joystickObject.AddComponent<VirtualJoystickInput>();
            PlayerMovementViewBehavior movement = hero.AddComponent<PlayerMovementViewBehavior>();
            movement.SetJoystick(joystick);
            return movement;
        }

        private void DestroyTestObjects(GameObject hero, GameObject joystickObject)
        {
            Object.DestroyImmediate(hero);
            Object.DestroyImmediate(joystickObject);
        }

        private GameObject CreateHeroWithAnimator()
        {
            GameObject hero = new GameObject("Hero");
            Animator animator = hero.AddComponent<Animator>();
            animator.runtimeAnimatorController = LoadHeroController();
            return hero;
        }

        private RuntimeAnimatorController LoadHeroController()
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Animations/Hero/HeroAnimationController.controller");
        }

        private void InvokeNonPublicAwake(PlayerAttackAnimationBehavior attackBehavior)
        {
            MethodInfo awake = typeof(PlayerAttackAnimationBehavior).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(attackBehavior, null);
        }
    }
}
