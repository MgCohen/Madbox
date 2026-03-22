using Madbox.App.GameView.Player;
using NUnit.Framework;
using UnityEngine;

namespace Madbox.App.GameView.Tests
{
    public sealed class PlayerViewDataTests
    {
        [Test]
        public void AttackSpeedStat_WhenSetBelowMinimum_Clamps()
        {
            GameObject go = new GameObject("pd");
            var data = go.AddComponent<PlayerViewData>();
            data.AttackSpeedStat = 0.01f;
            Assert.GreaterOrEqual(data.AttackSpeedStat, 0.05f);
            Object.DestroyImmediate(go);
        }
    }
}
