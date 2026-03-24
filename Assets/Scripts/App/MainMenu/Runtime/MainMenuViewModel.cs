using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Madbox.App.Gameplay;
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
            if (levelService != null)
            {
                levelService.AvailableLevelsChanged -= OnLevelProgressionChanged;
                levelService.AvailableLevelsChanged += OnLevelProgressionChanged;
            }

            SyncAvailableLevelsFromService();
            Wallet = goldService.GetWallet();
        }

        protected override void OnClosed()
        {
            if (levelService != null)
            {
                levelService.AvailableLevelsChanged -= OnLevelProgressionChanged;
            }
            base.OnClosed();
        }

        private void OnLevelProgressionChanged()
        {
            SyncAvailableLevelsFromService();
        }

        private void SyncAvailableLevelsFromService()
        {
            AvailableLevels.Clear();
            if (levelService == null)
            {
                return;
            }

            foreach (AvailableLevel level in levelService.GetAvailableLevels())
            {
                if (level?.Definition != null)
                {
                    AvailableLevels.Add(level);
                }
            }
        }

        public void AddOneGold()
        {
            if (goldService == null)
            {
                return;
            }
            goldService.Add(1);
        }

        public void PlayLevel(AvailableLevel entry)
        {
            if (entry?.Definition == null || navigation == null)
            {
                return;
            }

            if (entry.IsBlocked)
            {
                return;
            }

            navigation.Open(new GameViewModel(entry.Definition), closeCurrent: false);
        }
    }
}
