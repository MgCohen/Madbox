using System.Reflection;
using NUnit.Framework;

namespace Madbox.Ugs.Tests
{
    public sealed class UgsTypeTests
    {
        [Test]
        public void Ugs_DeclaresEnsureInitializedAsync()
        {
            MethodInfo method = typeof(Ugs).GetMethod(
                nameof(Ugs.EnsureInitializedAsync),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
        }
    }
}
