using System;
using UnityEngine;

namespace Madbox.Entities
{
    /// <summary>
    /// Adds <see cref="Delta"/> to the effective value of <see cref="Attribute"/> on <see cref="Entity"/>.
    /// </summary>
    [Serializable]
    public sealed class EntityAttributeModifierEntry
    {
        [SerializeField]
        private EntityAttribute attribute;

        [SerializeField]
        private float delta;

        public EntityAttribute Attribute => attribute;

        public float Delta => delta;

        public EntityAttributeModifierEntry(EntityAttribute attribute, float delta)
        {
            this.attribute = attribute;
            this.delta = delta;
        }
    }
}
