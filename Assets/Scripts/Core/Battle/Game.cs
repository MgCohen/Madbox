using System;
using System.Collections.Generic;
using Madbox.Gold;
using Madbox.Levels;
#pragma warning disable SCA0003
#pragma warning disable SCA0006
#pragma warning disable SCA0009
#pragma warning disable SCA0014
#pragma warning disable SCA0020

namespace Madbox.Battle
{
    public class Game
    {
        private const int PlayerBaseDamage = 10;
        private const float SpawnDistanceToPlayer = 3f;

        private readonly LevelDefinition levelDefinition;
        private readonly GoldWallet goldWallet;
        private readonly Dictionary<EntityId, EnemyRuntimeState> enemiesByRuntimeId;
        private readonly EntityId playerId;
        private readonly int playerMaxHealth;
        private bool completionRaised;

        public Game(LevelDefinition levelDefinition, GoldWallet goldWallet, EntityId playerId, int playerMaxHealth = 100)
        {
            this.levelDefinition = levelDefinition ?? throw new ArgumentNullException(nameof(levelDefinition));
            this.goldWallet = goldWallet ?? throw new ArgumentNullException(nameof(goldWallet));
            this.playerId = playerId ?? throw new ArgumentNullException(nameof(playerId));

            if (playerMaxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerMaxHealth), "Player max health must be greater than zero.");
            }

            this.playerMaxHealth = playerMaxHealth;
            CurrentLevelId = levelDefinition.LevelId ?? throw new ArgumentException("LevelDefinition.LevelId is required.", nameof(levelDefinition));
            State = new BattleRuntimeState();
            enemiesByRuntimeId = CreateEnemies(levelDefinition);
            CurrentState = GameState.NotRunning;
            ResetStateValues();
        }

        public event Action<BattleEvent> EventTriggered;

        public event Action<GameEndReason> OnCompleted;

        public GameState CurrentState { get; private set; }

        public float ElapsedTimeSeconds => State.ElapsedTimeSeconds;

        public LevelId CurrentLevelId { get; }

        public BattleRuntimeState State { get; }

        public void Initialize()
        {
            CurrentState = GameState.NotRunning;
            ResetStateValues();
            ResetEnemies();
            completionRaised = false;
        }

        public void Start()
        {
            if (CurrentState != GameState.NotRunning)
            {
                return;
            }

            CurrentState = GameState.Running;
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState != GameState.Running)
            {
                return;
            }

            if (deltaTime <= 0f)
            {
                return;
            }

            State.ElapsedTimeSeconds += deltaTime;
            ProcessEnemyBehaviors(deltaTime);
        }

        public void Trigger(BattleEvent gameEvent)
        {
            if (gameEvent == null)
            {
                throw new ArgumentNullException(nameof(gameEvent));
            }

            if (CurrentState != GameState.Running)
            {
                return;
            }

            switch (gameEvent)
            {
                case TryPlayerAttack attack:
                    HandleTryPlayerAttack(attack);
                    break;
                case EnemyHitObserved enemyHit:
                    HandleEnemyHitObserved(enemyHit);
                    break;
            }
        }

        public bool TryGetEnemyDistance(EntityId enemyId, out float distance)
        {
            if (enemyId == null)
            {
                distance = default;
                return false;
            }

            if (enemiesByRuntimeId.TryGetValue(enemyId, out EnemyRuntimeState enemy) == false)
            {
                distance = default;
                return false;
            }

            distance = enemy.DistanceToPlayer;
            return true;
        }

        private void HandleTryPlayerAttack(TryPlayerAttack attack)
        {
            if (Equals(attack.ActorId, playerId) == false)
            {
                return;
            }

            if (attack.TargetId == null)
            {
                return;
            }

            if (enemiesByRuntimeId.TryGetValue(attack.TargetId, out EnemyRuntimeState enemy) == false)
            {
                return;
            }

            if (enemy.IsAlive == false)
            {
                return;
            }

            enemy.CurrentHealth -= PlayerBaseDamage;
            EventTriggered?.Invoke(new PlayerAttack(attack.ActorId, attack.TargetId, PlayerBaseDamage));

            if (enemy.IsAlive == false)
            {
                State.AliveEnemies--;
                State.DeadEnemies++;
                EventTriggered?.Invoke(new EnemyKilled(enemy.RuntimeEntityId, attack.ActorId));

                if (State.AliveEnemies == 0)
                {
                    Complete(GameEndReason.Win);
                }
            }
        }

        private void HandleEnemyHitObserved(EnemyHitObserved hit)
        {
            if (Equals(hit.PlayerId, playerId) == false)
            {
                return;
            }

            if (hit.RawDamage <= 0)
            {
                return;
            }

            if (enemiesByRuntimeId.TryGetValue(hit.EnemyId, out EnemyRuntimeState enemy) == false)
            {
                return;
            }

            if (enemy.IsAlive == false)
            {
                return;
            }

            ContactAttackBehaviorDefinition attackBehavior = FindBehavior<ContactAttackBehaviorDefinition>(enemy.Definition.Behaviors);
            if (attackBehavior == null)
            {
                return;
            }

            if (enemy.DistanceToPlayer > attackBehavior.AttackRange)
            {
                return;
            }

            if (enemy.AttackCooldownRemaining > 0f)
            {
                return;
            }

            int appliedDamage = Math.Min(hit.RawDamage, attackBehavior.Damage);
            if (appliedDamage <= 0)
            {
                return;
            }

            int nextHealth = Math.Max(0, State.PlayerHealth - appliedDamage);
            State.PlayerHealth = nextHealth;
            enemy.AttackCooldownRemaining = Math.Max(0f, attackBehavior.CooldownSeconds);
            EventTriggered?.Invoke(new PlayerDamaged(playerId, hit.EnemyId, appliedDamage, nextHealth));

            if (nextHealth == 0)
            {
                EventTriggered?.Invoke(new PlayerKilled(playerId, hit.EnemyId));
                Complete(GameEndReason.Lose);
            }
        }

        private void ProcessEnemyBehaviors(float deltaTime)
        {
            foreach (EnemyRuntimeState enemy in enemiesByRuntimeId.Values)
            {
                if (enemy.IsAlive == false)
                {
                    continue;
                }

                if (enemy.AttackCooldownRemaining > 0f)
                {
                    enemy.AttackCooldownRemaining = Math.Max(0f, enemy.AttackCooldownRemaining - deltaTime);
                }

                MovementBehaviorDefinition movement = FindBehavior<MovementBehaviorDefinition>(enemy.Definition.Behaviors);
                if (movement == null)
                {
                    continue;
                }

                float movedDistance = movement.MoveSpeed * deltaTime;
                enemy.DistanceToPlayer = Math.Max(0f, enemy.DistanceToPlayer - movedDistance);
            }
        }

        private void Complete(GameEndReason reason)
        {
            if (completionRaised)
            {
                return;
            }

            completionRaised = true;
            CurrentState = GameState.Done;

            if (reason == GameEndReason.Win && levelDefinition.GoldReward > 0)
            {
                goldWallet.Add(levelDefinition.GoldReward);
            }

            OnCompleted?.Invoke(reason);
        }

        private void ResetStateValues()
        {
            State.PlayerHealth = playerMaxHealth;
            State.AliveEnemies = CountAliveEnemies();
            State.DeadEnemies = 0;
            State.ElapsedTimeSeconds = 0f;
        }

        private void ResetEnemies()
        {
            foreach (EnemyRuntimeState enemy in enemiesByRuntimeId.Values)
            {
                enemy.CurrentHealth = enemy.Definition.MaxHealth;
                enemy.DistanceToPlayer = SpawnDistanceToPlayer;
                enemy.AttackCooldownRemaining = 0f;
            }
        }

        private int CountAliveEnemies()
        {
            int count = 0;
            foreach (EnemyRuntimeState enemy in enemiesByRuntimeId.Values)
            {
                if (enemy.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private static Dictionary<EntityId, EnemyRuntimeState> CreateEnemies(LevelDefinition definition)
        {
            Dictionary<EntityId, EnemyRuntimeState> enemies = new Dictionary<EntityId, EnemyRuntimeState>();
            int enemyIndex = 1;

            foreach (LevelEnemyDefinition levelEnemy in definition.Enemies)
            {
                for (int i = 0; i < levelEnemy.Count; i++)
                {
                    EntityId runtimeId = new EntityId($"enemy-{enemyIndex}");
                    enemyIndex++;
                    enemies.Add(
                        runtimeId,
                        new EnemyRuntimeState(runtimeId, levelEnemy.Enemy, levelEnemy.Enemy.MaxHealth, SpawnDistanceToPlayer));
                }
            }

            return enemies;
        }

        private static TBehavior FindBehavior<TBehavior>(IReadOnlyList<EnemyBehaviorDefinition> behaviors)
            where TBehavior : EnemyBehaviorDefinition
        {
            if (behaviors == null)
            {
                return null;
            }

            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is TBehavior behavior)
                {
                    return behavior;
                }
            }

            return null;
        }
    }
}
