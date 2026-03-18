using UnityEngine;
#pragma warning disable SCA0007
#pragma warning disable SCA0020

namespace Madbox.Gold.Authoring.Definitions
{
    [CreateAssetMenu(menuName = "Madbox/Authoring/Gold Config")]
    public sealed class GoldConfigSO : ScriptableObject
    {
        [SerializeField] private int initialGold;

        public int InitialGold => initialGold;
    }
}
