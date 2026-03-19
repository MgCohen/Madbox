using System;
using Madbox.Battle.Services;
using Scaffold.MVVM;
using UnityEngine;
using UnityEngine.UI;
#pragma warning disable SCA0006

namespace Madbox.App.GameView
{
    public class GameView : UIView<GameViewModel>
    {
        [SerializeField] private Component gameStateText;
        [SerializeField] private Button completeButton;

        protected override void OnBind()
        {
            EnsureUi();
            Bind<string, string>(() => viewModel.GameStateText, UpdateStateText);
            Bind<bool, bool>(() => viewModel.IsCompleteVisible, UpdateCompleteButtonVisible);
            BindButton();
            UpdateStateText(viewModel.GameStateText);
            UpdateCompleteButtonVisible(viewModel.IsCompleteVisible);
        }

        protected override void OnUnbind()
        {
            UnbindButton();
        }

        private void Update()
        {
            if (viewModel == null)
            {
                return;
            }

            viewModel.Tick(Time.deltaTime);
        }

        private void EnsureUi()
        {
            if (gameStateText == null) { gameStateText = CreateStateText(); }
            if (completeButton == null) { completeButton = CreateCompleteButton(); }
        }

        private Component CreateStateText()
        {
            Type textType = ResolveTmpType();
            if (textType == null) { return null; }
            RectTransform parent = EnsureContentRoot();
            GameObject labelObject = CreateTextObject("GameStateText", textType, parent);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            ConfigureStateTextRect(rect);
            Component text = labelObject.GetComponent(textType);
            ConfigureStateText(text);
            return text;
        }

        private Button CreateCompleteButton()
        {
            RectTransform parent = EnsureContentRoot();
            GameObject buttonObject = CreateButtonObject(parent);
            buttonObject.name = "CompleteButton";
            Button button = buttonObject.GetComponent<Button>();
            CreateButtonLabel(buttonObject.transform);
            return button;
        }

        private RectTransform EnsureContentRoot()
        {
            RectTransform root = transform as RectTransform;
            return root ?? CreateRootRectTransform();
        }

        private RectTransform CreateRootRectTransform()
        {
            RectTransform root = gameObject.AddComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            return root;
        }

        private GameObject CreateButtonObject(RectTransform parent)
        {
            GameObject buttonObject = new GameObject("CompleteButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            ConfigureButtonRect(rect);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.12f, 0.4f, 0.8f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            return buttonObject;
        }

        private void CreateButtonLabel(Transform parent)
        {
            Type textType = ResolveTmpType();
            if (textType == null) { return; }
            GameObject labelObject = CreateTextObject("Label", textType, parent);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            StretchToParent(rect);
            Component text = labelObject.GetComponent(textType);
            SetTmpFloat(text, "fontSize", 40f);
            SetTmpAlignment(text, 514);
            SetTmpText(text, "Complete");
            SetTmpColor(text, Color.white);
        }

        private GameObject CreateTextObject(string name, Type textType, Transform parent)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), textType);
            textObject.transform.SetParent(parent, false);
            return textObject;
        }

        private void ConfigureStateTextRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 160f);
            rect.sizeDelta = new Vector2(700f, 140f);
        }

        private void ConfigureStateText(Component text)
        {
            SetTmpFloat(text, "fontSize", 56f);
            SetTmpAlignment(text, 514);
            SetTmpColor(text, Color.white);
        }

        private void ConfigureButtonRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -80f);
            rect.sizeDelta = new Vector2(460f, 130f);
        }

        private void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void BindButton()
        {
            if (completeButton == null) { return; }
            completeButton.onClick.RemoveListener(OnCompleteClicked);
            completeButton.onClick.AddListener(OnCompleteClicked);
        }

        private void UnbindButton()
        {
            if (completeButton == null) { return; }
            completeButton.onClick.RemoveListener(OnCompleteClicked);
        }

        private void OnCompleteClicked()
        {
            viewModel?.Complete();
        }

        private void UpdateStateText(string value)
        {
            if (gameStateText == null) { return; }
            SetTmpText(gameStateText, value);
        }

        private void UpdateCompleteButtonVisible(bool visible)
        {
            if (completeButton == null) { return; }
            completeButton.gameObject.SetActive(visible);
        }

        private Type ResolveTmpType()
        {
            return System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        }

        private void SetTmpText(Component text, string value)
        {
            SetTmpValue(text, "text", value);
        }

        private void SetTmpFloat(Component text, string propertyName, float value)
        {
            SetTmpValue(text, propertyName, value);
        }

        private void SetTmpColor(Component text, Color value)
        {
            SetTmpValue(text, "color", value);
        }

        private void SetTmpAlignment(Component text, int enumValue)
        {
            Type enumType = System.Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
            if (enumType == null) { return; }
            object value = Enum.ToObject(enumType, enumValue);
            SetTmpValue(text, "alignment", value);
        }

        private void SetTmpValue(Component text, string propertyName, object value)
        {
            if (text == null) { return; }
            System.Reflection.PropertyInfo property = text.GetType().GetProperty(propertyName);
            if (property == null || !property.CanWrite) { return; }
            property.SetValue(text, value);
        }
    }
}
