using Madbox.Entity;
using UnityEngine;

namespace Madbox.App.GameView.Projectile
{
    /// <summary>
    /// Projectile stats as attributes (damage, speed); list matching <see cref="ProjectileAttribute"/> entries on the component.
    /// </summary>
    public sealed class ProjectileData : EntityData
    {
        [SerializeField]
        private ProjectileAttribute damageAttribute;

        [SerializeField]
        private ProjectileAttribute speedAttribute;

        public float Damage
        {
            get => GetFloatAttribute(damageAttribute);
            set => SetFloatAttribute(damageAttribute, value);
        }

        public float Speed
        {
            get => GetFloatAttribute(speedAttribute);
            set => SetFloatAttribute(speedAttribute, value);
        }
    }
}
