using System;
using System.Collections.Generic;
using Madbox.Gold;
using Madbox.Levels;
using NUnit.Framework;
#pragma warning disable SCA0003
#pragma warning disable SCA0005
#pragma warning disable SCA0006
#pragma warning disable SCA0023

namespace Madbox.Battle.Tests
{
    public class GameTests
    {
        [Test]
        public void Initialize_StartAndTick_TransitionsToRunningAndUpdatesElapsedTime()
        {
            Game game = CreateGame();

            game.Initialize();
            game.Start();
            game.Tick(0.5f);

            Assert.AreEqual(GameState.Running, game.CurrentState);
            Assert.AreEqual(0.5f, game.ElapsedTimeSeconds, 0.0001f);
        }

        [Test]
        public void Tick_WhenNotRunning_DoesNotUpdateElapsedTime()
        {
            Game game = CreateGame();
            game.Initialize();

            game.Tick(1f);

            Assert.AreEqual(0f, game.ElapsedTimeSeconds);
        }

        [Test]
        public void Trigger_TryPlayerAttack_WhenValid_EmitsPlayerAttackAndEnemyKilled()
        {
            Game game = CreateGame();
            game.Initialize();
            game.Start();
            game.Tick(3f);
            List<BattleEvent> emitted = new List<BattleEvent>();
            game.EventTriggered += emitted.Add;

            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));

            Assert.That(emitted, Has.Some.InstanceOf<PlayerAttack>());
            Assert.That(emitted, Has.Some.InstanceOf<EnemyKilled>());
        }

        [Test]
        public void Trigger_TryPlayerAttack_WhenInvalidActor_EmitsNothing()
        {
            Game game = CreateGame();
            game.Initialize();
            game.Start();
            int emittedCount = 0;
            game.EventTriggered += _ => emittedCount++;

            game.Trigger(new TryPlayerAttack(new EntityId("not-player"), new EntityId("enemy-1")));

            Assert.AreEqual(0, emittedCount);
        }

        [Test]
        public void Trigger_EnemyHitObserved_WhenValid_EmitsPlayerDamaged()
        {
            Game game = CreateGame();
            game.Initialize();
            game.Start();
            game.Tick(3f);
            PlayerDamaged damagedEvent = null;
            game.EventTriggered += e =>
            {
                if (e is PlayerDamaged damaged)
                {
                    damagedEvent = damaged;
                }
            };

            game.Trigger(new EnemyHitObserved(new EntityId("enemy-1"), new EntityId("player-1"), 3));

            Assert.IsNotNull(damagedEvent);
            Assert.AreEqual(97, damagedEvent.RemainingHp);
        }

        [Test]
        public void Trigger_EnemyHitObserved_WhenPlayerDies_EmitsPlayerKilledAndCompletesAsLose()
        {
            Game game = CreateGame(playerHealth: 6);
            game.Initialize();
            game.Start();
            game.Tick(3f);
            bool playerKilledObserved = false;
            GameEndReason completionReason = GameEndReason.None;
            game.EventTriggered += e => playerKilledObserved = playerKilledObserved || e is PlayerKilled;
            game.OnCompleted += reason => completionReason = reason;

            game.Trigger(new EnemyHitObserved(new EntityId("enemy-1"), new EntityId("player-1"), 6));

            Assert.IsTrue(playerKilledObserved);
            Assert.AreEqual(GameEndReason.Lose, completionReason);
            Assert.AreEqual(GameState.Done, game.CurrentState);
        }

        [Test]
        public void Trigger_WhenAllEnemiesDie_CompletesAsWinAndAddsGoldReward()
        {
            GoldWallet wallet = new GoldWallet();
            Game game = CreateGame(wallet: wallet);
            GameEndReason completionReason = GameEndReason.None;
            game.OnCompleted += reason => completionReason = reason;
            game.Initialize();
            game.Start();
            game.Tick(3f);

            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-2")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-2")));

            Assert.AreEqual(GameEndReason.Win, completionReason);
            Assert.AreEqual(15, wallet.CurrentGold);
            Assert.AreEqual(GameState.Done, game.CurrentState);
        }

        [Test]
        public void OnCompleted_WhenGameAlreadyDone_RaisesOnlyOnce()
        {
            Game game = CreateGame();
            int completionCalls = 0;
            game.OnCompleted += _ => completionCalls++;
            game.Initialize();
            game.Start();
            game.Tick(3f);

            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-2")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-2")));
            game.Trigger(new EnemyHitObserved(new EntityId("enemy-1"), new EntityId("player-1"), 999));

            Assert.AreEqual(1, completionCalls);
        }

        [Test]
        public void Tick_WhenEnemyHasMovementBehavior_UpdatesEnemyDistance()
        {
            Game game = CreateGame();
            game.Initialize();
            game.Start();
            bool foundBefore = game.TryGetEnemyDistance(new EntityId("enemy-1"), out float before);

            game.Tick(1f);

            bool foundAfter = game.TryGetEnemyDistance(new EntityId("enemy-1"), out float after);
            Assert.IsTrue(foundBefore);
            Assert.IsTrue(foundAfter);
            Assert.Less(after, before);
        }

        private Game CreateGame(int playerHealth = 100, GoldWallet wallet = null)
        {
            EnemyDefinition enemy = new EnemyDefinition(
                new EntityId("slime"),
                maxHealth: 20,
                new EnemyBehaviorDefinition[]
                {
                    new MovementBehaviorDefinition(1.5f, 4f),
                    new ContactAttackBehaviorDefinition(6, 0.5f, 1.5f)
                });

            LevelDefinition level = new LevelDefinition(
                new LevelId("level-1"),
                goldReward: 15,
                enemies: new List<LevelEnemyDefinition>
                {
                    new LevelEnemyDefinition(enemy, 2)
                });

            return new Game(level, wallet ?? new GoldWallet(), new EntityId("player-1"), playerHealth);
        }
    }
}
