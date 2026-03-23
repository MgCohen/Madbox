using System.Reflection;
using Madbox.Levels;
using Scaffold.MVVM;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Madbox.App.MainMenu
{
    public class MainMenuView : UIView<MainMenuViewModel>
    {
        [SerializeField] private TextMeshProUGUI goldLabel;
        [Header("Title")]
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private string titleText = "Fuleiro";
        [SerializeField] private TextMeshProUGUI subtitleLabel;
        [SerializeField] private string subtitleText = "(Its a brazilian pun)";
        [SerializeField] private float hoverDistance = 8f;
        [SerializeField] private float hoverDuration = 1.8f;
        [SerializeField] private Button addGoldButton;
        [SerializeField] private LevelButtonCollectionHandlerBehaviour levelButtonCollectionHandler;

        private RectTransform titleRectTransform;
        private RectTransform subtitleRectTransform;
        private Vector2 titleBaseAnchoredPosition;
        private Vector2 subtitleBaseAnchoredPosition;
        private float hoverElapsedTime;
        private bool isHoverActive;

        protected override void OnBind()
        {
            ApplyTitleTexts();
            StartTitleHover();

            if (goldLabel != null)
            {
                Bind<int, int>(() => viewModel.Wallet.CurrentGold, UpdateGoldText);
            }

            if (addGoldButton != null)
            {
                BindAddGoldButton();
            }

            if (levelButtonCollectionHandler != null)
            {
                levelButtonCollectionHandler.SetLevelSelectHandler(viewModel.PlayLevel);
                BindCollection(() => viewModel.AvailableLevels, levelButtonCollectionHandler);
            }
        }

        private void BindAddGoldButton()
        {
            addGoldButton.onClick.AddListener(OnAddGoldClicked);
        }

        private void UpdateGoldText(int value)
        {
            goldLabel.text = $"Gold: {value}";
        }


        protected override void OnUnbind()
        {
            StopTitleHover();

            if (addGoldButton != null)
            {
                addGoldButton.onClick.RemoveListener(OnAddGoldClicked);
            }
        }

        private void OnDisable()
        {
            StopTitleHover();
        }

        private void Update()
        {
            if (!isHoverActive || hoverDistance <= 0f || hoverDuration <= 0f)
            {
                return;
            }

            hoverElapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = hoverElapsedTime / hoverDuration;
            float titleOffset = Mathf.Sin(normalizedTime * Mathf.PI * 2f) * hoverDistance;
            float subtitleOffset = Mathf.Sin((normalizedTime + 0.5f) * Mathf.PI * 2f) * hoverDistance;

            if (titleRectTransform != null)
            {
                titleRectTransform.anchoredPosition = titleBaseAnchoredPosition + new Vector2(0f, titleOffset);
            }

            if (subtitleRectTransform != null)
            {
                subtitleRectTransform.anchoredPosition = subtitleBaseAnchoredPosition + new Vector2(0f, subtitleOffset);
            }
        }

        public void OnAddGoldClicked()
        {
            viewModel?.AddOneGold();
        }

        private void ApplyTitleTexts()
        {
            if (titleLabel != null)
            {
                titleLabel.text = titleText;
            }

            if (subtitleLabel != null)
            {
                subtitleLabel.text = subtitleText;
            }
        }

        private void StartTitleHover()
        {
            StopTitleHover();

            if (hoverDistance <= 0f || hoverDuration <= 0f)
            {
                return;
            }

            titleRectTransform = titleLabel != null ? titleLabel.rectTransform : null;
            subtitleRectTransform = subtitleLabel != null ? subtitleLabel.rectTransform : null;

            if (titleRectTransform != null)
            {
                titleBaseAnchoredPosition = titleRectTransform.anchoredPosition;
            }

            if (subtitleRectTransform != null)
            {
                subtitleBaseAnchoredPosition = subtitleRectTransform.anchoredPosition;
            }

            hoverElapsedTime = 0f;
            isHoverActive = titleRectTransform != null || subtitleRectTransform != null;
        }

        private void StopTitleHover()
        {
            isHoverActive = false;

            if (titleRectTransform != null)
            {
                titleRectTransform.anchoredPosition = titleBaseAnchoredPosition;
            }

            if (subtitleRectTransform != null)
            {
                subtitleRectTransform.anchoredPosition = subtitleBaseAnchoredPosition;
            }
        }
    }
}
