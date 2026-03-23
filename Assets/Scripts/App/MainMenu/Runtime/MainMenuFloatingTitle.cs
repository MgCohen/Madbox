using DG.Tweening;
using UnityEngine;

namespace Madbox.App.MainMenu
{
    [DisallowMultipleComponent]
    public sealed class MainMenuFloatingTitle : MonoBehaviour
    {
        [SerializeField] private RectTransform floatRoot;
        [SerializeField] private float hoverDistance = 8f;
        [SerializeField] private float hoverDuration = 1.8f;

        private Vector2 baseAnchoredPosition;
        private Tween hoverTween;

        private void OnEnable()
        {
            StartHover();
        }

        private void OnDisable()
        {
            StopHover();
        }

        private void StartHover()
        {
            StopHover();

            RectTransform target = floatRoot != null ? floatRoot : transform as RectTransform;
            if (target == null || hoverDistance <= 0f || hoverDuration <= 0f)
            {
                return;
            }

            baseAnchoredPosition = target.anchoredPosition;
            hoverTween = DOTween
                .To(
                    () => 0f,
                    value =>
                    {
                        float offset = Mathf.Sin(value * Mathf.PI * 2f) * hoverDistance;
                        target.anchoredPosition = baseAnchoredPosition + new Vector2(0f, offset);
                    },
                    1f,
                    hoverDuration)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);
        }

        private void StopHover()
        {
            hoverTween?.Kill();
            hoverTween = null;

            RectTransform target = floatRoot != null ? floatRoot : transform as RectTransform;
            if (target != null)
            {
                target.anchoredPosition = baseAnchoredPosition;
            }
        }
    }
}
