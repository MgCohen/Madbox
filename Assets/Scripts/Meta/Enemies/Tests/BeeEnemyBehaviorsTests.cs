using Madbox.Entities;
using Madbox.Players;
using NUnit.Framework;
using UnityEditor;
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

            var enemy = enemyGo.AddComponent<Enemy>();
            enemy.Initialize();
            var dash = enemyGo.AddComponent<BeeDashAttackEnemyBehavior>();
            var chase = enemyGo.AddComponent<BeeChaseEnemyBehavior>();

            BeeEnemyTestAttributeSet attrs = ConfigureBeeEnemyAttributes(
                enemy,
                dash,
                chase,
                attackRange: 4f,
                attackCooldownSeconds: 1.25f,
                dashDurationSeconds: 0.35f,
                dashSpeed: 12f,
                dashImpulse: 8f,
                chaseSpeed: 2.5f);

            var player = playerGo.AddComponent<Player>();
            var input = new EnemyInputContext(player);

            RunEnemyFrame(enemy, in input, 0.05f, dash, chase);

            Assert.Greater(enemyGo.transform.position.x, 0.01f, "Dash should advance toward the player on X.");

            attrs.DestroyAssets();
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

            var enemy = enemyGo.AddComponent<Enemy>();
            enemy.Initialize();
            var dash = enemyGo.AddComponent<BeeDashAttackEnemyBehavior>();
            var chase = enemyGo.AddComponent<BeeChaseEnemyBehavior>();

            BeeEnemyTestAttributeSet attrs = ConfigureBeeEnemyAttributes(
                enemy,
                dash,
                chase,
                attackRange: 5f,
                attackCooldownSeconds: 10f,
                dashDurationSeconds: 0.15f,
                dashSpeed: 15f,
                dashImpulse: 8f,
                chaseSpeed: 4f);

            var player = playerGo.AddComponent<Player>();
            var input = new EnemyInputContext(player);

            for (int i = 0; i < 20; i++)
            {
                RunEnemyFrame(enemy, in input, 0.05f, dash, chase);
            }

            playerGo.transform.position = new Vector3(40f, 0f, 0f);
            float xBeforeChaseOnly = enemyGo.transform.position.x;

            for (int i = 0; i < 15; i++)
            {
                RunEnemyFrame(enemy, in input, 0.05f, dash, chase);
            }

            Assert.Greater(enemyGo.transform.position.x, xBeforeChaseOnly, "Chase should advance while attack cooldown blocks a new dash.");

            attrs.DestroyAssets();
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

            var enemy = enemyGo.AddComponent<Enemy>();
            enemy.Initialize();
            var dash = enemyGo.AddComponent<BeeDashAttackEnemyBehavior>();
            var chase = enemyGo.AddComponent<BeeChaseEnemyBehavior>();

            BeeEnemyTestAttributeSet attrs = ConfigureBeeEnemyAttributes(
                enemy,
                dash,
                chase,
                attackRange: 4f,
                attackCooldownSeconds: 1.25f,
                dashDurationSeconds: 0.35f,
                dashSpeed: 12f,
                dashImpulse: 8f,
                chaseSpeed: 2.5f);

            var player = playerGo.AddComponent<Player>();
            var input = new EnemyInputContext(player);

            RunEnemyFrame(enemy, in input, 0.05f, dash, chase);

            Assert.Greater(enemyGo.transform.position.x, 0.01f, "Chase should move when attack is out of range.");

            attrs.DestroyAssets();
            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void DashAttack_WhenDashStarts_AppliesDashDamageToPlayerDamageable()
        {
            EntityAttribute dashDamageAttr = ScriptableObject.CreateInstance<EntityAttribute>();

            GameObject enemyGo = new GameObject("Enemy");
            GameObject playerGo = new GameObject("Player");
            enemyGo.transform.position = Vector3.zero;
            playerGo.transform.position = new Vector3(3f, 0f, 0f);

            Enemy enemy = enemyGo.AddComponent<Enemy>();
            enemy.Initialize();
            BeeDashAttackEnemyBehavior dash = enemyGo.AddComponent<BeeDashAttackEnemyBehavior>();
            BeeChaseEnemyBehavior chase = enemyGo.AddComponent<BeeChaseEnemyBehavior>();

            BeeEnemyTestAttributeSet attrs = ConfigureBeeEnemyAttributes(
                enemy,
                dash,
                chase,
                attackRange: 4f,
                attackCooldownSeconds: 1.25f,
                dashDurationSeconds: 0.35f,
                dashSpeed: 12f,
                dashImpulse: 8f,
                chaseSpeed: 2.5f,
                dashDamageAttribute: dashDamageAttr,
                dashDamageBaseValue: 5f);

            Player player = playerGo.AddComponent<Player>();
            EnemyTestPlayerDamageableSetup.ConfigurePlayerDamageable(playerGo, player, maxHp: 100f);

            Damageable damageable = playerGo.GetComponentInChildren<Damageable>();
            Assert.That(damageable.CurrentHp, Is.EqualTo(100f).Within(0.0001f));

            EnemyInputContext input = new EnemyInputContext(player);
            RunEnemyFrame(enemy, in input, 0.05f, dash, chase);

            Assert.That(damageable.CurrentHp, Is.EqualTo(95f).Within(0.0001f));

            attrs.DestroyAssets();
            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(dashDamageAttr);
        }

        private static BeeEnemyTestAttributeSet ConfigureBeeEnemyAttributes(
            Enemy enemy,
            BeeDashAttackEnemyBehavior dash,
            BeeChaseEnemyBehavior chase,
            float attackRange,
            float attackCooldownSeconds,
            float dashDurationSeconds,
            float dashSpeed,
            float dashImpulse,
            float chaseSpeed,
            EntityAttribute dashDamageAttribute = null,
            float dashDamageBaseValue = 0f)
        {
            var attackRangeAttribute = ScriptableObject.CreateInstance<EntityAttribute>();
            var attackCooldownAttribute = ScriptableObject.CreateInstance<EntityAttribute>();
            var dashDurationAttribute = ScriptableObject.CreateInstance<EntityAttribute>();
            var dashSpeedAttribute = ScriptableObject.CreateInstance<EntityAttribute>();
            var dashImpulseAttribute = ScriptableObject.CreateInstance<EntityAttribute>();
            var chaseSpeedAttribute = ScriptableObject.CreateInstance<EntityAttribute>();

            SerializedObject enemySo = new SerializedObject(enemy);
            SerializedProperty list = enemySo.FindProperty("attributeEntries");
            int entryCount = dashDamageAttribute != null ? 7 : 6;
            list.arraySize = entryCount;
            SetAttributeEntry(list, 0, attackRangeAttribute, attackRange);
            SetAttributeEntry(list, 1, attackCooldownAttribute, attackCooldownSeconds);
            SetAttributeEntry(list, 2, dashDurationAttribute, dashDurationSeconds);
            SetAttributeEntry(list, 3, dashSpeedAttribute, dashSpeed);
            SetAttributeEntry(list, 4, dashImpulseAttribute, dashImpulse);
            SetAttributeEntry(list, 5, chaseSpeedAttribute, chaseSpeed);
            if (dashDamageAttribute != null)
            {
                SetAttributeEntry(list, 6, dashDamageAttribute, dashDamageBaseValue);
            }

            enemySo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject dashSo = new SerializedObject(dash);
            dashSo.FindProperty("attackRangeAttribute").objectReferenceValue = attackRangeAttribute;
            dashSo.FindProperty("attackCooldownSecondsAttribute").objectReferenceValue = attackCooldownAttribute;
            dashSo.FindProperty("dashDurationSecondsAttribute").objectReferenceValue = dashDurationAttribute;
            dashSo.FindProperty("dashSpeedAttribute").objectReferenceValue = dashSpeedAttribute;
            dashSo.FindProperty("dashImpulseAttribute").objectReferenceValue = dashImpulseAttribute;
            dashSo.FindProperty("dashDamageAttribute").objectReferenceValue = dashDamageAttribute;
            dashSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject chaseSo = new SerializedObject(chase);
            chaseSo.FindProperty("chaseSpeedAttribute").objectReferenceValue = chaseSpeedAttribute;
            chaseSo.ApplyModifiedPropertiesWithoutUndo();

            return new BeeEnemyTestAttributeSet(
                attackRangeAttribute,
                attackCooldownAttribute,
                dashDurationAttribute,
                dashSpeedAttribute,
                dashImpulseAttribute,
                chaseSpeedAttribute);
        }

        private static void SetAttributeEntry(SerializedProperty list, int index, EntityAttribute attribute, float baseValue)
        {
            SerializedProperty entry = list.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("attribute").objectReferenceValue = attribute;
            entry.FindPropertyRelative("baseValue").floatValue = baseValue;
        }

        private static void RunEnemyFrame(
            Enemy enemy,
            in EnemyInputContext input,
            float deltaTime,
            BeeDashAttackEnemyBehavior dash,
            BeeChaseEnemyBehavior chase)
        {
            IEnemyBehavior winner = null;
            if (dash.TryAcceptControl(enemy, in input))
            {
                winner = dash;
            }
            else if (chase.TryAcceptControl(enemy, in input))
            {
                winner = chase;
            }

            winner?.Execute(enemy, in input, deltaTime);
        }

        private readonly struct BeeEnemyTestAttributeSet
        {
            private readonly EntityAttribute attackRangeAttribute;
            private readonly EntityAttribute attackCooldownAttribute;
            private readonly EntityAttribute dashDurationAttribute;
            private readonly EntityAttribute dashSpeedAttribute;
            private readonly EntityAttribute dashImpulseAttribute;
            private readonly EntityAttribute chaseSpeedAttribute;

            public BeeEnemyTestAttributeSet(
                EntityAttribute attackRangeAttribute,
                EntityAttribute attackCooldownAttribute,
                EntityAttribute dashDurationAttribute,
                EntityAttribute dashSpeedAttribute,
                EntityAttribute dashImpulseAttribute,
                EntityAttribute chaseSpeedAttribute)
            {
                this.attackRangeAttribute = attackRangeAttribute;
                this.attackCooldownAttribute = attackCooldownAttribute;
                this.dashDurationAttribute = dashDurationAttribute;
                this.dashSpeedAttribute = dashSpeedAttribute;
                this.dashImpulseAttribute = dashImpulseAttribute;
                this.chaseSpeedAttribute = chaseSpeedAttribute;
            }

            public void DestroyAssets()
            {
                Object.DestroyImmediate(attackRangeAttribute);
                Object.DestroyImmediate(attackCooldownAttribute);
                Object.DestroyImmediate(dashDurationAttribute);
                Object.DestroyImmediate(dashSpeedAttribute);
                Object.DestroyImmediate(dashImpulseAttribute);
                Object.DestroyImmediate(chaseSpeedAttribute);
            }
        }
    }
}
