using System;
using System.Collections.Generic;
using Madbox.Enemies;
using Madbox.Levels;
using Madbox.Levels.Rules;
using Madbox.Players;
using UnityEngine;

namespace Madbox.Battle
{
    public sealed class BattleGame
    {
        public BattleGame(
            LevelDefinition level,
            EnemyService enemyService,
            RuleHandlerRegistry ruleRegistry,
            Transform enemySpawnParent = null,
            Bounds? enemySpawnBounds = null,
            Vector3? playerSpawnExclusionCenter = null,
            float minHorizontalDistanceFromPlayer = 0f)
        {
            Level = level;
            this.enemyService = enemyService ?? throw new ArgumentNullException(nameof(enemyService));
            if (ruleRegistry == null)
            {
                throw new ArgumentNullException(nameof(ruleRegistry));
            }

            this.enemySpawnParent = enemySpawnParent;
            this.enemySpawnBounds = enemySpawnBounds;
            this.playerSpawnExclusionCenter = playerSpawnExclusionCenter;
            this.minHorizontalDistanceFromPlayer = Mathf.Max(0f, minHorizontalDistanceFromPlayer);
            randomRange = UnityEngine.Random.Range;
            ruleHandlers = ruleRegistry.CreateHandlers(level.GameRules);
            CurrentState = BattleGameState.NotRunning;
        }

        public BattleGame(
            LevelDefinition level,
            EnemyService enemyService,
            RuleHandlerRegistry ruleRegistry,
            Transform enemySpawnParent,
            Func<float, float, float> randomRange,
            Bounds? enemySpawnBounds = null,
            Vector3? playerSpawnExclusionCenter = null,
            float minHorizontalDistanceFromPlayer = 0f)
            : this(level, enemyService, ruleRegistry, enemySpawnParent, enemySpawnBounds, playerSpawnExclusionCenter, minHorizontalDistanceFromPlayer)
        {
            this.randomRange = randomRange ?? throw new ArgumentNullException(nameof(randomRange));
        }

        public LevelDefinition Level { get; }

        public BattleGameState CurrentState { get; private set; }

        public float ElapsedTimeSeconds { get; private set; }

        public bool IsRunning => CurrentState == BattleGameState.Running;

        public EnemyService EnemyService => enemyService;

        /// <summary>
        /// Session player spawned for this battle, when available.
        /// </summary>
        public Player SessionPlayer { get; private set; }

        private readonly EnemyService enemyService;

        private readonly Transform enemySpawnParent;

        private readonly Bounds? enemySpawnBounds;

        private readonly Vector3? playerSpawnExclusionCenter;

        private readonly float minHorizontalDistanceFromPlayer;

        private Func<float, float, float> randomRange;

        private readonly IReadOnlyList<IRuleHandler> ruleHandlers;

        private readonly List<Vector3> spawnedEnemyPositions = new List<Vector3>(32);

        public event Action<GameEndOutcome> OnCompleted;

        public void SetSessionPlayer(Player player)
        {
            SessionPlayer = player;
        }

        public void SpawnEnemyCopies(Enemy prefab, int count, Vector3 origin, float spacingPerIndex)
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

        private void SpawnEnemyCopiesCore(Enemy prefab, int count, Vector3 origin, float spacingPerIndex)
        {
            float spacing = Mathf.Max(0.25f, spacingPerIndex);
            float radiusForFallback = Mathf.Max(spacing * 2f, spacing * Mathf.Max(1f, count - 1f));
            float minSeparation = spacing * 0.75f;
            float minSeparationSqr = minSeparation * minSeparation;
            float minPlayerDistSqr = 0f;
            if (playerSpawnExclusionCenter.HasValue && minHorizontalDistanceFromPlayer > 0f)
            {
                minPlayerDistSqr = minHorizontalDistanceFromPlayer * minHorizontalDistanceFromPlayer;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 position = default;
                const int maxAttempts = 24;
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    position = GenerateCandidateSpawnPosition(origin, radiusForFallback);
                    bool sepOk = spawnedEnemyPositions.Count == 0 || !IsTooClose(position, spawnedEnemyPositions, minSeparationSqr);
                    bool playerOk = minPlayerDistSqr <= 0f
                        || !IsTooCloseHorizontally(position, playerSpawnExclusionCenter.Value, minPlayerDistSqr);
                    if (sepOk && playerOk)
                    {
                        break;
                    }
                }

                spawnedEnemyPositions.Add(position);
                enemyService.Spawn(prefab, position, Quaternion.identity, enemySpawnParent);
            }
        }

        private Vector3 GenerateCandidateSpawnPosition(Vector3 origin, float radiusForFallback)
        {
            if (enemySpawnBounds.HasValue)
            {
                Bounds b = enemySpawnBounds.Value;
                float x = randomRange(b.min.x, b.max.x);
                float z = randomRange(b.min.z, b.max.z);
                // Keep Y fixed to the arena enemy spawn plane (bounds center can sit above the floor).
                float y = origin.y;
                return new Vector3(x, y, z);
            }

            Vector3 offset = GenerateOffset(radiusForFallback);
            return origin + offset;
        }

        private Vector3 GenerateOffset(float radius)
        {
            float x = randomRange(-radius, radius);
            float z = randomRange(-radius, radius);
            return new Vector3(x, 0f, z);
        }

        private static bool IsTooClose(Vector3 candidate, List<Vector3> usedOffsets, float minSeparationSqr)
        {
            for (int i = 0; i < usedOffsets.Count; i++)
            {
                if ((usedOffsets[i] - candidate).sqrMagnitude < minSeparationSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTooCloseHorizontally(Vector3 candidate, Vector3 center, float minDistanceSqr)
        {
            float dx = candidate.x - center.x;
            float dz = candidate.z - center.z;
            return dx * dx + dz * dz < minDistanceSqr;
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
            enemyService.Tick(deltaTime);
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
                if (handler.Evaluate(this, out GameEndOutcome outcome) && outcome.Reason != GameEndReason.None)
                {
                    Complete(outcome);
                    return;
                }
            }
        }

        private void Complete(GameEndOutcome outcome)
        {
            CurrentState = BattleGameState.Done;
            OnCompleted?.Invoke(outcome);
        }
    }
}
