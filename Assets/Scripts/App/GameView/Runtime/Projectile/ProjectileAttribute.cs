using Madbox.Entity;
using UnityEngine;

namespace Madbox.App.GameView.Projectile
{
    /// <summary>
    /// Projectile-specific <see cref="EntityAttribute"/> (e.g. damage, speed).
    /// </summary>
    [CreateAssetMenu(menuName = "Madbox/Projectile/Projectile Attribute", fileName = "ProjectileAttribute")]
    public sealed class ProjectileAttribute : EntityAttribute
    {
    }
}
