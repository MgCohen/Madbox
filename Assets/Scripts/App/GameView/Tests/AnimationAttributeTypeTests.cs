using Madbox.App.GameView.Animation;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.App.GameView.Tests
{
    /// <summary>
    /// Regression: <see cref="AnimationAttribute"/> is usable as a ScriptableObject (authored assets share the
    /// <see cref="AnimationController"/> script file; <see cref="AssetDatabase.LoadAssetAtPath{T}"/> may not resolve them).
    /// </summary>
    public sealed class AnimationAttributeTypeTests
    {
        [Test]
        public void AnimationAttribute_ParameterName_MatchesObjectName()
        {
            AnimationAttribute attr = ScriptableObject.CreateInstance<AnimationAttribute>();
            Assert.IsNotNull(attr);

            attr.name = "Moving";

            Assert.AreEqual("Moving", attr.ParameterName);
            Assert.AreEqual("Moving", attr.name);
        }
    }
}
