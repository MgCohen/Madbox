using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

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
            SetJoystickDirectionForTests(joystick, forward);
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

        [Test]
        public void Tick_WhenMoving_RotatesCharacterToFacingDirection()
        {
            GameObject hero = CreateHeroObject();
            GameObject joystickObject = CreateJoystickObject();
            PlayerMovementViewBehavior movement = ConfigureMovement(hero, joystickObject, out VirtualJoystickInput joystick);
            SetJoystickDirectionForTests(joystick, new Vector2(0f, 1f));
            movement.Tick(1f);
            Assert.Greater(hero.transform.forward.z, 0f);
            DestroyTestObjects(hero, joystickObject);
        }

        [Test]
        public void Tick_WhenJoystickHasDirection_PlaysRunAnimation()
        {
            GameObject hero = CreateHeroWithAnimator();
            PlayerAttackAnimationBehavior animationController = hero.AddComponent<PlayerAttackAnimationBehavior>();
            InvokeNonPublicAwake(animationController);
            animationController.SetMoving(true);
            int activeHash = GetCurrentAnimationHash(animationController);
            int runHash = Animator.StringToHash("Run");
            int fallbackHash = Animator.StringToHash("HeroMove");
            bool isRun = activeHash == runHash || activeHash == fallbackHash;
            Assert.IsTrue(isRun);
            Object.DestroyImmediate(hero);
        }

        [Test]
        public void OnPointerDown_WhenTouchStarts_RepositionsStickToTouchPoint()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1080f, 1920f);

            GameObject stickRootObject = new GameObject("StickRoot", typeof(RectTransform), typeof(VirtualJoystickInput));
            RectTransform stickRoot = stickRootObject.GetComponent<RectTransform>();
            stickRoot.SetParent(canvasRect, false);
            stickRoot.sizeDelta = new Vector2(240f, 240f);

            GameObject innerStickObject = new GameObject("InnerStick", typeof(RectTransform));
            RectTransform innerStick = innerStickObject.GetComponent<RectTransform>();
            innerStick.SetParent(stickRoot, false);
            innerStick.sizeDelta = new Vector2(80f, 80f);

            VirtualJoystickInput joystick = stickRootObject.GetComponent<VirtualJoystickInput>();
            InvokeNonPublicAwake(joystick);
            EventSystem eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            PointerEventData eventData = new PointerEventData(eventSystem);
            eventData.position = new Vector2(340f, 420f);
            eventData.pressPosition = eventData.position;

            joystick.OnPointerDown(eventData);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, null, out Vector2 expectedPosition);
            Assert.That(stickRoot.anchoredPosition.x, Is.EqualTo(expectedPosition.x).Within(0.01f));
            Assert.That(stickRoot.anchoredPosition.y, Is.EqualTo(expectedPosition.y).Within(0.01f));

            Object.DestroyImmediate(eventSystem.gameObject);
            Object.DestroyImmediate(stickRootObject);
            Object.DestroyImmediate(canvasObject);
        }

        [Test]
        public void OnPointerDown_WhenTouchIsOutsideCanvas_ClampsStickInsideParentBounds()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1080f, 1920f);

            GameObject stickRootObject = new GameObject("StickRoot", typeof(RectTransform), typeof(VirtualJoystickInput));
            RectTransform stickRoot = stickRootObject.GetComponent<RectTransform>();
            stickRoot.SetParent(canvasRect, false);
            stickRoot.sizeDelta = new Vector2(240f, 240f);

            GameObject innerStickObject = new GameObject("InnerStick", typeof(RectTransform));
            RectTransform innerStick = innerStickObject.GetComponent<RectTransform>();
            innerStick.SetParent(stickRoot, false);
            innerStick.sizeDelta = new Vector2(80f, 80f);

            VirtualJoystickInput joystick = stickRootObject.GetComponent<VirtualJoystickInput>();
            InvokeNonPublicAwake(joystick);

            EventSystem eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            PointerEventData eventData = new PointerEventData(eventSystem);
            eventData.position = new Vector2(-2000f, -2000f);
            eventData.pressPosition = eventData.position;

            joystick.OnPointerDown(eventData);

            float minX = canvasRect.rect.xMin - stickRoot.rect.xMin;
            float maxX = canvasRect.rect.xMax - stickRoot.rect.xMax;
            float minY = canvasRect.rect.yMin - stickRoot.rect.yMin;
            float maxY = canvasRect.rect.yMax - stickRoot.rect.yMax;

            Assert.That(stickRoot.anchoredPosition.x, Is.InRange(minX, maxX));
            Assert.That(stickRoot.anchoredPosition.y, Is.InRange(minY, maxY));

            Object.DestroyImmediate(eventSystem.gameObject);
            Object.DestroyImmediate(stickRootObject);
            Object.DestroyImmediate(canvasObject);
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

        private void InvokeNonPublicAwake(Object component)
        {
            MethodInfo awake = component.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            if (awake == null) return;
            awake.Invoke(component, null);
        }

        private void SetJoystickDirectionForTests(VirtualJoystickInput joystick, Vector2 direction)
        {
            PropertyInfo directionProperty = typeof(VirtualJoystickInput).GetProperty("Direction", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setMethod = directionProperty.GetSetMethod(nonPublic: true);
            setMethod.Invoke(joystick, new object[] { direction });
        }

        private int GetCurrentAnimationHash(PlayerAttackAnimationBehavior animationController)
        {
            FieldInfo currentStateHashField = typeof(PlayerAttackAnimationBehavior).GetField("currentStateHash", BindingFlags.Instance | BindingFlags.NonPublic);
            object value = currentStateHashField.GetValue(animationController);
            return (int)value;
        }
    }
}
