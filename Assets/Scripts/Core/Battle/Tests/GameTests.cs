using System;
using System.Collections.Generic;
using Madbox.Battle.Events;
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
    public class GameTests
    {
        [Test]
        public void StartAndTick_TransitionsToRunningAndUpdatesElapsedTime()
        {
            Game game = CreateGame();

            game.Start();
            game.Tick(0.5f);

            Assert.AreEqual(GameState.Running, game.CurrentState);
            Assert.AreEqual(0.5f, game.ElapsedTimeSeconds, 0.0001f);
        }

        [Test]
        public void Tick_WhenNotRunning_DoesNotUpdateElapsedTime()
        {
            Game game = CreateGame();

            game.Tick(1f);

            Assert.AreEqual(0f, game.ElapsedTimeSeconds);
        }

        [Test]
        public void Trigger_TryPlayerAttack_WhenValid_EmitsPlayerAttackAndEnemyKilled()
        {
            Game game = CreateGame();
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
            game.Start();
            game.Tick(3f);
            bool playerKilledObserved = false;
            GameEndReason completionReason = GameEndReason.None;
            game.EventTriggered += e => playerKilledObserved = playerKilledObserved || e is PlayerKilled;
            game.OnCompleted += reason => completionReason = reason;

            game.Trigger(new EnemyHitObserved(new EntityId("enemy-1"), new EntityId("player-1"), 6));
            game.Tick(0.01f);

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
            game.Start();
            game.Tick(3f);

            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-2")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-2")));
            game.Tick(0.01f);

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
            game.Start();
            game.Tick(3f);

            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-2")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-2")));
            game.Tick(0.01f);
            game.Trigger(new EnemyHitObserved(new EntityId("enemy-1"), new EntityId("player-1"), 999));
            game.Tick(0.01f);

            Assert.AreEqual(1, completionCalls);
        }

        [Test]
        public void Tick_EvaluatesGameRulesAndCompletesWhenAllEnemiesAreDead()
        {
            GoldWallet wallet = new GoldWallet();
            Game game = CreateGame(wallet: wallet, gameRules: CreateEnemyKillOnlyRuleSet());
            game.Start();
            game.Tick(3f);

            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-2")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-2")));

            Assert.AreEqual(GameState.Running, game.CurrentState);

            game.Tick(0.01f);

            Assert.AreEqual(GameState.Done, game.CurrentState);
            Assert.AreEqual(15, wallet.CurrentGold);
        }

        [Test]
        public void Simulation_TimerOnlyRule_WhenTimeRunsOut_CompletesAsLose()
        {
            Game game = CreateGame(gameRules: CreateTimerOnlyRuleSet(1f));
            GameEndReason completionReason = GameEndReason.None;
            game.OnCompleted += reason => completionReason = reason;

            game.Start();
            game.Tick(0.5f);
            Assert.AreEqual(GameState.Running, game.CurrentState);

            game.Tick(0.5f);

            Assert.AreEqual(GameState.Done, game.CurrentState);
            Assert.AreEqual(GameEndReason.Lose, completionReason);
        }

        [Test]
        public void Simulation_EnemyKillOnlyRule_WhenEnemiesDie_CompletesAsWin()
        {
            GoldWallet wallet = new GoldWallet();
            Game game = CreateGame(wallet: wallet, gameRules: CreateEnemyKillOnlyRuleSet());
            GameEndReason completionReason = GameEndReason.None;
            game.OnCompleted += reason => completionReason = reason;

            game.Start();
            game.Tick(3f);
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-2")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-2")));
            game.Tick(0.01f);

            Assert.AreEqual(GameState.Done, game.CurrentState);
            Assert.AreEqual(GameEndReason.Win, completionReason);
            Assert.AreEqual(15, wallet.CurrentGold);
        }

        [Test]
        public void Simulation_TimerAndEnemyRules_WhenTimerRunsOutWithEnemiesAlive_CompletesAsLose()
        {
            Game game = CreateGame(gameRules: CreateEnemyAndTimerRuleSet(1f));
            GameEndReason completionReason = GameEndReason.None;
            game.OnCompleted += reason => completionReason = reason;

            game.Start();
            game.Tick(1f);

            Assert.AreEqual(GameState.Done, game.CurrentState);
            Assert.AreEqual(GameEndReason.Lose, completionReason);
        }

        [Test]
        public void Simulation_TimerAndEnemyRules_WhenEnemiesDieBeforeTimer_CompletesAsWin()
        {
            GoldWallet wallet = new GoldWallet();
            Game game = CreateGame(wallet: wallet, gameRules: CreateEnemyAndTimerRuleSet(5f));
            GameEndReason completionReason = GameEndReason.None;
            game.OnCompleted += reason => completionReason = reason;

            game.Start();
            game.Tick(3f);
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-1")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-2")));
            game.Trigger(new TryPlayerAttack(new EntityId("player-1"), new EntityId("enemy-2")));
            game.Tick(0.01f);

            Assert.AreEqual(GameState.Done, game.CurrentState);
            Assert.AreEqual(GameEndReason.Win, completionReason);
            Assert.AreEqual(15, wallet.CurrentGold);
        }

        [Test]
        public void Trigger_PlayerAutoAttackObserved_WhenProjectileHits_EmitsAttackAndEnemyKilled()
        {
            Game game = CreateGame();
            game.Start();
            List<BattleEvent> emitted = new List<BattleEvent>();
            game.EventTriggered += emitted.Add;

            game.Trigger(new PlayerAutoAttackObserved(new EntityId("player-1"), new EntityId("enemy-1")));
            PlayerProjectileSpawned firstProjectile = emitted.Find(e => e is PlayerProjectileSpawned) as PlayerProjectileSpawned;
            Assert.IsNotNull(firstProjectile);

            game.Trigger(new PlayerProjectileHitObserved(firstProjectile.ProjectileId, new EntityId("player-1"), new EntityId("enemy-1")));
            Assert.That(emitted, Has.Some.InstanceOf<PlayerAttack>());
            Assert.That(emitted, Has.None.InstanceOf<EnemyKilled>());

            game.Tick(0.6f);
            game.Trigger(new PlayerAutoAttackObserved(new EntityId("player-1"), new EntityId("enemy-1")));
            PlayerProjectileSpawned secondProjectile = emitted.FindLast(e => e is PlayerProjectileSpawned) as PlayerProjectileSpawned;
            Assert.IsNotNull(secondProjectile);
            game.Trigger(new PlayerProjectileHitObserved(secondProjectile.ProjectileId, new EntityId("player-1"), new EntityId("enemy-1")));

            Assert.That(emitted, Has.Some.InstanceOf<EnemyKilled>());
        }

        [Test]
        public void Trigger_PlayerProjectileHitObserved_WhenProjectileUnknown_EmitsNothing()
        {
            Game game = CreateGame();
            game.Start();
            int emittedCount = 0;
            game.EventTriggered += _ => emittedCount++;

            game.Trigger(new PlayerProjectileHitObserved(new EntityId("unknown-projectile"), new EntityId("player-1"), new EntityId("enemy-1")));

            Assert.AreEqual(0, emittedCount);
        }

        private static Game CreateGame(
            int playerHealth = 100,
            GoldWallet wallet = null,
            IReadOnlyList<LevelGameRuleDefinition> gameRules = null)
        {
            EnemyDefinition enemy = new EnemyDefinition(
                new EntityId("slime"),
                maxHealth: 20,
                    new EnemyBehaviorDefinition[]
                {
                    new MovementBehaviorDefinition(1.5f, 4f),
                    new ContactAttackBehaviorDefinition(6, 0.5f, 4f)
                });

            LevelDefinition level = new LevelDefinition(
                new LevelId("level-1"),
                goldReward: 15,
                enemies: new List<LevelEnemyDefinition>
                {
                    new LevelEnemyDefinition(enemy, 2)
                },
                gameRules ?? CreateEnemyAndTimerRuleSet(10f));

            Player player = new Player(new EntityId("player-1"), playerHealth);
            return new Game(level, wallet ?? new GoldWallet(), player);
        }

        private static IReadOnlyList<LevelGameRuleDefinition> CreateTimerOnlyRuleSet(float loseAfterSeconds)
        {
            return new LevelGameRuleDefinition[]
            {
                new TimeLimitLoseRuleDefinition(loseAfterSeconds)
            };
        }

        private static IReadOnlyList<LevelGameRuleDefinition> CreateEnemyKillOnlyRuleSet()
        {
            return new LevelGameRuleDefinition[]
            {
                new EnemyEliminatedWinRuleDefinition()
            };
        }

        private static IReadOnlyList<LevelGameRuleDefinition> CreateEnemyAndTimerRuleSet(float loseAfterSeconds)
        {
            return new LevelGameRuleDefinition[]
            {
                new EnemyEliminatedWinRuleDefinition(),
                new TimeLimitLoseRuleDefinition(loseAfterSeconds)
            };
        }
    }
}


