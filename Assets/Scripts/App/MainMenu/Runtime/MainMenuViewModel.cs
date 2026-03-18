using CommunityToolkit.Mvvm.ComponentModel;
using Madbox.Gold.Contracts;
using Scaffold.MVVM;
using VContainer;

namespace Madbox.App.MainMenu
{
    public partial class MainMenuViewModel : ViewModel
    {
        [ObservableProperty] private MainMenuModel model = new MainMenuModel();
        [ObservableProperty] private int gold;

        private IGoldService goldService;

        [Inject] public void Construct(IGoldService goldService)
        {
            if (goldService == null) { return; }
            UnregisterGoldService();
            this.goldService = goldService;
            RegisterGoldService();
            SyncGold(goldService.CurrentGold);
        }

        protected override void Initialize()
        {
            Bind(() => Model.Gold, () => Gold);
            Gold = Model.Gold;
        }

        public void AddOneGold()
        {
            if (goldService == null) { return; }
            goldService.Add(1);
        }

        protected override void OnClosed()
        {
            UnregisterGoldService();
        }

        private void RegisterGoldService()
        {
            goldService.GoldChanged += HandleGoldChanged;
        }

        private void UnregisterGoldService()
        {
            if (goldService == null) { return; }
            goldService.GoldChanged -= HandleGoldChanged;
        }

        private void HandleGoldChanged(int value)
        {
            SyncGold(value);
        }

        private void SyncGold(int value)
        {
            Model.Gold = value;
        }
    }
}
