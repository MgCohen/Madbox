using System.Collections.Generic;
using Madbox.Players;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.App.GameView.Tests
{
    public sealed class WeaponVisualControllerTests
    {
        [Test]
        public void SetSelectedWeaponIndex_OnlyOneWeaponActive()
        {
            GameObject root = new GameObject("root");
            WeaponVisualController visual = root.AddComponent<WeaponVisualController>();
            GameObject w0 = new GameObject("w0");
            GameObject w1 = new GameObject("w1");
            GameObject w2 = new GameObject("w2");
            visual.SetWeaponInstances(new List<GameObject> { w0, w1, w2 });

            visual.SetSelectedWeaponIndex(1);

            Assert.IsFalse(w0.activeSelf);
            Assert.IsTrue(w1.activeSelf);
            Assert.IsFalse(w2.activeSelf);
            Assert.AreEqual(1, visual.SelectedWeaponIndex);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(w0);
            Object.DestroyImmediate(w1);
            Object.DestroyImmediate(w2);
        }

        [Test]
        public void SetSelectedWeaponIndex_BeforeSetWeaponInstances_Throws()
        {
            GameObject root = new GameObject("root");
            WeaponVisualController visual = root.AddComponent<WeaponVisualController>();
            Assert.Throws<System.InvalidOperationException>(() => visual.SetSelectedWeaponIndex(0));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void SetSelectedWeaponIndex_RaisesSelectedWeaponChanged()
        {
            GameObject root = new GameObject("root");
            WeaponVisualController visual = root.AddComponent<WeaponVisualController>();
            GameObject w0 = new GameObject("w0");
            GameObject w1 = new GameObject("w1");
            visual.SetWeaponInstances(new List<GameObject> { w0, w1 });
            int previous = -99;
            int current = -99;
            visual.SelectedWeaponChanged += (p, c) =>
            {
                previous = p;
                current = c;
            };

            visual.SetSelectedWeaponIndex(1);

            Assert.AreEqual(0, previous);
            Assert.AreEqual(1, current);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(w0);
            Object.DestroyImmediate(w1);
        }

    }
}
