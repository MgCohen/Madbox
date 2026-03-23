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
        where TData : EntityData
    {
        [SerializeField]
        [FormerlySerializedAs("playerCore")]
        [FormerlySerializedAs("playerData")]
        private TData entityData;

        [SerializeField]
        [FormerlySerializedAs("inputProvider")]
        private MonoBehaviour inputProviderBehaviour;

        [SerializeField]
        private List<MonoBehaviour> behaviorComponents = new List<MonoBehaviour>();

        private readonly List<IEntityBehavior<TData, TInput>> behaviors = new List<IEntityBehavior<TData, TInput>>();

        private IEntityFrameInputProvider<TInput> inputProvider;

        private IEntityBehavior<TData, TInput> lastExecutedBehavior;

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
            if (entityData == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            TInput input = inputProvider != null ? inputProvider.GetFrameInput() : default;
            IEntityBehavior<TData, TInput> winner = null;
            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i].TryAcceptControl(entityData, in input))
                {
                    winner = behaviors[i];
                    break;
                }
            }

            if (winner != lastExecutedBehavior)
            {
                lastExecutedBehavior?.OnQuit(entityData);
                lastExecutedBehavior = winner;
            }

            if (winner != null)
            {
                winner.Execute(entityData, in input, dt);
            }
        }
    }
}
