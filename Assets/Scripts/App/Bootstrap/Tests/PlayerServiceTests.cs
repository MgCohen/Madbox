using System;
using Madbox.App.Bootstrap.Player;
using Madbox.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.App.Bootstrap.Tests
{
    public sealed class PlayerServiceTests
    {
        [Test]
        public void Constructor_GetLoadout_ReturnsSameInstance()
        {
            PlayerLoadoutDefinition def = ScriptableObject.CreateInstance<PlayerLoadoutDefinition>();
            var service = new PlayerService(def);
            Assert.AreSame(def, service.Loadout);
            UnityEngine.Object.DestroyImmediate(def);
        }

        [Test]
        public void Constructor_NullLoadout_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new PlayerService(null));
        }
    }
}
