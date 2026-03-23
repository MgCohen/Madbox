using System;
using System.Collections.Generic;
using Madbox.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Madbox.App.Animation
{
    /// <summary>
    /// Pushes <see cref="EntityData"/> attribute values into animator parameters when values change and on enable.
    /// </summary>
    public class EntityAttributeAnimatorDriver<TData> : MonoBehaviour
        where TData : EntityData
    {
        [Serializable]
        private sealed class EntityAttributeAnimatorLink
        {
            [SerializeField]
            private EntityAttribute playerAttribute;

            [SerializeField]
            private AnimationAttribute animatorParameter;

            [SerializeField]
            private bool useBoolParameter;

            public EntityAttribute EntityAttribute => playerAttribute;

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
        [FormerlySerializedAs("playerData")]
        private TData entityData;

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

            if (entityData == null)
            {
                entityData = GetComponentInParent<TData>();
            }
        }

        private void OnEnable()
        {
            if (entityData != null)
            {
                entityData.AttributeValueChanged += OnAttributeValueChanged;
            }

            PushAll();
        }

        private void OnDisable()
        {
            if (entityData != null)
            {
                entityData.AttributeValueChanged -= OnAttributeValueChanged;
            }
        }

        private void OnAttributeValueChanged(EntityAttribute attribute, float value)
        {
            ApplyBinding(attribute, value);
        }

        private void PushAll()
        {
            if (entityData == null || animationController == null)
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

                float v = entityData.GetFloatAttribute(link.EntityAttribute);
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
