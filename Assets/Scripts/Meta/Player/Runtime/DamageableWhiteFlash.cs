using System.Collections;
using System.Collections.Generic;
using Madbox.Entities;
using UnityEngine;

namespace Madbox.Players
{
    /// <summary>
    /// Brief white tint on character meshes when <see cref="Damageable"/> takes damage (material <c>_BaseColor</c>).
    /// Assign <see cref="damageable"/> in the inspector.
    /// Duration is <see cref="totalFlashDurationSeconds"/> or <see cref="Damageable.DamageDelaySeconds"/> when
    /// <see cref="syncFlashDurationWithDamageDelay"/> is enabled; each blink is split evenly from <see cref="blinkPeriodSeconds"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageableWhiteFlash : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private static readonly int SColorId = Shader.PropertyToID("_SColor");

        private static readonly int HColorId = Shader.PropertyToID("_HColor");

        [SerializeField]
        private Damageable damageable;

        [SerializeField]
        private Transform renderRoot;

        [SerializeField]
        [Tooltip("When true, flash length matches the assigned Damageable Damage Delay (seconds).")]
        private bool syncFlashDurationWithDamageDelay = true;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Total real-time spent blinking when sync is off or Damageable is missing.")]
        private float totalFlashDurationSeconds = 0.5f;

        [SerializeField]
        [Min(0.0001f)]
        [Tooltip("One full blink (on + off). Each half is on / off; repeats until the total duration elapses.")]
        private float blinkPeriodSeconds = 0.1f;

        [SerializeField]
        [Range(0f, 1f)]
        private float whiteBlend = 0.9f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("Target tint when _BaseColor is already white; use >1 for a visible HDR-style flash.")]
        private float baseColorFlashBoost = 2.5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Peak emission multiplier when the shader has _EmissionColor.")]
        private float emissionFlashStrength = 2.5f;

        private readonly List<Renderer> renderers = new List<Renderer>();

        private MaterialPropertyBlock mpb;

        private Coroutine flashRoutine;

        private void Awake()
        {
            mpb = new MaterialPropertyBlock();
            Transform root = renderRoot != null ? renderRoot : transform.root;
            renderers.Clear();
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                {
                    renderers.Add(r);
                }
            }
        }

        private void OnEnable()
        {
            if (damageable != null)
            {
                damageable.Damaged += OnDamaged;
            }
        }

        private void OnDisable()
        {
            if (damageable != null)
            {
                damageable.Damaged -= OnDamaged;
            }

            StopFlash();
        }

        /// <summary>
        /// Total duration used for the next flash (sync or <see cref="totalFlashDurationSeconds"/>).
        /// </summary>
        public float GetEffectiveFlashDurationSeconds()
        {
            if (syncFlashDurationWithDamageDelay && damageable != null)
            {
                return Mathf.Max(0f, damageable.DamageDelaySeconds);
            }

            return Mathf.Max(0f, totalFlashDurationSeconds);
        }

        private void OnDamaged(object sender, DamagedEventArgs _)
        {
            EnsureRenderersPopulated();

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private void EnsureRenderersPopulated()
        {
            if (renderers.Count > 0)
            {
                return;
            }

            Transform root = renderRoot != null ? renderRoot : transform.root;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                {
                    renderers.Add(r);
                }
            }
        }

        private void StopFlash()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            ClearFlash();
        }

        private void ClearFlash()
        {
            foreach (Renderer r in renderers)
            {
                if (r == null)
                {
                    continue;
                }

                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    r.SetPropertyBlock(null, i);
                }
            }
        }

        private IEnumerator FlashRoutine()
        {
            float total = GetEffectiveFlashDurationSeconds();
            float period = Mathf.Max(0.0001f, blinkPeriodSeconds);
            float halfPeriod = period * 0.5f;
            float endTime = Time.realtimeSinceStartup + total;

            while (Time.realtimeSinceStartup < endTime)
            {
                ApplyWhite();
                float remaining = endTime - Time.realtimeSinceStartup;
                float waitOn = Mathf.Min(halfPeriod, remaining);
                if (waitOn > 0f)
                {
                    yield return new WaitForSecondsRealtime(waitOn);
                }

                ClearFlash();
                remaining = endTime - Time.realtimeSinceStartup;
                float waitOff = Mathf.Min(halfPeriod, remaining);
                if (waitOff > 0f)
                {
                    yield return new WaitForSecondsRealtime(waitOff);
                }
            }

            flashRoutine = null;
        }

        private void ApplyWhite()
        {
            Color brightTint = new Color(baseColorFlashBoost, baseColorFlashBoost, baseColorFlashBoost, 1f);
            Color emissionPeak = Color.white * emissionFlashStrength;

            foreach (Renderer r in renderers)
            {
                if (r == null)
                {
                    continue;
                }

                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null)
                    {
                        continue;
                    }

                    mpb.Clear();
                    r.GetPropertyBlock(mpb, i);
                    bool wrote = false;

                    if (m.HasProperty(BaseColorId))
                    {
                        Color baseColor = m.GetColor(BaseColorId);
                        Color target = Color.Lerp(baseColor, brightTint, whiteBlend);
                        mpb.SetColor(BaseColorId, target);
                        wrote = true;
                    }

                    if (m.HasProperty(ColorId))
                    {
                        Color c = m.GetColor(ColorId);
                        mpb.SetColor(ColorId, Color.Lerp(c, brightTint, whiteBlend));
                        wrote = true;
                    }

                    if (m.HasProperty(EmissionColorId))
                    {
                        Color e = m.GetColor(EmissionColorId);
                        mpb.SetColor(EmissionColorId, Color.Lerp(e, emissionPeak, whiteBlend));
                        wrote = true;
                    }

                    if (m.HasProperty(SColorId))
                    {
                        Color sc = m.GetColor(SColorId);
                        mpb.SetColor(SColorId, Color.Lerp(sc, Color.white, whiteBlend));
                        wrote = true;
                    }

                    if (m.HasProperty(HColorId))
                    {
                        Color hc = m.GetColor(HColorId);
                        mpb.SetColor(HColorId, Color.Lerp(hc, Color.white, whiteBlend));
                        wrote = true;
                    }

                    if (wrote)
                    {
                        r.SetPropertyBlock(mpb, i);
                    }
                }
            }
        }
    }
}
