using System;
using System.Collections.Generic;
using Madbox.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Madbox.App.Animation
{
    /// <summary>
    /// Pushes <see cref="Entity"/> attribute values into animator parameters when values change and on enable.
    /// </summary>
    public class EntityAttributeAnimatorDriver : MonoBehaviour
    {
        [Serializable]
        private sealed class EntityAttributeAnimatorLink
        {
            [SerializeField]
            [FormerlySerializedAs("playerAttribute")]
            private EntityAttribute attribute;

            [SerializeField]
            private AnimationAttribute animatorParameter;

            [SerializeField]
            private bool useBoolParameter;

            public EntityAttribute EntityAttribute => attribute;

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
        [FormerlySerializedAs("Player")]
        [FormerlySerializedAs("Entity")]
        private Entity entity;

        [SerializeField]
        private AnimationController animationController;

        [SerializeField]
        private List<EntityAttributeAnimatorLink> bindings = new List<EntityAttributeAnimatorLink>();

        private void Awake()
        {
            if (animationController == null)
            {
                animationController = GetComponent<AnimationController>();
            }

            if (entity == null)
            {
                entity = GetComponentInParent<Entity>();
            }
        }

        private void OnEnable()
        {
            if (entity != null)
            {
                entity.AttributeValueChanged += OnAttributeValueChanged;
            }

            PushAll();
        }

        private void OnDisable()
        {
            if (entity != null)
            {
                entity.AttributeValueChanged -= OnAttributeValueChanged;
            }
        }

        private void OnAttributeValueChanged(EntityAttribute attribute, float value)
        {
            ApplyBinding(attribute, value);
        }

        private void PushAll()
        {
            if (entity == null || animationController == null)
            {
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                EntityAttributeAnimatorLink link = bindings[i];
                if (link.EntityAttribute == null)
                {
                    continue;
                }

                float v = entity.GetFloatAttribute(link.EntityAttribute);
                link.Apply(animationController, v);
            }
        }

        private void ApplyBinding(EntityAttribute attribute, float value)
        {
            if (animationController == null || attribute == null)
            {
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].EntityAttribute == attribute)
                {
                    bindings[i].Apply(animationController, value);
                    return;
                }
            }
        }
    }
}
