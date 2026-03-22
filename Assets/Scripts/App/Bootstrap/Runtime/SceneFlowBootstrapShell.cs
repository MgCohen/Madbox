using Madbox.SceneFlow;
using UnityEngine;

namespace Madbox.App.Bootstrap
{
    /// <summary>
    /// Disables Bootstrap camera and AudioListener while additive content scenes own world rendering and audio.
    /// </summary>
    public sealed class SceneFlowBootstrapShell : MonoBehaviour, ISceneFlowBootstrapShell
    {
        [SerializeField] private Camera bootstrapCamera;
        [SerializeField] private AudioListener bootstrapAudioListener;

        public void SetAdditiveContentActive(bool active)
        {
            if (bootstrapCamera != null)
            {
                bootstrapCamera.enabled = !active;
            }

            if (bootstrapAudioListener != null)
            {
                bootstrapAudioListener.enabled = !active;
            }
        }
    }
}
