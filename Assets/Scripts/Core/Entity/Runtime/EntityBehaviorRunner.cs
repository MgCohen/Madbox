using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Madbox.Entities
{
    /// <summary>
    /// Runs ordered <see cref="IEntityBehavior{TData,TInput}"/> components; first accepting behavior wins each frame.
    /// Tracks the active flow and calls <see cref="IEntityBehavior{TData,TInput}.OnQuit"/> when it ends or when switching to another flow.
    /// </summary>
    public class EntityBehaviorRunner<TData, TInput> : MonoBehaviour
        where TData : Entity
    {
        [SerializeField]
        [FormerlySerializedAs("playerCore")]
        [FormerlySerializedAs("Player")]
        protected TData Entity;

        [SerializeField]
        [FormerlySerializedAs("inputProvider")]
        private MonoBehaviour inputProviderBehaviour;

        [SerializeField]
        private List<MonoBehaviour> behaviorComponents = new List<MonoBehaviour>();

        private readonly List<IEntityBehavior<TData, TInput>> behaviors = new List<IEntityBehavior<TData, TInput>>();

        private IEntityFrameInputProvider<TInput> inputProvider;

        private IEntityBehavior<TData, TInput> lastExecutedBehavior;

        /// <summary>
        /// When false, the runner skips the frame (e.g. entity not ready after pool get).
        /// </summary>
        protected virtual bool ShouldRunTick()
        {
            return true;
        }

        public void ForceQuitActiveBehavior()
        {
            if (lastExecutedBehavior != null)
            {
                lastExecutedBehavior.OnQuit(Entity);
                lastExecutedBehavior = null;
            }
        }

        /// <summary>
        /// Binds input after spawn when the provider lives outside the entity prefab (e.g. GameView UI in the bootstrap scene).
        /// </summary>
        public void AssignInputProvider(IEntityFrameInputProvider<TInput> provider)
        {
            if (provider == null)
            {
                throw new System.ArgumentNullException(nameof(provider));
            }

            inputProvider = provider;
            inputProviderBehaviour = provider as MonoBehaviour;
        }

        private void Awake()
        {
            inputProvider = inputProviderBehaviour as IEntityFrameInputProvider<TInput>;

            behaviors.Clear();
            for (int i = 0; i < behaviorComponents.Count; i++)
            {
                if (behaviorComponents[i] is IEntityBehavior<TData, TInput> b)
                {
                    behaviors.Add(b);
                }
            }
        }

        private void Update()
        {
            if (Entity == null)
            {
                return;
            }

            if (ShouldRunTick() == false)
            {
                return;
            }

            float dt = Time.deltaTime;
            TInput input = inputProvider != null ? inputProvider.GetFrameInput() : default;
            IEntityBehavior<TData, TInput> winner = null;
            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i].TryAcceptControl(Entity, in input))
                {
                    winner = behaviors[i];
                    break;
                }
            }

            if (winner != lastExecutedBehavior)
            {
                lastExecutedBehavior?.OnQuit(Entity);
                lastExecutedBehavior = winner;
            }

            if (winner != null)
            {
                winner.Execute(Entity, in input, dt);
            }
        }
    }
}
