using NUnit.Framework;
using UnityEngine;

namespace Madbox.Enemies.Tests
{
    public sealed class EnemyWorldHealthBarViewTests
    {
        [Test]
        public void BillboardRotationTowardsCamera_AlignsForwardWithDirectionToCamera()
        {
            Vector3 bar = new Vector3(0f, 1.35f, 0f);
            Vector3 camPos = new Vector3(0f, 5f, -10f);
            Vector3 camUp = Vector3.up;

            Quaternion q = EnemyWorldHealthBarView.BillboardRotationTowardsCamera(bar, camPos, camUp);

            Vector3 toCam = (camPos - bar).normalized;
            Vector3 forward = q * Vector3.forward;
            Assert.That(Vector3.Dot(forward, toCam), Is.GreaterThan(0.999f));
        }

        [Test]
        public void BillboardRotationTowardsCamera_UsesCameraUpForRoll()
        {
            Vector3 bar = Vector3.zero;
            Vector3 camPos = new Vector3(10f, 0f, 0f);
            Vector3 camUp = new Vector3(0f, 1f, 0.2f).normalized;

            Quaternion q = EnemyWorldHealthBarView.BillboardRotationTowardsCamera(bar, camPos, camUp);

            Vector3 up = q * Vector3.up;
            Assert.That(Vector3.Dot(up, camUp), Is.GreaterThan(0.99f));
        }

        [Test]
        public void BillboardRotationTowardsCamera_WhenCoincident_ReturnsIdentity()
        {
            Vector3 p = new Vector3(1f, 2f, 3f);
            Quaternion q = EnemyWorldHealthBarView.BillboardRotationTowardsCamera(p, p, Vector3.up);
            Assert.That(q, Is.EqualTo(Quaternion.identity));
        }
    }
}
