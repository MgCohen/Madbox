using System.Collections.Generic;
using UnityEngine;

namespace Madbox.App.GameView
{
    public sealed class PlayerBehaviorRunner : MonoBehaviour
    {
        [SerializeField] private PlayerCore playerCore;
        [SerializeField] private MonoBehaviour inputProviderComponent;
        [SerializeField] private List<MonoBehaviour> behaviorComponents;

        private IInputContextProvider inputProvider;
        private readonly List<IPlayerBehavior> behaviors = new();
        private IPlayerBehavior activeBehavior;

        private void Awake()
        {
            inputProvider = ResolveInputProvider();
            ResolveBehaviors();
        }

        private void Update()
        {
            if (playerCore == null || inputProvider == null) return;
            TickBehaviors();
        }

        private IInputContextProvider ResolveInputProvider()
        {
            if (inputProviderComponent != null) return (IInputContextProvider)inputProviderComponent;
            return FindObjectOfType<PlayerInputProvider>();
        }

        private void ResolveBehaviors()
        {
            if (behaviorComponents == null) return;
            foreach (MonoBehaviour component in behaviorComponents)
            {
                if (component is IPlayerBehavior behavior) behaviors.Add(behavior);
            }
        }

        private void TickBehaviors()
        {
            PlayerState state = playerCore.State;
            InputContext input = inputProvider.Current;

            IPlayerBehavior winner = null;
            foreach (IPlayerBehavior behavior in behaviors)
            {
                if (behavior.CanTakeControl(state, in input))
                {
                    winner = behavior;
                    break;
                }
            }

            if (winner != activeBehavior)
            {
                activeBehavior?.OnExitControl(state, in input);
                winner?.OnEnterControl(state, in input);
                activeBehavior = winner;
            }

            if (activeBehavior == null)
            {
                inputProvider.EndFrame();
                return;
            }

            activeBehavior.Tick(state, in input, Time.deltaTime);
            inputProvider.EndFrame();
        }
    }
}
