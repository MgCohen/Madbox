using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Madbox.Entities;
using Madbox.Players;
using Scaffold.MVVM;
using UnityEngine;

namespace Madbox.App.Gameplay
{
    public partial class PlayerViewModel : ViewModel
    {
        private readonly Damageable damageable;

        [ObservableProperty]
        private int currentHealth;

        public PlayerViewModel(Player player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            Damageable found = player.GetComponentInChildren<Damageable>(true);
            if (found == null)
            {
                throw new InvalidOperationException("Player must have a Damageable component in its hierarchy.");
            }

            damageable = found;
        }

        protected override void Initialize()
        {
            base.Initialize();
            damageable.Damaged += OnDamageableHealthChanged;
            damageable.Died += OnDamageableDied;
            RefreshFromDamageable();
        }

        /// <summary>
        /// Stops mirroring HP (e.g. when the session player is cleared or replaced). Safe to call more than once.
        /// </summary>
        public void TearDown()
        {
            OnClosed();
        }

        protected override void OnClosed()
        {
            damageable.Damaged -= OnDamageableHealthChanged;
            damageable.Died -= OnDamageableDied;
            CurrentHealth = 0;
            base.OnClosed();
        }

        private void OnDamageableHealthChanged(object _, DamagedEventArgs __)
        {
            RefreshFromDamageable();
        }

        private void OnDamageableDied(object _, EventArgs __)
        {
            RefreshFromDamageable();
        }

        private void RefreshFromDamageable()
        {
            CurrentHealth = Mathf.CeilToInt(damageable.CurrentHp);
        }
    }
}
