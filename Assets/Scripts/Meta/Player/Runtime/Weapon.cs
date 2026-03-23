using System.Collections.Generic;
using Madbox.Entities;
using UnityEngine;

namespace Madbox.Players
{
    public sealed class Weapon : MonoBehaviour
    {
        public IReadOnlyList<EntityAttributeModifierEntry> Modifiers => modifiers;

        [SerializeField]
        private List<EntityAttributeModifierEntry> modifiers = new List<EntityAttributeModifierEntry>();
    }
}
