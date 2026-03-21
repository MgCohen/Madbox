using Madbox.V2.Levels.Rules;

namespace Madbox.V2.Battle
{
    public interface IRuleHandlerV2
    {
        bool Evaluate(GameV2 game, out GameEndReasonV2 reason);
    }
}
