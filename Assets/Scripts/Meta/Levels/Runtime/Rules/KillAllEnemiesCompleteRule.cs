using UnityEngine;

namespace Madbox.Levels.Rules
{
    [CreateAssetMenu(menuName = "Madbox/Levels/Rules/Kill All Enemies Complete", fileName = "KillAllEnemiesCompleteRule")]
    public sealed class KillAllEnemiesCompleteRule : LevelRuleDefinition
    {
        public GameEndReason CompletionReason => endReason;

        [SerializeField] private GameEndReason endReason = GameEndReason.Win;
    }
}
