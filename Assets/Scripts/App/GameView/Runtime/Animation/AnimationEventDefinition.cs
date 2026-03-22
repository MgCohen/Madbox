using UnityEngine;

namespace Madbox.App.GameView.Animation
{
    /// <summary>
    /// Authoring identity for a clip animation event. Set <see cref="StableId"/> on each clip event's int parameter to match.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationEventDefinition", menuName = "Madbox/Game View/Animation Event Definition", order = 0)]
    public sealed class AnimationEventDefinition : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Must match Animation Event int parameter on clips. Must be non-zero.")]
        private int stableId;

        [SerializeField]
        private string displayName;

        [SerializeField]
        [TextArea(2, 6)]
        private string description;

        public int StableId => stableId;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        public string Description => description;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (stableId == 0)
            {
                stableId = UnityEngine.Random.Range(1, int.MaxValue);
            }
        }
#endif
    }
}
