using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Madbox.Gold;
using Madbox.Gold.Contracts;
using Madbox.Level;
using Scaffold.MVVM;
using VContainer;

namespace Madbox.App.MainMenu
{
    public partial class MainMenuViewModel : ViewModel
    {
        [ObservableProperty] private int gold;
        [ObservableProperty] private IReadOnlyList<AvailableLevel> availableLevels = Array.Empty<AvailableLevel>();
        [ObservableProperty] private GoldWallet wallet = new GoldWallet();

        private IGoldService goldService;

        [Inject]
        public void Construct(IGoldService goldService, ILevelService levelMenuService)
        {
            EnsureAvailableLevels(levelMenuService);
            ApplyGoldFromService(goldService);
        }

        private void EnsureAvailableLevels(ILevelService levelMenuService)
        {
            if (levelMenuService == null)
            {
                AvailableLevels = Array.Empty<AvailableLevel>();
                return;
            }
            AvailableLevels = levelMenuService.GetAvailableLevels();
        }

        private void ApplyGoldFromService(IGoldService goldService)
        {
            if (goldService == null)
            {
                return;
            }
            this.goldService = goldService;
            Wallet = goldService.GetWallet();
        }

        protected override void Initialize()
        {
            Bind(() => Wallet.CurrentGold, () => Gold);
            Gold = Wallet.CurrentGold;
        }

        public void AddOneGold()
        {
            if (goldService == null)
            {
                return;
            }
            goldService.Add(1);
        }
    }
}
