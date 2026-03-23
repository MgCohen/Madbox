using System.Collections.Generic;
using System.Reflection;
using Madbox.App.GameView.Player;
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
            SetSocketsViaReflection(visual, 3);
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
            SetSocketsViaReflection(visual, 3);
            Assert.Throws<System.InvalidOperationException>(() => visual.SetSelectedWeaponIndex(0));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void SetSelectedWeaponIndex_RaisesSelectedWeaponChanged()
        {
            GameObject root = new GameObject("root");
            WeaponVisualController visual = root.AddComponent<WeaponVisualController>();
            SetSocketsViaReflection(visual, 2);
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

        private static void SetSocketsViaReflection(WeaponVisualController visual, int count)
        {
            var sockets = new List<Transform>();
            for (int i = 0; i < count; i++)
            {
                Transform s = new GameObject("s" + i).transform;
                s.SetParent(visual.transform, false);
                sockets.Add(s);
            }

            typeof(WeaponVisualController).GetField("weaponSockets", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(visual, sockets);
        }

    }
}
