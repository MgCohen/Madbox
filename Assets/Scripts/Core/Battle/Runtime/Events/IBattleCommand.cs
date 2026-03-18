using System;

namespace Madbox.Battle.Events
{
    internal interface IBattleCommand
    {
        void Execute(BattleExecutionContext context);
    }
}
