using Madbox.Levels.Rules;

namespace Madbox.Battle
{
    public interface IRuleHandler
    {
        bool Evaluate(BattleGame game, out GameEndReason reason);
    }
}
