using System;
using System.Collections.Generic;
using Madbox.Enemies;
using Madbox.Enemies;
using Madbox.Levels;
using Madbox.Levels.Rules;
using UnityEngine;

namespace Madbox.Battle
{
    public sealed class BattleGame
    {
        public BattleGame(LevelDefinition level, EnemyService enemyService, RuleHandlerRegistry ruleRegistry)
        {
            Level = level;
            this.enemyService = enemyService ?? throw new ArgumentNullException(nameof(enemyService));
            if (ruleRegistry == null)
            {
                throw new ArgumentNullException(nameof(ruleRegistry));
            }

            ruleHandlers = ruleRegistry.CreateHandlers(level.GameRules);
            CurrentState = BattleGameState.NotRunning;
        }

        public LevelDefinition Level { get; }

        public BattleGameState CurrentState { get; private set; }

        public float ElapsedTimeSeconds { get; private set; }

        public bool IsRunning => CurrentState == BattleGameState.Running;

        public EnemyService EnemyService => enemyService;

        private readonly EnemyService enemyService;

        private readonly IReadOnlyList<IRuleHandler> ruleHandlers;

        public event Action<GameEndReason> OnCompleted;

        public void SpawnEnemyCopies(EnemyData prefab, int count, Vector3 origin, float spacingPerIndex)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (count <= 0)
            {
                return;
            }

            SpawnEnemyCopiesCore(prefab, count, origin, spacingPerIndex);
        }

        private void SpawnEnemyCopiesCore(EnemyData prefab, int count, Vector3 origin, float spacingPerIndex)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Vector3.right * (spacingPerIndex * i);
                Vector3 position = origin + offset;
                enemyService.Spawn(prefab, position, Quaternion.identity);
            }
        }

        public void Start()
        {
            if (CurrentState != BattleGameState.NotRunning)
            {
                return;
            }

            CurrentState = BattleGameState.Running;
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState != BattleGameState.Running)
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
                IRuleHandler handler = ruleHandlers[i];
                if (handler.Evaluate(this, out GameEndReason reason) && reason != GameEndReason.None)
                {
                    Complete(reason);
                    return;
                }
            }
        }

        private void Complete(GameEndReason reason)
        {
            CurrentState = BattleGameState.Done;
            OnCompleted?.Invoke(reason);
        }
    }
}
