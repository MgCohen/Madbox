using System;
using System.Collections.Generic;
using Madbox.Level;
using Madbox.Levels;
using Scaffold.MVVM;
using UnityEngine;
using UnityEngine.UI;

namespace Madbox.App.MainMenu
{
    public class MainMenuView : UIView<MainMenuViewModel>
    {
        [SerializeField] private Component goldText;
        [SerializeField] private Button addGoldButton;

        private readonly List<Button> levelButtons = new List<Button>();

        protected override void OnBind()
        {
            EnsureUi();
            BindAddGoldButton();
            Bind<int, int>(() => viewModel.Gold, UpdateGoldText);
            UpdateGoldText(viewModel.Gold);
            BuildLevelButtons();
        }

        private void EnsureUi()
        {
            if (goldText == null)
            {
                goldText = CreateGoldText();
            }

            if (addGoldButton == null)
            {
                addGoldButton = CreateAddGoldButton();
            }
        }

        private void BindAddGoldButton()
        {
            addGoldButton?.onClick.AddListener(OnAddGoldClicked);
        }

        private void UpdateGoldText(int value)
        {
            SetTmpValue(goldText, "text", $"Gold: {value}");
        }

        private void BuildLevelButtons()
        {
            ClearLevelButtons();
            IReadOnlyList<AvailableLevel> levels = viewModel.AvailableLevels;
            if (levels == null || levels.Count == 0)
            {
                return;
            }

            float y = -260f;
            const float step = -160f;
            for (int i = 0; i < levels.Count; i++)
            {
                AvailableLevel entry = levels[i];
                if (entry?.Definition == null)
                {
                    continue;
                }

                LevelDefinition definition = entry.Definition;
                string label = entry.MenuButtonLabel;
                Button button = CreateActionButton($"LevelButton_{i}", label, new Vector2(0f, y));
                y += step;
                AvailableLevel captured = entry;
                button.onClick.AddListener(() => OnLevelButtonClicked(captured));
                levelButtons.Add(button);
            }
        }

        private void OnLevelButtonClicked(AvailableLevel entry)
        {
            if (entry?.Definition == null)
            {
                return;
            }
            Debug.Log(entry.Definition.name);
        }

        private void ClearLevelButtons()
        {
            for (int i = 0; i < levelButtons.Count; i++)
            {
                Button button = levelButtons[i];
                if (button == null)
                {
                    continue;
                }
                button.onClick.RemoveAllListeners();
                Destroy(button.gameObject);
            }
            levelButtons.Clear();
        }

        private Component CreateGoldText()
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

            GameObject textObject = new GameObject("GoldText", typeof(RectTransform), tmpType);
            textObject.transform.SetParent(root, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 180f);
            rect.sizeDelta = new Vector2(620f, 140f);

            Component text = textObject.GetComponent(tmpType);
            SetTmpValue(text, "fontSize", 64f);
            SetTmpAlignment(text, 514);
            SetTmpValue(text, "color", Color.white);
            return text;
        }

        private Button CreateAddGoldButton()
        {
            Vector2 addGoldPosition = new Vector2(0f, -80f);
            return CreateActionButton("AddGoldButton", "Add Gold", addGoldPosition);
        }

        private Button CreateActionButton(string name, string label, Vector2 position)
        {
            Type tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            RectTransform root = transform as RectTransform;
            if (root == null)
            {
                root = gameObject.AddComponent<RectTransform>();
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
            }

            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(root, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = new Vector2(420f, 140f);

            Button button = buttonObject.GetComponent<Button>();
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.14f, 0.57f, 0.25f, 1f);
            button.targetGraphic = image;

            TryCreateButtonLabel(buttonObject, label, tmpType);
            return button;
        }

        private void TryCreateButtonLabel(GameObject buttonObject, string label, Type tmpType)
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
            SetTmpValue(text, "fontSize", 46f);
            SetTmpAlignment(text, 514);
            SetTmpValue(text, "text", label);
            SetTmpValue(text, "color", Color.white);
        }

        protected override void OnUnbind()
        {
            ClearLevelButtons();
            addGoldButton?.onClick.RemoveListener(OnAddGoldClicked);
        }

        public void OnAddGoldClicked()
        {
            viewModel?.AddOneGold();
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
