using UnityEngine;

namespace Madbox.App.GameView.Animation
{

    /// <summary>
    /// Thin animator wrapper: cross-fade play by state name, and named bool/float parameters.
    /// </summary>
    public sealed class AnimationController : MonoBehaviour
    {
        public Animator Animator => animator;
        [SerializeField] private Animator animator;

        [SerializeField]
        private float defaultPlayTransitionDuration = 0.1f;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }

        public void Play(string stateName, float normalizedTransitionDuration = -1f)
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
            {
                return;
            }

            float d = normalizedTransitionDuration >= 0f ? normalizedTransitionDuration : defaultPlayTransitionDuration;
            int stateHash = Animator.StringToHash(stateName);
            animator.CrossFade(stateHash, d, 0);
        }

        public bool GetBool(string parameterName)
        {
            if (animator == null || string.IsNullOrEmpty(parameterName))
            {
                return false;
            }

            return animator.GetBool(parameterName);
        }

        public bool GetBool(AnimationAttribute attribute)
        {
            if (attribute == null)
            {
                return false;
            }

            return GetBool(attribute.ParameterName);
        }

        public void SetBool(string parameterName, bool value)
        {
            if (animator == null || string.IsNullOrEmpty(parameterName))
            {
                return;
            }

            animator.SetBool(parameterName, value);
        }

        public void SetBool(AnimationAttribute attribute, bool value)
        {
            if (attribute == null)
            {
                return;
            }

            SetBool(attribute.ParameterName, value);
        }

        public void SetFloat(string parameterName, float value)
        {
            if (animator == null || string.IsNullOrEmpty(parameterName))
            {
                return;
            }

            animator.SetFloat(parameterName, value);
        }

        public void SetFloat(AnimationAttribute attribute, float value)
        {
            if (attribute == null)
            {
                return;
            }

            SetFloat(attribute.ParameterName, value);
        }
    }
}
