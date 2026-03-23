using System;
using System.Collections.Generic;
using Madbox.Entity;
using UnityEngine;

namespace Madbox.App.GameView.Player
{
    public sealed class Weapon : MonoBehaviour
    {
        public IReadOnlyList<EntityAttributeModifierEntry> Modifiers =>
            modifiers ?? (IReadOnlyList<EntityAttributeModifierEntry>)Array.Empty<EntityAttributeModifierEntry>();

        [SerializeField]
        private List<EntityAttributeModifierEntry> modifiers = new List<EntityAttributeModifierEntry>();
    }
}
