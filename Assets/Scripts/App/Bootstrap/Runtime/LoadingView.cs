using UnityEngine;
using UnityEngine.UI;

namespace Madbox.App.Bootstrap
{
    /// <summary>
    /// Standalone transition loading UI. Callers (e.g. a flow that loads Addressable scenes) own when to show or hide it; it is not registered in DI or coupled to SceneFlow.
    /// </summary>
    public sealed class LoadingView : MonoBehaviour
    {
        [SerializeField] private GameObject presenterRoot;
        [SerializeField] private Image progressFill;

        private void Awake()
        {
            if (presenterRoot == null)
            {
                EnsureDefaultUi();
            }

            Hide();
        }

        public bool IsVisible { get; private set; }

        public void Show()
        {
            if (presenterRoot != null)
            {
                presenterRoot.SetActive(true);
            }

            IsVisible = true;
        }

        public void Hide()
        {
            if (presenterRoot != null)
            {
                presenterRoot.SetActive(false);
            }

            IsVisible = false;
        }

        /// <summary>
        /// Sets normalized progress in [0,1] when a progress fill image is present; no-op otherwise.
        /// </summary>
        public void SetProgress(float normalized)
        {
            if (progressFill == null)
            {
                return;
            }

            progressFill.fillAmount = Mathf.Clamp01(normalized);
        }

        private void EnsureDefaultUi()
        {
            GameObject root = new GameObject("LoadingViewRoot");
            root.transform.SetParent(transform, false);
            presenterRoot = root;

            RectTransform rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1500;
            root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.AddComponent<GraphicRaycaster>();

            GameObject dim = new GameObject("Dim");
            dim.transform.SetParent(root.transform, false);
            Image dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0.02f, 0.02f, 0.05f, 0.94f);
            RectTransform dimRt = dim.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;

            GameObject bar = new GameObject("ProgressBar");
            bar.transform.SetParent(root.transform, false);
            RectTransform barRt = bar.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.15f, 0.46f);
            barRt.anchorMax = new Vector2(0.85f, 0.48f);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;

            Image bg = bar.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.18f, 1f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            progressFill = fill.AddComponent<Image>();
            progressFill.color = new Color(0.25f, 0.65f, 0.95f, 1f);
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillAmount = 0f;
            RectTransform fillRt = progressFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
        }
    }
}
