using System.Collections.Generic;

namespace Madbox.Enemies
{
    /// <summary>
    /// Per-frame context shared by enemy actor behaviors (priority runner).
    /// </summary>
    public struct EnemyBehaviorTickContext
    {
        public UnityEngine.Transform Self;
        public UnityEngine.Transform Target;
        public UnityEngine.Rigidbody Body;
    }

    /// <summary>
    /// Single enemy behavior evaluated in list order each frame. The first behavior that returns true wins;
    /// lower-priority behaviors are skipped for that frame (same pattern as a player priority stack).
    /// </summary>
    public interface IEnemyActorBehavior
    {
        bool TryExecute(ref EnemyBehaviorTickContext context, float deltaTime);
    }

    /// <summary>
    /// Runs a fixed behavior list once per frame: walks in order and executes the first winner only.
    /// </summary>
    public static class EnemyBehaviorRunner
    {
        public static void RunFrame(IReadOnlyList<IEnemyActorBehavior> behaviors, ref EnemyBehaviorTickContext context, float deltaTime)
        {
            if (behaviors == null || behaviors.Count == 0 || deltaTime <= 0f)
            {
                return;
            }

            for (int i = 0; i < behaviors.Count; i++)
            {
                IEnemyActorBehavior behavior = behaviors[i];
                if (behavior == null)
                {
                    continue;
                }

                if (behavior.TryExecute(ref context, deltaTime))
                {
                    return;
                }
            }
        }
    }
}
