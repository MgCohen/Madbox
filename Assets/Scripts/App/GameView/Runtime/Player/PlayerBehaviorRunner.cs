using System.Collections.Generic;
using UnityEngine;

namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Runs ordered <see cref="IPlayerBehavior"/> components; first accepting behavior wins each frame.
    /// </summary>
    public sealed class PlayerBehaviorRunner : MonoBehaviour
    {
        [SerializeField]
        private PlayerCore playerCore;

        [SerializeField]
        private PlayerAnimationController locomotionAnimation;

        [SerializeField]
        private List<MonoBehaviour> behaviorComponents = new List<MonoBehaviour>();

        private readonly List<IPlayerBehavior> behaviors = new List<IPlayerBehavior>();

        private void Awake()
        {
            behaviors.Clear();
            for (int i = 0; i < behaviorComponents.Count; i++)
            {
                if (behaviorComponents[i] is IPlayerBehavior b)
                {
                    behaviors.Add(b);
                }
            }
        }

        private void Update()
        {
            if (playerCore == null || playerCore.ViewData == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i].TryAcceptControl(playerCore))
                {
                    behaviors[i].Execute(playerCore, dt);
                    return;
                }
            }

            if (locomotionAnimation != null)
            {
                locomotionAnimation.SetLocomotionMoving(false);
            }
        }
    }
}
