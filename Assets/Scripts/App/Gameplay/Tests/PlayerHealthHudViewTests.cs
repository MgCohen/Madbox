using System.Collections.Generic;
using System.Reflection;
using Madbox.Entities;
using Madbox.Players;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Madbox.App.Gameplay.Tests
{
    public sealed class PlayerHealthHudViewTests
    {

        private static Damageable CreateConfiguredPlayerRoot(Transform parent, out EntityAttribute maxHpAsset)
        {
            GameObject playerRoot = new GameObject("PlayerRoot");
            playerRoot.transform.SetParent(parent, false);

            maxHpAsset = ScriptableObject.CreateInstance<EntityAttribute>();
            maxHpAsset.name = "MaxHp";

            playerRoot.AddComponent<Player>();
            Entity entity = playerRoot.AddComponent<Entity>();
            Damageable damageable = playerRoot.AddComponent<Damageable>();

            EntityAttributeEntry attributeEntry = new EntityAttributeEntry();
            SetPrivateField(attributeEntry, "attribute", maxHpAsset);
            SetPrivateField(attributeEntry, "baseValue", 15f);
            SetPrivateField(entity, "attributeEntries", new List<EntityAttributeEntry> { attributeEntry });

            SetPrivateField(damageable, "entity", entity);
            SetPrivateField(damageable, "maxHpAttribute", maxHpAsset);
            SetPrivateField(damageable, "currentHp", 10f);
            SetPrivateField(damageable, "resetHealthInAwake", false);
            return damageable;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(target, value);
        }

        private sealed class HudFixture : System.IDisposable
        {
            public HudFixture()
            {
                Root = new GameObject("HudFixtureRoot");
                GameObject hudGo = new GameObject("PlayerHealthHudView", typeof(RectTransform));
                hudGo.transform.SetParent(Root.transform, false);
                HealthLabel = hudGo.AddComponent<TextMeshProUGUI>();
                Hud = hudGo.AddComponent<PlayerHealthHudView>();
                SetPrivateField(Hud, "healthLabel", HealthLabel);
            }

            public GameObject Root { get; }

            public PlayerHealthHudView Hud { get; }

            public TextMeshProUGUI HealthLabel { get; }

            public void Dispose()
            {
                if (Root != null)
                {
                    Object.DestroyImmediate(Root);
                }
            }
        }
    }
}
