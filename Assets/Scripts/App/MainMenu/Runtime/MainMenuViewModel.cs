using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Madbox.Gold;
using Madbox.Gold.Contracts;
using Madbox.Levels;
using Scaffold.MVVM;
using VContainer;

namespace Madbox.App.MainMenu
{
    public partial class MainMenuViewModel : ViewModel
    {
        [ObservableProperty] private ObservableCollection<AvailableLevel> availableLevels = new ObservableCollection<AvailableLevel>();
        [ObservableProperty] private GoldWallet wallet = new GoldWallet();

        [Inject] private IGoldService goldService;
        [Inject] private ILevelService levelService;

        protected override void Initialize()
        {
            AvailableLevels.Clear();
            foreach (AvailableLevel level in levelService.GetAvailableLevels())
            {
                if (level?.Definition != null)
                {
                    AvailableLevels.Add(level);
                }
            }

            Wallet = goldService.GetWallet();
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
