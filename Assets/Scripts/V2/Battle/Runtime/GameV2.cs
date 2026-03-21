using System;
using System.Collections.Generic;
using Madbox.V2.Enemies;
using Madbox.V2.Levels;
using UnityEngine;

namespace Madbox.V2.Battle
{
    public sealed class GameV2
    {
        public GameV2(LevelDefinitionV2 level, EnemyServiceV2 enemyService, RuleHandlerRegistryV2 ruleRegistry)
        {
            Level = level;
            this.enemyService = enemyService ?? throw new ArgumentNullException(nameof(enemyService));
            if (ruleRegistry == null)
            {
                throw new ArgumentNullException(nameof(ruleRegistry));
            }

            ruleHandlers = ruleRegistry.CreateHandlers(level.GameRules);
            CurrentState = GameStateV2.NotRunning;
        }

        public LevelDefinitionV2 Level { get; }

        public GameStateV2 CurrentState { get; private set; }

        public float ElapsedTimeSeconds { get; private set; }

        public bool IsRunning => CurrentState == GameStateV2.Running;

        public EnemyServiceV2 EnemyService => enemyService;

        private readonly EnemyServiceV2 enemyService;

        private readonly IReadOnlyList<IRuleHandlerV2> ruleHandlers;

        public event Action<GameEndReasonV2> OnCompleted;

        /// <summary>
        /// Instantiates <paramref name="count"/> enemies from <paramref name="prefab"/> via <see cref="EnemyServiceV2"/>.
        /// Addressable loading is expected to happen outside (e.g. <see cref="GameFactoryV2.PrepareAndSpawnEnemiesFromLevelAsync"/>).
        /// </summary>
        public void SpawnEnemyCopies(EnemyActor prefab, int count, Vector3 origin, float spacingPerIndex)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (count <= 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 position = origin + Vector3.right * (spacingPerIndex * i);
                enemyService.Spawn(prefab, position, Quaternion.identity);
            }
        }

        public void Start()
        {
            if (CurrentState != GameStateV2.NotRunning)
            {
                return;
            }

            CurrentState = GameStateV2.Running;
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState != GameStateV2.Running)
            {
                return;
            }

            if (deltaTime <= 0f)
            {
                return;
            }

            ElapsedTimeSeconds += deltaTime;
            TryCompleteFromRules();
        }

        private void TryCompleteFromRules()
        {
            if (ruleHandlers == null || ruleHandlers.Count == 0)
            {
                return;
            }

            TryFirstCompletingRule();
        }

        private void TryFirstCompletingRule()
        {
            for (int i = 0; i < ruleHandlers.Count; i++)
            {
                IRuleHandlerV2 handler = ruleHandlers[i];
                if (handler.Evaluate(this, out GameEndReasonV2 reason) && reason != GameEndReasonV2.None)
                {
                    Complete(reason);
                    return;
                }
            }
        }

        private void Complete(GameEndReasonV2 reason)
        {
            CurrentState = GameStateV2.Done;
            OnCompleted?.Invoke(reason);
        }
    }
}
