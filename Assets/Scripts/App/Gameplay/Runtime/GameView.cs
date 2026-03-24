using Scaffold.MVVM;
using Scaffold.Navigation.Contracts;
using TMPro;
using Madbox.Levels.Rules;
using Madbox.Players;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Madbox.App.Gameplay
{
    public sealed class GameView : UIView<GameViewModel>
    {
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private GameObject endStatePopupRoot;
        [SerializeField] private TMP_Text endStateLabel;
        [SerializeField] private PlayerHealthHudView playerHealthHudView;
        [SerializeField] private GameObject loadingViewRoot;
        [SerializeField] private Button endStateCloseButton;
        [SerializeField] private Button[] weaponSlotButtons;

        private PlayerViewModel lastBoundPlayerHud;

        private UnityAction[] weaponSlotClickActions;

        protected override void OnBind()
        {
            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(OnBackClicked);
            }

            if (endStateCloseButton != null)
            {
                endStateCloseButton.onClick.AddListener(OnBackClicked);
            }

            BindWeaponSlotButtons();

            if (endStatePopupRoot != null)
            {
                endStatePopupRoot.SetActive(false);
            }

            if (viewModel != null)
            {
                viewModel.SessionCompleted += HandleSessionCompleted;
                viewModel.SessionReady += HandleSessionReady;
            }

            lastBoundPlayerHud = null;
            viewModel?.SyncSessionPlayer(null);
            if (playerHealthHudView != null && viewModel != null)
            {
                playerHealthHudView.Bind(this, viewModel.Player);
                lastBoundPlayerHud = viewModel.Player;
            }

            SetLoadingVisible(true);
            viewModel?.BeginSessionLoad(this);
        }

        protected override void OnUnbind()
        {
            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveListener(OnBackClicked);
            }

            if (endStateCloseButton != null)
            {
                endStateCloseButton.onClick.RemoveListener(OnBackClicked);
            }

            UnbindWeaponSlotButtons();

            if (viewModel != null)
            {
                viewModel.SessionCompleted -= HandleSessionCompleted;
                viewModel.SessionReady -= HandleSessionReady;
            }

            lastBoundPlayerHud = null;
            viewModel?.SyncSessionPlayer(null);
            if (playerHealthHudView != null)
            {
                playerHealthHudView.Bind((IViewController)null);
            }
        }

        private void Update()
        {
            TrySyncPlayerViewModel();
            viewModel?.Tick(Time.deltaTime);
        }

        private void OnBackClicked()
        {
            viewModel?.ExitToMenu();
        }

        private void HandleSessionCompleted(GameEndOutcome outcome)
        {
            if (endStatePopupRoot != null)
            {
                endStatePopupRoot.SetActive(true);
            }

            if (endStateLabel == null)
            {
                return;
            }

            endStateLabel.text = GetEndStateLabelText(outcome);
        }

        private static string GetEndStateLabelText(GameEndOutcome outcome)
        {
            if (!string.IsNullOrWhiteSpace(outcome.EndMessage))
            {
                return outcome.EndMessage.Trim();
            }

            return outcome.Reason switch
            {
                GameEndReason.Win => "You Win",
                GameEndReason.Lose => "You Lose",
                _ => "Session ended"
            };
        }

        private void HandleSessionReady()
        {
            SetLoadingVisible(false);
        }

        private void BindWeaponSlotButtons()
        {
            if (weaponSlotButtons == null)
            {
                return;
            }

            weaponSlotClickActions = new UnityAction[weaponSlotButtons.Length];
            for (int i = 0; i < weaponSlotButtons.Length; i++)
            {
                Button button = weaponSlotButtons[i];
                if (button == null)
                {
                    continue;
                }

                int slotIndex = i;
                UnityAction onWeaponSlotClicked = () => viewModel?.TryEquipWeaponSlot(slotIndex);
                weaponSlotClickActions[i] = onWeaponSlotClicked;
                button.onClick.AddListener(onWeaponSlotClicked);
            }
        }

        private void UnbindWeaponSlotButtons()
        {
            if (weaponSlotButtons == null)
            {
                return;
            }

            if (weaponSlotClickActions == null)
            {
                return;
            }

            for (int i = 0; i < weaponSlotButtons.Length; i++)
            {
                Button button = weaponSlotButtons[i];
                UnityAction action = i < weaponSlotClickActions.Length ? weaponSlotClickActions[i] : null;
                if (button == null || action == null)
                {
                    continue;
                }

                button.onClick.RemoveListener(action);
            }

            weaponSlotClickActions = null;
        }

        private void TrySyncPlayerViewModel()
        {
            if (viewModel == null)
            {
                return;
            }

            Player nextPlayer = viewModel.TryGetSessionPlayer();
            viewModel.SyncSessionPlayer(nextPlayer);

            if (playerHealthHudView == null)
            {
                return;
            }

            PlayerViewModel hudVm = viewModel.Player;
            if (!ReferenceEquals(hudVm, lastBoundPlayerHud))
            {
                lastBoundPlayerHud = hudVm;
                playerHealthHudView.Bind(this, hudVm);
            }
        }

        private void SetLoadingVisible(bool isVisible)
        {
            if (loadingViewRoot != null)
            {
                loadingViewRoot.SetActive(isVisible);
            }
        }
    }
}
