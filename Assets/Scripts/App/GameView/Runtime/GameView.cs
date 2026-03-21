using System;
using Madbox.Battle.Services;
using Scaffold.MVVM;
using UnityEngine;
using UnityEngine.UI;

namespace Madbox.App.GameView
{
    public class GameView : UIView<GameViewModel>
    {
        [SerializeField] private Component gameStateText;
        [SerializeField] private Button completeButton;

        protected override void OnBind()
        {
            EnsureUi();
            BindButton();
            Bind<string, string>(() => viewModel.GameStateText, UpdateStateText);
            Bind<bool, bool>(() => viewModel.IsCompleteVisible, UpdateCompleteButtonVisible);
            UpdateStateText(viewModel.GameStateText);
            UpdateCompleteButtonVisible(viewModel.IsCompleteVisible);
        }

        private void EnsureUi()
        {
            if (gameStateText == null)
            {
                gameStateText = CreateStateText();
            }

            if (completeButton == null)
            {
                completeButton = CreateCompleteButton();
            }
        }

        private void BindButton()
        {
            completeButton?.onClick.AddListener(OnCompleteClicked);
        }

        private void UpdateStateText(string value)
        {
            SetTmpValue(gameStateText, "text", value);
        }

        private void UpdateCompleteButtonVisible(bool visible)
        {
            if (completeButton == null)
            {
                return;
            }

            completeButton.gameObject.SetActive(visible);
        }

        private Component CreateStateText()
        {
            Type tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType == null)
            {
                return null;
            }

            RectTransform root = transform as RectTransform;
            if (root == null)
            {
                root = gameObject.AddComponent<RectTransform>();
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
            }

            GameObject textObject = new GameObject("GameStateText", typeof(RectTransform), tmpType);
            textObject.transform.SetParent(root, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 160f);
            rect.sizeDelta = new Vector2(700f, 140f);

            Component text = textObject.GetComponent(tmpType);
            SetTmpValue(text, "fontSize", 56f);
            SetTmpAlignment(text, 514);
            SetTmpValue(text, "color", Color.white);
            return text;
        }

        private Button CreateCompleteButton()
        {
            Type tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            RectTransform root = transform as RectTransform;
            if (root == null)
            {
                root = gameObject.AddComponent<RectTransform>();
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
            }

            GameObject buttonObject = new GameObject("CompleteButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(root, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -80f);
            rect.sizeDelta = new Vector2(460f, 130f);

            Button button = buttonObject.GetComponent<Button>();
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.4f, 0.8f, 1f);
            button.targetGraphic = image;

            TryCreateCompleteLabel(buttonObject, tmpType);
            return button;
        }

        private void TryCreateCompleteLabel(GameObject buttonObject, Type tmpType)
        {
            if (tmpType == null)
            {
                return;
            }

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), tmpType);
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Component text = labelObject.GetComponent(tmpType);
            SetTmpValue(text, "fontSize", 40f);
            SetTmpAlignment(text, 514);
            SetTmpValue(text, "text", "Complete");
            SetTmpValue(text, "color", Color.white);
        }

        protected override void OnUnbind()
        {
            UnbindButton();
        }

        private void UnbindButton()
        {
            completeButton?.onClick.RemoveListener(OnCompleteClicked);
        }

        public void OnCompleteClicked()
        {
            viewModel?.Complete();
        }

        public void Update()
        {
            if (viewModel == null)
            {
                return;
            }

            viewModel.Tick(Time.deltaTime);
        }

        private void SetTmpAlignment(Component text, int enumValue)
        {
            Type enumType = System.Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
            if (enumType == null)
            {
                return;
            }

            object alignmentValue = Enum.ToObject(enumType, enumValue);
            SetTmpValue(text, "alignment", alignmentValue);
        }

        private void SetTmpValue(Component text, string propertyName, object value)
        {
            if (text == null)
            {
                return;
            }

            var property = text.GetType().GetProperty(propertyName);
            if (property == null || !property.CanWrite)
            {
                return;
            }

            property.SetValue(text, value);
        }
    }
}
