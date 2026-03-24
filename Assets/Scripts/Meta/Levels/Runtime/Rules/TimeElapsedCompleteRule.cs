using UnityEngine;

namespace Madbox.Levels.Rules
{
    [CreateAssetMenu(menuName = "Madbox/Levels/Rules/Time Elapsed Complete", fileName = "TimeElapsedCompleteRule")]
    public sealed class TimeElapsedCompleteRule : LevelRuleDefinition
    {
        public float ElapsedSeconds => elapsedSeconds;

        public GameEndReason CompletionReason => endReason;

        [SerializeField] private float elapsedSeconds = 30f;
        [SerializeField] private GameEndReason endReason = GameEndReason.Win;
    }
}
