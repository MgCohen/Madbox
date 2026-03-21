using UnityEngine;

namespace Madbox.V2.Levels.Rules
{
    [CreateAssetMenu(menuName = "Madbox/V2/Rules/Time Elapsed Complete", fileName = "TimeElapsedCompleteRuleV2")]
    public sealed class TimeElapsedCompleteRuleV2 : LevelRuleDefinitionV2
    {
        public float ElapsedSeconds => elapsedSeconds;

        public GameEndReasonV2 CompletionReason => endReason;

        [SerializeField] private float elapsedSeconds = 30f;
        [SerializeField] private GameEndReasonV2 endReason = GameEndReasonV2.Win;
    }
}
