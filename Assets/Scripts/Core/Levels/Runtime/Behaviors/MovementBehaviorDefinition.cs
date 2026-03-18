using System;

namespace Madbox.Levels.Behaviors
{
    [Serializable]
    public record MovementBehaviorDefinition(float MoveSpeed, float FollowRange) : EnemyBehaviorDefinition;
}
