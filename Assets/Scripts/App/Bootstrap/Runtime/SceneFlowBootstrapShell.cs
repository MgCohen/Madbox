using Madbox.SceneFlow;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Madbox.App.Bootstrap
{
    /// <summary>
    /// Disables Bootstrap world presentation (camera, listener, UI input) while additive content scenes are active.
    /// </summary>
    public sealed class SceneFlowBootstrapShell : MonoBehaviour, ISceneFlowBootstrapShell
    {
        [SerializeField] private Camera bootstrapCamera;
        [SerializeField] private AudioListener bootstrapAudioListener;
        [SerializeField] private EventSystem bootstrapEventSystem;

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

            if (bootstrapEventSystem != null)
            {
                bootstrapEventSystem.enabled = !active;
            }
        }
    }
}
