using UnityEngine;
using Madbox.Levels.Authoring.Definitions;
#pragma warning disable SCA0007
#pragma warning disable SCA0020

namespace Madbox.Levels.Authoring.Components
{
    public sealed class EnemyAuthoringReference : MonoBehaviour
    {
        [SerializeField] private EnemyDefinitionSO definition;

        public EnemyDefinitionSO Definition => definition;
    }
}
