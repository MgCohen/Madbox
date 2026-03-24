using Madbox.App.Animation;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.App.Animation.Tests
{
    public sealed class AnimationEventDefinitionTests
    {
        [Test]
        public void EventId_MatchesObjectName()
        {
            AnimationEventDefinition def = ScriptableObject.CreateInstance<AnimationEventDefinition>();
            def.name = "attack_release";

            Assert.AreEqual("attack_release", def.EventId);
            Assert.AreEqual("attack_release", def.name);
        }
    }
}
