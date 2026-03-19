using System.Collections.Generic;
using Madbox.Battle.Events;
using Madbox.Battle.Services;
using Madbox.Gold;
using Madbox.Levels;
using Madbox.Levels.Behaviors;
using Madbox.Levels.Rules;
using NUnit.Framework;
#pragma warning disable SCA0003
#pragma warning disable SCA0005
#pragma warning disable SCA0006
#pragma warning disable SCA0023

namespace Madbox.Battle.Tests
{
    public class PlayerIntentContractTests
    {
        [Test]
        public void Player_StartAndStopMoving_UpdatesState()
        {
            EntityId playerId = new EntityId("player-1");
            Player player = new Player(playerId, 100);

            player.StartMoving(3.5f);
            Assert.IsTrue(player.IsMoving);
            Assert.AreEqual(3.5f, player.MovementSpeed, 0.0001f);

            player.StopMoving();
            Assert.IsFalse(player.IsMoving);
            Assert.AreEqual(3.5f, player.MovementSpeed, 0.0001f);
        }

        [Test]
        public void Player_SelectAndClearTarget_UpdatesState()
        {
            EntityId playerId = new EntityId("player-1");
            EntityId targetId = new EntityId("enemy-1");
            Player player = new Player(playerId, 100);

            player.SelectTarget(targetId);
            Assert.AreEqual(targetId, player.SelectedTargetId);

            player.ClearTarget();
            Assert.IsNull(player.SelectedTargetId);
        }

        [Test]
        public void Player_AttackData_ReflectsEquippedWeapon()
        {
            EntityId playerId = new EntityId("player-1");
            Player player = new Player(playerId, 100, WeaponProfiles.CurvedSword);

            Assert.AreEqual(WeaponProfiles.CurvedSword.Range, player.AttackRange, 0.0001f);
            Assert.AreEqual(WeaponProfiles.CurvedSword.CooldownSeconds, player.AttackCooldownSeconds, 0.0001f);
            Assert.AreEqual(WeaponProfiles.CurvedSword.AttackTimingNormalized, player.AttackTimingNormalized, 0.0001f);
        }

        [Test]
        public void Trigger_EquipPlayerWeaponIntent_EmitsWeaponAndAttackDataEvents()
        {
            Game game = CreateGame();
            List<BattleEvent> emitted = new List<BattleEvent>();
            game.EventTriggered += emitted.Add;
            game.Start();

            EntityId playerId = new EntityId("player-1");
            EquipPlayerWeaponIntent equipIntent = new EquipPlayerWeaponIntent(playerId, WeaponProfiles.GreatSword);
            game.Trigger(equipIntent);

            Assert.That(emitted, Has.Some.InstanceOf<PlayerWeaponEquipped>());
            Assert.That(emitted, Has.Some.InstanceOf<PlayerAutoAttackDataUpdated>());

            PlayerAutoAttackDataUpdated attackData = emitted.FindLast(evt => evt is PlayerAutoAttackDataUpdated) as PlayerAutoAttackDataUpdated;
            Assert.IsNotNull(attackData);
            Assert.AreEqual(WeaponProfiles.GreatSword.CooldownSeconds, attackData.CooldownSeconds, 0.0001f);
            Assert.AreEqual(WeaponProfiles.GreatSword.Range, attackData.Range, 0.0001f);
        }

        [Test]
        public void Trigger_PlayerMovementIntents_EmitMovementChanged()
        {
            Game game = CreateGame();
            List<BattleEvent> emitted = new List<BattleEvent>();
            game.EventTriggered += emitted.Add;
            game.Start();

            EntityId playerId = new EntityId("player-1");
            PlayerMovementStarted startedIntent = new PlayerMovementStarted(playerId, 2.25f);
            PlayerMovementStopped stoppedIntent = new PlayerMovementStopped(playerId);

            game.Trigger(startedIntent);
            game.Trigger(stoppedIntent);

            PlayerMovementChanged first = emitted.Find(evt => evt is PlayerMovementChanged) as PlayerMovementChanged;
            PlayerMovementChanged second = emitted.FindLast(evt => evt is PlayerMovementChanged) as PlayerMovementChanged;

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            Assert.IsTrue(first.IsMoving);
            Assert.IsFalse(second.IsMoving);
            Assert.AreEqual(2.25f, first.Speed, 0.0001f);
        }

        [Test]
        public void Trigger_TargetIntents_EmitTargetChanged()
        {
            Game game = CreateGame();
            List<BattleEvent> emitted = new List<BattleEvent>();
            game.EventTriggered += emitted.Add;
            game.Start();

            EntityId playerId = new EntityId("player-1");
            EntityId enemyId = new EntityId("enemy-1");
            TargetSelected selectedIntent = new TargetSelected(playerId, enemyId);
            TargetCleared clearedIntent = new TargetCleared(playerId);

            game.Trigger(selectedIntent);
            game.Trigger(clearedIntent);

            PlayerTargetChanged first = emitted.Find(evt => evt is PlayerTargetChanged) as PlayerTargetChanged;
            PlayerTargetChanged second = emitted.FindLast(evt => evt is PlayerTargetChanged) as PlayerTargetChanged;

            Assert.IsNotNull(first);
            Assert.AreEqual(enemyId, first.TargetId);
            Assert.IsNotNull(second);
            Assert.IsNull(second.TargetId);
        }

        [Test]
        public void Trigger_AutoAttackTriggered_EmitsAttackDataUpdate()
        {
            Game game = CreateGame();
            List<BattleEvent> emitted = new List<BattleEvent>();
            game.EventTriggered += emitted.Add;
            game.Start();

            EntityId playerId = new EntityId("player-1");
            EquipPlayerWeaponIntent equipIntent = new EquipPlayerWeaponIntent(playerId, WeaponProfiles.CurvedSword);
            AutoAttackTriggered triggerIntent = new AutoAttackTriggered(playerId);

            game.Trigger(equipIntent);
            game.Trigger(triggerIntent);

            PlayerAutoAttackDataUpdated latest = emitted.FindLast(evt => evt is PlayerAutoAttackDataUpdated) as PlayerAutoAttackDataUpdated;
            Assert.IsNotNull(latest);
            Assert.AreEqual(WeaponProfiles.CurvedSword.CooldownSeconds, latest.CooldownSeconds, 0.0001f);
            Assert.AreEqual(WeaponProfiles.CurvedSword.Range, latest.Range, 0.0001f);
        }

        [Test]
        public void SpawnBehaviorDefinition_CanRepresentArchetypes()
        {
            EntityId enemyType = new EntityId("bee");
            SpawnArchetypeDefinition archetype = new SpawnArchetypeDefinition(enemyType, 3);
            List<SpawnArchetypeDefinition> archetypes = new List<SpawnArchetypeDefinition> { archetype };
            SpawnBehaviorDefinition definition = new SpawnBehaviorDefinition(archetypes);

            Assert.AreEqual(1, definition.Archetypes.Count);
            Assert.AreEqual("bee", definition.Archetypes[0].EnemyTypeId.Value);
            Assert.AreEqual(3, definition.Archetypes[0].Count);
        }

        private Game CreateGame()
        {
            EntityId enemyTypeId = new EntityId("slime");
            MovementBehaviorDefinition movement = new MovementBehaviorDefinition(1.5f, 4f);
            ContactAttackBehaviorDefinition attack = new ContactAttackBehaviorDefinition(6, 0.5f, 4f);
            EnemyBehaviorDefinition[] behaviors = new EnemyBehaviorDefinition[] { movement, attack };
            EnemyDefinition enemy = new EnemyDefinition(enemyTypeId, 20, behaviors);

            LevelEnemyDefinition levelEnemy = new LevelEnemyDefinition(enemy, 2);
            List<LevelEnemyDefinition> enemies = new List<LevelEnemyDefinition> { levelEnemy };

            EnemyEliminatedWinRuleDefinition winRule = new EnemyEliminatedWinRuleDefinition();
            TimeLimitLoseRuleDefinition loseRule = new TimeLimitLoseRuleDefinition(10f);
            LevelGameRuleDefinition[] rules = new LevelGameRuleDefinition[] { winRule, loseRule };

            LevelId levelId = new LevelId("level-1");
            LevelDefinition level = new LevelDefinition(levelId, 15, enemies, rules);
            GoldWallet wallet = new GoldWallet();
            EntityId playerId = new EntityId("player-1");
            Player player = new Player(playerId, 100);
            return new Game(level, wallet, player);
        }
    }
}
