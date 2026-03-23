using System.Linq;
using Madbox.App.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Madbox.App.GameView.Tests
{
    /// <summary>
    /// Regression: hero animator defines parameters used by <see cref="AnimationController"/> wrappers.
    /// </summary>
    public sealed class HeroAnimationControllerLocomotionTests
    {
        private const string HeroControllerPath = "Assets/Art/Animations/Hero/HeroAnimationController.controller";

        [Test]
        public void HeroAnimationController_DefinesMovingBoolParameter()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(HeroControllerPath);
            Assert.IsNotNull(controller, $"Missing asset at {HeroControllerPath}");

            const string movingParameterName = "Moving";
            AnimatorControllerParameter moving = controller.parameters.FirstOrDefault(p => p.name == movingParameterName);
            Assert.IsNotNull(moving, $"Animator controller must define bool parameter '{movingParameterName}' for locomotion.");
            Assert.AreEqual(AnimatorControllerParameterType.Bool, moving.type);
        }

        [Test]
        public void HeroAnimationController_DefinesAttackingBoolParameter()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(HeroControllerPath);
            Assert.IsNotNull(controller, $"Missing asset at {HeroControllerPath}");

            const string attackingParameterName = "attacking";
            AnimatorControllerParameter attacking = controller.parameters.FirstOrDefault(p => p.name == attackingParameterName);
            Assert.IsNotNull(attacking, $"Animator controller must define bool parameter '{attackingParameterName}' for attacks.");
            Assert.AreEqual(AnimatorControllerParameterType.Bool, attacking.type);
        }
    }
}
