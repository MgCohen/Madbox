using Madbox.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Madbox.Enemies
{
    /// <summary>
    /// World-space health bar driven by <see cref="Damageable"/> on the same enemy hierarchy.
    /// Assign <see cref="healthBarUiPrefab"/> (or serialized <see cref="healthSlider"/> / <see cref="worldCanvas"/>)
    /// so UI is authored in the editor instead of built at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyWorldHealthBarView : MonoBehaviour
    {
        private const float VerticalOffset = 1.35f;

        private const float BarRootScale = 0.01f;

        [SerializeField]
        [Tooltip("Prefab root must include a world-space Canvas and a UI Slider (fill only) for HP.")]
        private GameObject healthBarUiPrefab;

        [SerializeField]
        private Damageable damageable;

        [SerializeField]
        private Slider healthSlider;

        [SerializeField]
        private Canvas worldCanvas;

        private void Awake()
        {
            if (damageable == null)
            {
                damageable = GetComponentInChildren<Damageable>(true);
            }

            if (healthSlider == null)
            {
                if (worldCanvas != null)
                {
                    healthSlider = worldCanvas.GetComponentInChildren<Slider>(true);
                }

                if (healthSlider == null && healthBarUiPrefab != null)
                {
                    InstantiateHealthBarFromPrefab();
                }
            }

            if (healthSlider == null)
            {
                Debug.LogError(
                    $"{nameof(EnemyWorldHealthBarView)} on {name}: assign {nameof(healthBarUiPrefab)} or wire {nameof(worldCanvas)} with a {nameof(Slider)}, or assign {nameof(healthSlider)}.",
                    this);
            }
            else
            {
                healthSlider.minValue = 0f;
                healthSlider.maxValue = 1f;
                healthSlider.wholeNumbers = false;
                healthSlider.interactable = false;
            }
        }

        private void InstantiateHealthBarFromPrefab()
        {
            GameObject root = Instantiate(healthBarUiPrefab, transform, false);
            root.name = healthBarUiPrefab.name;
            Transform t = root.transform;
            t.localPosition = new Vector3(0f, VerticalOffset, 0f);
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one * BarRootScale;

            worldCanvas = root.GetComponent<Canvas>();
            if (worldCanvas == null)
            {
                worldCanvas = root.GetComponentInChildren<Canvas>(true);
            }

            if (worldCanvas != null)
            {
                worldCanvas.renderMode = RenderMode.WorldSpace;
                if (Camera.main != null)
                {
                    worldCanvas.worldCamera = Camera.main;
                }
            }

            healthSlider = root.GetComponentInChildren<Slider>(true);
            if (healthSlider == null)
            {
                Debug.LogError(
                    $"{nameof(EnemyWorldHealthBarView)}: prefab '{healthBarUiPrefab.name}' must contain a {nameof(Slider)} (e.g. HP fill).",
                    this);
            }
        }

        private void OnEnable()
        {
            if (damageable != null)
            {
                damageable.Damaged += OnHealthChanged;
                damageable.Died += OnHealthChanged;
            }

            RefreshBar();
        }

        private void OnDisable()
        {
            if (damageable != null)
            {
                damageable.Damaged -= OnHealthChanged;
                damageable.Died -= OnHealthChanged;
            }
        }

        private void LateUpdate()
        {
            if (worldCanvas != null && worldCanvas.renderMode == RenderMode.WorldSpace)
            {
                Transform cam = Camera.main != null ? Camera.main.transform : null;
                if (cam != null)
                {
                    worldCanvas.transform.rotation = BillboardRotationTowardsCamera(
                        worldCanvas.transform.position,
                        cam.position,
                        cam.up);
                }
            }
        }

        /// <summary>
        /// Full camera-facing billboard: canvas forward aligns with view direction (bar toward camera), roll follows camera up.
        /// </summary>
        public static Quaternion BillboardRotationTowardsCamera(
            Vector3 barWorldPosition,
            Vector3 cameraPosition,
            Vector3 cameraUp)
        {
            Vector3 toCam = cameraPosition - barWorldPosition;
            if (toCam.sqrMagnitude <= 0.0001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(toCam.normalized, cameraUp);
        }

        private void OnHealthChanged(object _, System.EventArgs __)
        {
            RefreshBar();
        }

        private void RefreshBar()
        {
            if (damageable == null || healthSlider == null)
            {
                return;
            }

            float max = damageable.MaxHp;
            float t = max > 0f ? Mathf.Clamp01(damageable.CurrentHp / max) : 0f;
            healthSlider.SetValueWithoutNotify(t);

            if (worldCanvas != null)
            {
                worldCanvas.enabled = damageable.IsAlive && max > 0f;
            }
        }
    }
}
