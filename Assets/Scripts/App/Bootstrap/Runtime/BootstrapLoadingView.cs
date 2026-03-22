using Madbox.Scope.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Madbox.App.Bootstrap
{
    /// <summary>
    /// Full-screen bootstrap loading presentation driven by <see cref="ILayeredScopeProgress"/>.
    /// </summary>
    public sealed class BootstrapLoadingView : MonoBehaviour, ILayeredScopeProgress
    {
        [SerializeField] private Image progressFill;
        [SerializeField] private GameObject loaderRoot;

        private float pendingNormalized;
        private bool pendingDirty;

        private void Awake()
        {
            if (progressFill == null)
            {
                EnsureDefaultUi();
            }
            else if (loaderRoot == null)
            {
                Canvas canvas = progressFill.GetComponentInParent<Canvas>();
                loaderRoot = canvas != null ? canvas.gameObject : progressFill.gameObject;
            }
        }

        private void EnsureDefaultUi()
        {
            GameObject root = new GameObject("BootstrapLoadingRoot");
            root.transform.SetParent(transform, false);
            loaderRoot = root;

            RectTransform rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.AddComponent<GraphicRaycaster>();

            GameObject dim = new GameObject("Dim");
            dim.transform.SetParent(root.transform, false);
            Image dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
            RectTransform dimRt = dim.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;

            GameObject bar = new GameObject("ProgressBar");
            bar.transform.SetParent(root.transform, false);
            RectTransform barRt = bar.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.1f, 0.45f);
            barRt.anchorMax = new Vector2(0.9f, 0.48f);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;

            Image bg = bar.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

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

        void ILayeredScopeProgress.OnLayerPipelineStep(int completedLayerIndex, int totalLayers)
        {
            if (totalLayers <= 0)
            {
                return;
            }

            pendingNormalized = completedLayerIndex / (float)totalLayers;
            pendingDirty = true;
        }

        private void LateUpdate()
        {
            if (!pendingDirty || progressFill == null)
            {
                return;
            }

            pendingDirty = false;
            progressFill.fillAmount = pendingNormalized;
        }

        public void Hide()
        {
            if (loaderRoot != null)
            {
                loaderRoot.SetActive(false);
                return;
            }

            if (progressFill != null)
            {
                Canvas canvas = progressFill.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(false);
                    return;
                }
            }

            gameObject.SetActive(false);
        }
    }
}
