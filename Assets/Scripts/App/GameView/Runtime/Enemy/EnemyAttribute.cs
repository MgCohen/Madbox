using Madbox.Entity;
using UnityEngine;

namespace Madbox.App.GameView.Enemy
{
    /// <summary>
    /// Enemy-specific <see cref="EntityAttribute"/> asset for stats used by enemy view logic.
    /// </summary>
    [CreateAssetMenu(menuName = "Madbox/Enemy/Enemy Attribute", fileName = "EnemyAttribute")]
    public sealed class EnemyAttribute : EntityAttribute
    {
    }
}
