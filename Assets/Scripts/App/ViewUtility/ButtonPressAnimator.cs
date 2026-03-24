using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Madbox.ViewUtility
{
    /// <summary>
    /// Serialized per-layer motion (rect + offsets). Shared timings live on <see cref="ButtonPressAnimator"/>.
    /// </summary>
    [System.Serializable]
    public sealed class ButtonPressLayer
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private float hoverOffsetY = 4f;
        [SerializeField] private float clickOffsetY = 8f;

        public RectTransform Rect => _rect;
        public float HoverOffsetY => hoverOffsetY;
        public float ClickOffsetY => clickOffsetY;
    }

    [RequireComponent(typeof(RectTransform))]
    public class ButtonPressAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private List<ButtonPressLayer> _layers = new List<ButtonPressLayer>();

        [SerializeField] private float hoverDuration = 0.1f;
        [SerializeField] private float pressDuration = 0.05f;
        [SerializeField] private float popDuration = 0.15f;

        private readonly List<LayerRuntime> _runtimeLayers = new List<LayerRuntime>();
        private bool _isHovered;

        private sealed class LayerRuntime
        {
            public RectTransform Rect;
            public Vector2 InitialAnchoredPos;
            public float HoverOffsetY;
            public float ClickOffsetY;
            public Tween PositionTween;
        }

        private void Awake()
        {
            BuildRuntimeLayers();
        }

        private void BuildRuntimeLayers()
        {
            _runtimeLayers.Clear();
            if (_layers != null)
            {
                foreach (var layer in _layers)
                {
                    if (layer == null || layer.Rect == null)
                    {
                        continue;
                    }

                    _runtimeLayers.Add(new LayerRuntime
                    {
                        Rect = layer.Rect,
                        InitialAnchoredPos = layer.Rect.anchoredPosition,
                        HoverOffsetY = layer.HoverOffsetY,
                        ClickOffsetY = layer.ClickOffsetY,
                    });
                }
            }

            if (_runtimeLayers.Count == 0)
            {
                var rt = GetComponent<RectTransform>();
                if (rt != null)
                {
                    _runtimeLayers.Add(new LayerRuntime
                    {
                        Rect = rt,
                        InitialAnchoredPos = rt.anchoredPosition,
                        HoverOffsetY = 4f,
                        ClickOffsetY = 8f,
                    });
                }
            }
        }

        private void OnEnable()
        {
            foreach (var layer in _runtimeLayers)
            {
                layer.Rect.anchoredPosition = layer.InitialAnchoredPos;
            }
        }

        private void OnDisable()
        {
            KillPositionTweens();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            KillPositionTweens();
            foreach (var layer in _runtimeLayers)
            {
                TweenToHoverRest(layer);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            KillPositionTweens();
            foreach (var layer in _runtimeLayers)
            {
                TweenToFullRest(layer);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            KillPositionTweens();
            foreach (var layer in _runtimeLayers)
            {
                TweenToPressDown(layer);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            KillPositionTweens();
            foreach (var layer in _runtimeLayers)
            {
                TweenToRestWithPop(layer);
            }
        }

        private void KillPositionTweens()
        {
            foreach (var layer in _runtimeLayers)
            {
                if (layer.PositionTween != null && layer.PositionTween.IsActive())
                {
                    layer.PositionTween.Kill();
                    layer.PositionTween = null;
                }
            }
        }

        private static float GetRestY(LayerRuntime layer, bool isHovered)
        {
            float offset = isHovered ? layer.HoverOffsetY : 0f;
            return layer.InitialAnchoredPos.y - offset;
        }

        private void TweenToHoverRest(LayerRuntime layer)
        {
            float targetY = layer.InitialAnchoredPos.y - layer.HoverOffsetY;
            layer.PositionTween = layer.Rect.DOAnchorPosY(targetY, hoverDuration).SetEase(Ease.OutQuad);
        }

        private void TweenToFullRest(LayerRuntime layer)
        {
            layer.PositionTween = layer.Rect
                .DOAnchorPosY(layer.InitialAnchoredPos.y, hoverDuration)
                .SetEase(Ease.OutQuad);
        }

        private void TweenToPressDown(LayerRuntime layer)
        {
            float targetY = layer.InitialAnchoredPos.y - layer.ClickOffsetY;
            layer.PositionTween = layer.Rect.DOAnchorPosY(targetY, pressDuration).SetEase(Ease.OutQuad);
        }

        private void TweenToRestWithPop(LayerRuntime layer)
        {
            float targetY = GetRestY(layer, _isHovered);
            layer.PositionTween = layer.Rect.DOAnchorPosY(targetY, popDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => SnapAnchorY(layer, targetY));
        }

        private void SnapAnchorY(LayerRuntime layer, float targetY)
        {
            if (layer.Rect == null)
            {
                return;
            }

            Vector2 p = layer.Rect.anchoredPosition;
            p.y = targetY;
            layer.Rect.anchoredPosition = p;
        }
    }
}
