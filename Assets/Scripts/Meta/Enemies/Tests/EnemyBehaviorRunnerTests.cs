using NUnit.Framework;
using UnityEngine;

namespace Madbox.Enemies.Tests
{
    public sealed class EnemyBehaviorRunnerTests
    {
        [Test]
        public void RunFrame_WhenFirstBehaviorClaims_DoesNotRunSecond()
        {
            var first = new CountingBehavior { ReturnValue = true };
            var second = new CountingBehavior { ReturnValue = true };
            IEnemyActorBehavior[] list = { first, second };
            var context = new EnemyBehaviorTickContext();

            EnemyBehaviorRunner.RunFrame(list, ref context, 0.016f);

            Assert.AreEqual(1, first.ExecuteCount);
            Assert.AreEqual(0, second.ExecuteCount);
        }

        [Test]
        public void RunFrame_WhenFirstDeclaresSecondClaims_RunsSecond()
        {
            var first = new CountingBehavior { ReturnValue = false };
            var second = new CountingBehavior { ReturnValue = true };
            IEnemyActorBehavior[] list = { first, second };
            var context = new EnemyBehaviorTickContext();

            EnemyBehaviorRunner.RunFrame(list, ref context, 0.016f);

            Assert.AreEqual(1, first.ExecuteCount);
            Assert.AreEqual(1, second.ExecuteCount);
        }

        [Test]
        public void RunFrame_WhenDeltaTimeNotPositive_DoesNothing()
        {
            var first = new CountingBehavior { ReturnValue = true };
            IEnemyActorBehavior[] list = { first };
            var context = new EnemyBehaviorTickContext();

            EnemyBehaviorRunner.RunFrame(list, ref context, 0f);

            Assert.AreEqual(0, first.ExecuteCount);
        }

        private sealed class CountingBehavior : IEnemyActorBehavior
        {
            public int ExecuteCount;
            public bool ReturnValue;

            public bool TryExecute(ref EnemyBehaviorTickContext context, float deltaTime)
            {
                ExecuteCount++;
                return ReturnValue;
            }
        }
    }
}
