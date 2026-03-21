using System;
using Madbox.Gold;
using Madbox.Battle.Events;
using Madbox.Battle.Rules;
using Madbox.Enemies.Services;
using Madbox.Levels;
using Madbox.Levels.Rules;

namespace Madbox.Battle
{
    public class Game
    {
        public Game(LevelDefinition levelDefinition, GoldWallet goldWallet, Player player)
        {
            if (levelDefinition == null)
            {
                throw new ArgumentNullException(nameof(levelDefinition));
            }

            if (goldWallet == null)
            {
                throw new ArgumentNullException(nameof(goldWallet));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            this.levelDefinition = levelDefinition;
            this.goldWallet = goldWallet;
            this.player = player;
            CurrentLevelId = levelDefinition.LevelId ?? throw new ArgumentException("LevelDefinition.LevelId is required.", nameof(levelDefinition));

            enemyService = new EnemyService(levelDefinition);
            eventRouter = new BattleEventRouter(enemyService, player, evt => EventTriggered?.Invoke(evt));
            gameRuleEvaluator = new GameRuleEvaluator(levelDefinition);

            CurrentState = GameState.NotRunning;
        }

        public GameState CurrentState { get; private set; }

        public float ElapsedTimeSeconds => elapsedTimeSeconds;

        public LevelId CurrentLevelId { get; }

        private readonly LevelDefinition levelDefinition;
        private readonly GoldWallet goldWallet;
        private readonly EnemyService enemyService;
        private readonly BattleEventRouter eventRouter;
        private readonly GameRuleEvaluator gameRuleEvaluator;
        private readonly Player player;
        private float elapsedTimeSeconds;

        public event Action<BattleEvent> EventTriggered;

        public event Action<GameEndReason> OnCompleted;

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
            if (CanTick(deltaTime) == false)
            {
                return;
            }

            AdvanceTick(deltaTime);
            EvaluateGameRules();
        }

        public void Trigger(BattleEvent gameEvent)
        {
            if (CanTrigger(gameEvent) == false)
            {
                return;
            }

            eventRouter.Route(gameEvent);
        }

        private bool CanTick(float deltaTime)
        {
            return CurrentState == GameState.Running && deltaTime > 0f;
        }

        private void AdvanceTick(float deltaTime)
        {
            elapsedTimeSeconds += deltaTime;
            eventRouter.Tick(deltaTime);
            enemyService.Tick(deltaTime);
        }

        private bool CanTrigger(BattleEvent gameEvent)
        {
            if (gameEvent == null)
            {
                throw new ArgumentNullException(nameof(gameEvent));
            }

            return CurrentState == GameState.Running;
        }

        private void EvaluateGameRules()
        {
            if (CurrentState is not GameState.Running) return;
            if (gameRuleEvaluator.TryEvaluate(CurrentState, elapsedTimeSeconds, player, enemyService, out GameEndReason reason))
            {
                Complete(reason);
            }
        }

        private void Complete(GameEndReason reason)
        {
            CurrentState = GameState.Done;
            AwardWinReward(reason);
            OnCompleted?.Invoke(reason);
        }

        private void AwardWinReward(GameEndReason reason)
        {
            if (reason == GameEndReason.Win && levelDefinition.GoldReward > 0)
            {
                goldWallet.Add(levelDefinition.GoldReward);
            }
        }
    }
}

