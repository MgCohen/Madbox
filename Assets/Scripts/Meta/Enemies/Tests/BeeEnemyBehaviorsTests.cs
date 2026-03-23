using NUnit.Framework;
using UnityEngine;

namespace Madbox.Enemies.Tests
{
    public sealed class BeeEnemyBehaviorsTests
    {
        [Test]
        public void DashAttack_WhenTargetInsideRange_ClaimsAndMovesAlongX()
        {
            var enemyGo = new GameObject("Enemy");
            var playerGo = new GameObject("Player");
            enemyGo.transform.position = Vector3.zero;
            playerGo.transform.position = new Vector3(3f, 0f, 0f);

            var attack = new BeeDashAttackEnemyBehavior(5f, 1f, 0.2f, 10f, false, 0f);
            var chase = new BeeChaseEnemyBehavior(1f, false);
            IEnemyActorBehavior[] stack = { attack, chase };
            var context = new EnemyBehaviorTickContext
            {
                Self = enemyGo.transform,
                Target = playerGo.transform,
                Body = null
            };

            EnemyBehaviorRunner.RunFrame(stack, ref context, 0.05f);

            Assert.Greater(enemyGo.transform.position.x, 0.01f, "Dash should advance toward the player on X.");

            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void Runner_WhenAttackOnCooldown_ChaseMovesEnemy()
        {
            var enemyGo = new GameObject("Enemy");
            var playerGo = new GameObject("Player");
            enemyGo.transform.position = Vector3.zero;
            playerGo.transform.position = new Vector3(3f, 0f, 0f);

            var attack = new BeeDashAttackEnemyBehavior(5f, 10f, 0.15f, 15f, false, 0f);
            var chase = new BeeChaseEnemyBehavior(4f, false);
            IEnemyActorBehavior[] stack = { attack, chase };
            var context = new EnemyBehaviorTickContext
            {
                Self = enemyGo.transform,
                Target = playerGo.transform,
                Body = null
            };

            for (int i = 0; i < 20; i++)
            {
                EnemyBehaviorRunner.RunFrame(stack, ref context, 0.05f);
            }

            playerGo.transform.position = new Vector3(40f, 0f, 0f);
            float xBeforeChaseOnly = enemyGo.transform.position.x;

            for (int i = 0; i < 15; i++)
            {
                EnemyBehaviorRunner.RunFrame(stack, ref context, 0.05f);
            }

            Assert.Greater(enemyGo.transform.position.x, xBeforeChaseOnly, "Chase should advance while attack cooldown blocks a new dash.");

            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void DashAttack_WhenTargetOutOfRange_DoesNotClaimSoChaseCanRun()
        {
            var enemyGo = new GameObject("Enemy");
            var playerGo = new GameObject("Player");
            enemyGo.transform.position = Vector3.zero;
            playerGo.transform.position = new Vector3(50f, 0f, 0f);

            var attack = new BeeDashAttackEnemyBehavior(4f, 0.5f, 0.2f, 10f, false, 0f);
            var chase = new BeeChaseEnemyBehavior(3f, false);
            IEnemyActorBehavior[] stack = { attack, chase };
            var context = new EnemyBehaviorTickContext
            {
                Self = enemyGo.transform,
                Target = playerGo.transform,
                Body = null
            };

            EnemyBehaviorRunner.RunFrame(stack, ref context, 0.05f);

            Assert.Greater(enemyGo.transform.position.x, 0.01f, "Chase should move when attack is out of range.");

            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(playerGo);
        }
    }
}
