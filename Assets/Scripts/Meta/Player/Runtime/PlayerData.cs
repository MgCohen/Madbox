using Madbox.Entities;
using UnityEngine;

namespace Madbox.Player
{
    /// <summary>
    /// Player entity data: <see cref="IsAlive"/> and <see cref="CanMove"/> use dedicated attributes and must have matching entries in the inherited list.
    /// </summary>
    public sealed class PlayerData : EntityData
    {
        [SerializeField]
        private PlayerAttribute isAliveAttribute;

        [SerializeField]
        private PlayerAttribute canMoveAttribute;

        public bool IsAlive
        {
            get => GetBoolAttribute(isAliveAttribute);
            set => SetBoolAttribute(isAliveAttribute, value);
        }

        public bool CanMove
        {
            get => GetBoolAttribute(canMoveAttribute);
            set => SetBoolAttribute(canMoveAttribute, value);
        }
    }
}
