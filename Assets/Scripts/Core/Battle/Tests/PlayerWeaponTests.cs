using Madbox.Levels;
using NUnit.Framework;

namespace Madbox.Battle.Tests
{
    public class PlayerWeaponTests
    {
        [Test]
        public void Constructor_WithoutWeapon_UsesLongSwordDefault()
        {
            EntityId playerId = new EntityId("player-1");
            Player player = new Player(playerId, 100);

            Assert.AreEqual(WeaponProfiles.LongSword, player.EquippedWeapon);
            Assert.AreEqual("long-sword", player.EquippedWeapon.Id.Value);
        }

        [Test]
        public void Constructor_WithWeapon_UsesProvidedWeapon()
        {
            EntityId playerId = new EntityId("player-1");
            Player player = new Player(playerId, 100, WeaponProfiles.CurvedSword);

            Assert.AreEqual(WeaponProfiles.CurvedSword, player.EquippedWeapon);
            Assert.AreEqual("curved-sword", player.EquippedWeapon.Id.Value);
        }

        [Test]
        public void EquipWeapon_ChangesEquippedWeapon()
        {
            EntityId playerId = new EntityId("player-1");
            Player player = new Player(playerId, 100, WeaponProfiles.CurvedSword);

            player.EquipWeapon(WeaponProfiles.GreatSword);

            Assert.AreEqual(WeaponProfiles.GreatSword, player.EquippedWeapon);
            Assert.AreEqual("great-sword", player.EquippedWeapon.Id.Value);
        }
    }
}


