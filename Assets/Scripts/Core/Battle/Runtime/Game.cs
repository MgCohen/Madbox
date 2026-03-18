using System;
using Madbox.Gold;
using Madbox.Levels;
#pragma warning disable SCA0003
#pragma warning disable SCA0004
#pragma warning disable SCA0005
#pragma warning disable SCA0006
#pragma warning disable SCA0009
#pragma warning disable SCA0012
#pragma warning disable SCA0014
#pragma warning disable SCA0017
#pragma warning disable SCA0020

namespace Madbox.Battle
{
    public class Game
    {
        private readonly LevelDefinition levelDefinition;
        private readonly GoldWallet goldWallet;
        private readonly EnemyService enemyService;
        private readonly BattleEventRouter eventRouter;
        private readonly GameRuleEvaluator gameRuleEvaluator;
        private readonly Player player;
        private bool completionRaised;
        private float elapsedTimeSeconds;

        public Game(LevelDefinition levelDefinition, GoldWallet goldWallet, EntityId playerId, int playerMaxHealth = 100)
        {
            this.levelDefinition = levelDefinition ?? throw new ArgumentNullException(nameof(levelDefinition));
            this.goldWallet = goldWallet ?? throw new ArgumentNullException(nameof(goldWallet));
            if (playerId == null)
            {
                throw new ArgumentNullException(nameof(playerId));
            }

            CurrentLevelId = levelDefinition.LevelId ?? throw new ArgumentException("LevelDefinition.LevelId is required.", nameof(levelDefinition));

            player = new Player(playerId, playerMaxHealth);
            enemyService = new EnemyService(levelDefinition);
            eventRouter = new BattleEventRouter(enemyService, player, RaiseEvent);
            gameRuleEvaluator = new GameRuleEvaluator(levelDefinition);

            CurrentState = GameState.NotRunning;
        }

        public event Action<BattleEvent> EventTriggered;

        public event Action<GameEndReason> OnCompleted;

        public GameState CurrentState { get; private set; }

        public float ElapsedTimeSeconds => elapsedTimeSeconds;

        public LevelId CurrentLevelId { get; }

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

            elapsedTimeSeconds += deltaTime;
            enemyService.Tick(deltaTime);
            EvaluateGameRules();
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

            eventRouter.Route(gameEvent);
        }

        public bool TryGetEnemyDistance(EntityId enemyId, out float distance)
        {
            if (enemyId == null)
            {
                distance = default;
                return false;
            }

            return enemyService.TryGetEnemyDistance(enemyId, out distance);
        }

        private void RaiseEvent(BattleEvent battleEvent)
        {
            EventTriggered?.Invoke(battleEvent);
        }

        private void EvaluateGameRules()
        {
            if (completionRaised)
            {
                return;
            }

            if (gameRuleEvaluator.TryEvaluate(CurrentState, player, enemyService, out GameEndReason reason) == false)
            {
                return;
            }

            Complete(reason);
        }

        private void Complete(GameEndReason reason)
        {
            completionRaised = true;
            CurrentState = GameState.Done;

            if (reason == GameEndReason.Win && levelDefinition.GoldReward > 0)
            {
                goldWallet.Add(levelDefinition.GoldReward);
            }

            OnCompleted?.Invoke(reason);
        }
    }
}
