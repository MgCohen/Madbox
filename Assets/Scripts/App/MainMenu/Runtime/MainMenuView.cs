using System;
using UnityEngine;
using UnityEngine.UI;
using Scaffold.MVVM;
#pragma warning disable SCA0006

namespace Madbox.App.MainMenu
{
    public class MainMenuView : UIView<MainMenuViewModel>
    {
        [SerializeField] private Component goldText;
        [SerializeField] private Button addGoldButton;
        [SerializeField] private Button startGameButton;

        protected override void OnBind()
        {
            EnsureUi();
            Bind<int, int>(() => viewModel.Gold, UpdateGoldText);
            BindButton();
            BindStartButton();
            UpdateGoldText(viewModel.Gold);
        }

        protected override void OnUnbind()
        {
            UnbindButton();
            UnbindStartButton();
        }

        private void EnsureUi()
        {
            if (goldText == null) { goldText = CreateGoldText(); }
            if (addGoldButton == null) { addGoldButton = CreateAddButton(); }
            if (startGameButton == null) { startGameButton = CreateStartButton(); }
        }

        private void BindButton()
        {
            if (addGoldButton == null) { return; }
            addGoldButton.onClick.RemoveListener(OnAddGoldClicked);
            addGoldButton.onClick.AddListener(OnAddGoldClicked);
        }

        private void UnbindButton()
        {
            if (addGoldButton == null) { return; }
            addGoldButton.onClick.RemoveListener(OnAddGoldClicked);
        }

        private void BindStartButton()
        {
            if (startGameButton == null) { return; }
            startGameButton.onClick.RemoveListener(OnStartGameClicked);
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        private void UnbindStartButton()
        {
            if (startGameButton == null) { return; }
            startGameButton.onClick.RemoveListener(OnStartGameClicked);
        }

        private Component CreateGoldText()
        {
            Type textType = ResolveTmpType();
            if (textType == null) { return null; }
            RectTransform parent = EnsureContentRoot();
            GameObject labelObject = CreateTextObject("GoldText", textType, parent);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            ConfigureGoldTextRect(rect);
            return ConfigureAndGetGoldText(labelObject, textType);
        }

        private Button CreateAddButton()
        {
            RectTransform parent = EnsureContentRoot();
            GameObject buttonObject = CreateButtonObject(parent);
            Button button = buttonObject.GetComponent<Button>();
            CreateButtonLabel(buttonObject.transform);
            return button;
        }

        private Button CreateStartButton()
        {
            RectTransform parent = EnsureContentRoot();
            GameObject buttonObject = CreateButtonObject(parent);
            buttonObject.name = "StartGameButton";
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, -260f);
            Button button = buttonObject.GetComponent<Button>();
            CreateStartButtonLabel(buttonObject.transform);
            return button;
        }

        private GameObject CreateButtonObject(RectTransform parent)
        {
            GameObject buttonObject = new GameObject("AddGoldButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            ConfigureButtonRect(rect);
            Image image = buttonObject.GetComponent<Image>();
            ConfigureButtonGraphic(buttonObject, image);
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
            ConfigureButtonLabelText(text);
        }

        private RectTransform EnsureContentRoot()
        {
            RectTransform root = transform as RectTransform;
            return root ?? CreateRootRectTransform();
        }

        private void ConfigureGoldTextRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 180f);
            rect.sizeDelta = new Vector2(620f, 140f);
        }

        private void ConfigureButtonRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -80f);
            rect.sizeDelta = new Vector2(420f, 140f);
        }

        private void OnAddGoldClicked()
        {
            viewModel?.AddOneGold();
        }

        private void OnStartGameClicked()
        {
            viewModel?.StartGame();
        }

        private void UpdateGoldText(int value)
        {
            if (goldText == null) { return; }
            SetTmpText(goldText, $"Gold: {value}");
        }

        private System.Type ResolveTmpType()
        {
            return System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        }

        private Component ConfigureAndGetGoldText(GameObject labelObject, Type textType)
        {
            Component text = labelObject.GetComponent(textType);
            ConfigureGoldText(text);
            return text;
        }

        private void ConfigureGoldText(Component text)
        {
            SetTmpFloat(text, "fontSize", 64f);
            SetTmpAlignment(text, 514);
            SetTmpColor(text, Color.white);
        }

        private RectTransform CreateRootRectTransform()
        {
            RectTransform root = gameObject.AddComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            return root;
        }

        private void ConfigureButtonLabelText(Component text)
        {
            SetTmpFloat(text, "fontSize", 46f);
            SetTmpAlignment(text, 514);
            SetTmpText(text, "Add Gold");
            SetTmpColor(text, Color.white);
        }

        private void CreateStartButtonLabel(Transform parent)
        {
            Type textType = ResolveTmpType();
            if (textType == null) { return; }
            GameObject labelObject = CreateTextObject("Label", textType, parent);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            StretchToParent(rect);
            Component text = labelObject.GetComponent(textType);
            SetTmpFloat(text, "fontSize", 46f);
            SetTmpAlignment(text, 514);
            SetTmpText(text, "Start Game");
            SetTmpColor(text, Color.white);
        }

        private void ConfigureButtonGraphic(GameObject buttonObject, Image image)
        {
            image.color = new Color(0.14f, 0.57f, 0.25f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
        }

        private GameObject CreateTextObject(string name, Type textType, Transform parent)
        {
            GameObject objectRef = new GameObject(name, typeof(RectTransform), textType);
            objectRef.transform.SetParent(parent, false);
            return objectRef;
        }

        private void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
            System.Type enumType = ResolveTmpAlignmentType();
            if (enumType == null) { return; }
            object value = System.Enum.ToObject(enumType, enumValue);
            SetTmpValue(text, "alignment", value);
        }

        private System.Type ResolveTmpAlignmentType()
        {
            return System.Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
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
