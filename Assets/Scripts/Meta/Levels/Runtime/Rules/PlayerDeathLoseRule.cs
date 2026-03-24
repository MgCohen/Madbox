using UnityEngine;

namespace Madbox.Levels.Rules
{
    [CreateAssetMenu(menuName = "Madbox/Levels/Rules/Player Death (Lose)", fileName = "PlayerDeathLoseRule")]
    public sealed class PlayerDeathLoseRule : LevelRuleDefinition
    {
        public GameEndReason CompletionReason => endReason;

        [SerializeField] private GameEndReason endReason = GameEndReason.Lose;
    }
}
