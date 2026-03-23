using System;
using System.Collections.Generic;
using Madbox.App.GameView.Animation;
using Madbox.App.GameView.Input;
using UnityEngine;
using UnityEngine.Serialization;

namespace Madbox.App.GameView.Player
{
    /// <summary>
    /// Runs ordered <see cref="IPlayerBehavior"/> components; first accepting behavior wins each frame.
    /// Uses <see cref="PlayerInputProvider.GetInputContext"/> once per frame.
    /// Tracks the active flow and calls <see cref="IPlayerBehavior.OnQuit"/> when it ends or when switching to another flow.
    /// </summary>
    public sealed class PlayerBehaviorRunner : MonoBehaviour
    {
        [SerializeField]
        [FormerlySerializedAs("playerCore")]
        private PlayerData playerData;

        [SerializeField]
        private PlayerInputProvider inputProvider;

        [SerializeField]
        private List<MonoBehaviour> behaviorComponents = new List<MonoBehaviour>();

        private readonly List<IPlayerBehavior> behaviors = new List<IPlayerBehavior>();

        private IPlayerBehavior lastExecutedBehavior;

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
            if (playerData == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            PlayerInputContext input = inputProvider != null ? inputProvider.GetInputContext() : default;
            IPlayerBehavior winner = null;
            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i].TryAcceptControl(playerData, in input))
                {
                    winner = behaviors[i];
                    break;
                }
            }

            if (winner != lastExecutedBehavior)
            {
                lastExecutedBehavior?.OnQuit(playerData);
                lastExecutedBehavior = winner;
            }

            if (winner != null)
            {
                winner.Execute(playerData, in input, dt);
            }
        }
    }

    /// <summary>
    /// Pushes <see cref="PlayerData"/> attribute values into animator parameters when values change and on enable.
    /// </summary>
    public sealed class PlayerAttributeAnimatorDriver : MonoBehaviour
    {
        [Serializable]
        private sealed class PlayerAttributeAnimatorLink
        {
            [SerializeField]
            private PlayerAttribute playerAttribute;

            [SerializeField]
            private AnimationAttribute animatorParameter;

            [SerializeField]
            private bool useBoolParameter;

            public PlayerAttribute PlayerAttribute => playerAttribute;

            public void Apply(AnimationController controller, float value)
            {
                if (controller == null || animatorParameter == null)
                {
                    return;
                }

                if (useBoolParameter)
                {
                    controller.SetBool(animatorParameter, value > 0.5f);
                }
                else
                {
                    controller.SetFloat(animatorParameter, value);
                }
            }
        }

        [SerializeField]
        [FormerlySerializedAs("viewData")]
        private PlayerData playerData;

        [SerializeField]
        private AnimationController animationController;

        [SerializeField]
        private List<PlayerAttributeAnimatorLink> bindings = new List<PlayerAttributeAnimatorLink>();

        private void Awake()
        {
            if (animationController == null)
            {
                animationController = GetComponent<AnimationController>();
            }

            if (playerData == null)
            {
                playerData = GetComponentInParent<PlayerData>();
            }
        }

        private void OnEnable()
        {
            if (playerData != null)
            {
                playerData.AttributeValueChanged += OnAttributeValueChanged;
            }

            PushAll();
        }

        private void OnDisable()
        {
            if (playerData != null)
            {
                playerData.AttributeValueChanged -= OnAttributeValueChanged;
            }
        }

        private void OnAttributeValueChanged(PlayerAttribute attribute, float value)
        {
            ApplyBinding(attribute, value);
        }

        private void PushAll()
        {
            if (playerData == null || animationController == null)
            {
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                PlayerAttributeAnimatorLink link = bindings[i];
                if (link.PlayerAttribute == null)
                {
                    continue;
                }

                float v = playerData.GetFloatAttribute(link.PlayerAttribute);
                link.Apply(animationController, v);
            }
        }

        private void ApplyBinding(PlayerAttribute attribute, float value)
        {
            if (animationController == null || attribute == null)
            {
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].PlayerAttribute == attribute)
                {
                    bindings[i].Apply(animationController, value);
                    return;
                }
            }
        }
    }
}