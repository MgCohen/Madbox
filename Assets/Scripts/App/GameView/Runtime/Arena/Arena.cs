using UnityEngine;
using UnityEngine.SceneManagement;

namespace Madbox.App.GameView.Arena
{
    /// <summary>
    /// Place on a root or manager object in a level (Addressable) scene. After the scene loads, resolve it with
    /// <see cref="TryFindInScene"/> to read spawn positions and optional play area bounds.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Arena : MonoBehaviour
    {
        [SerializeField] private BoxCollider areaBoundsSource;
        [SerializeField] private Transform enemySpawnPoint;
        [SerializeField] private Transform playerSpawnPoint;

        /// <summary>World-space position used for enemy line spawns when no <see cref="enemySpawnPoint"/> is set.</summary>
        public Vector3 EnemySpawnWorldPosition => enemySpawnPoint != null ? enemySpawnPoint.position : transform.position;

        /// <summary>World-space position for the player when no <see cref="playerSpawnPoint"/> is set.</summary>
        public Vector3 PlayerSpawnWorldPosition => playerSpawnPoint != null ? playerSpawnPoint.position : transform.position;

        /// <summary>Play area bounds in world space when a box collider is assigned or present on this object.</summary>
        public bool TryGetWorldBounds(out Bounds bounds)
        {
            BoxCollider source = ResolveBoundsSource();
            if (source == null)
            {
                bounds = default;
                return false;
            }

            bounds = source.bounds;
            return true;
        }

        private void Awake()
        {
            if (areaBoundsSource == null)
            {
                areaBoundsSource = GetComponent<BoxCollider>();
            }
        }

        private BoxCollider ResolveBoundsSource()
        {
            if (areaBoundsSource != null)
            {
                return areaBoundsSource;
            }

            return GetComponent<BoxCollider>();
        }

        /// <summary>
        /// Looks for an <see cref="Arena"/> under the root objects of <paramref name="scene"/> (includes inactive children).
        /// </summary>
        public static bool TryFindInScene(Scene scene, out Arena arena)
        {
            arena = null;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Arena found = roots[i].GetComponentInChildren<Arena>(true);
                if (found != null)
                {
                    arena = found;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the first arena found in any loaded scene, in ascending build index order (not deterministic for multiple matches).
        /// Prefer <see cref="TryFindInScene"/> when the level scene reference is known.
        /// </summary>
        public static bool TryFindInLoadedScenes(out Arena arena)
        {
            Arena[] arenas = Object.FindObjectsByType<Arena>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (arenas == null || arenas.Length == 0)
            {
                arena = null;
                return false;
            }

            arena = arenas[0];
            return true;
        }
    }
}
