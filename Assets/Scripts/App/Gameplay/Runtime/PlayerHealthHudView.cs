using DG.Tweening;
using Scaffold.MVVM;
using Scaffold.MVVM.Binding;
using TMPro;
using UnityEngine;

namespace Madbox.App.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealthHudView : ViewElement<GameView, PlayerViewModel>
    {
        [SerializeField]
        private TMP_Text healthLabel;

        [Tooltip("Optional override. If unset, scales this GameObject (the whole HUD element/button).")]
        [SerializeField]
        private Transform punchScaleTarget;

        [SerializeField]
        private float punchStrength = 0.18f;

        [SerializeField]
        private float punchDuration = 0.35f;

        [SerializeField]
        private int punchVibrato = 8;

        [SerializeField]
        private float punchElasticity = 0.6f;

        private int? _lastAppliedHealth;
        private Tween _punchTween;

        protected override void OnBind()
        {
            if (viewModel == null)
            {
                return;
            }

            _lastAppliedHealth = null;
            Bind<int, int>(() => viewModel.CurrentHealth, ApplyCurrentHealthToLabel);
        }

        protected override void OnUnbind()
        {
            KillPunchTween();
            ResetPunchScale();
            _lastAppliedHealth = null;
            base.OnUnbind();
        }

        private void OnDisable()
        {
            KillPunchTween();
            ResetPunchScale();
        }

        private void ApplyCurrentHealthToLabel(int currentHealth)
        {
            if (healthLabel == null)
            {
                return;
            }

            healthLabel.text = currentHealth.ToString();

            if (_lastAppliedHealth.HasValue && _lastAppliedHealth.Value != currentHealth)
            {
                PlayHealthPunchScale();
            }

            _lastAppliedHealth = currentHealth;
        }

        private void PlayHealthPunchScale()
        {
            Transform target = punchScaleTarget != null ? punchScaleTarget : transform;
            KillPunchTween();
            Vector3 punch = Vector3.one * punchStrength;
            _punchTween = target.DOPunchScale(punch, punchDuration, punchVibrato, punchElasticity);
        }

        private void KillPunchTween()
        {
            if (_punchTween != null && _punchTween.IsActive())
            {
                _punchTween.Kill();
            }

            _punchTween = null;
        }

        private void ResetPunchScale()
        {
            Transform target = punchScaleTarget != null ? punchScaleTarget : transform;
            target.localScale = Vector3.one;
        }
    }
}
