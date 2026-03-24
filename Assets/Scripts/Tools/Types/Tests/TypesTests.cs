using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Scaffold.Types.Tests
{
    public class TypesTests
    {
        [Test]
        public void GetConstructorDependencies_SingleDep_ReturnsParameterTypes()
        {
            DependencyExtractor extractor = new DependencyExtractor();
            Type serviceType = typeof(ServiceWithDep);
            IEnumerable<Type> dependencies = extractor.GetConstructorDependencies(serviceType);
            Type[] types = dependencies.ToArray();
            Type expected = typeof(string);
            Assert.AreEqual(1, types.Length);
            Assert.AreEqual(expected, types[0]);
        }

        [Test]
        public void GetConstructorDependencies_NoDeps_ReturnsEmpty()
        {
            DependencyExtractor extractor = new DependencyExtractor();
            Type serviceType = typeof(ServiceNoDep);
            IEnumerable<Type> dependencies = extractor.GetConstructorDependencies(serviceType);
            Type[] types = dependencies.ToArray();
            Assert.AreEqual(0, types.Length);
        }

        [Test]
        public void GetConstructorDependencies_MultipleDeps_ReturnsAllParameterTypes()
        {
            DependencyExtractor extractor = new DependencyExtractor();
            Type serviceType = typeof(ServiceWithMultipleDeps);
            IEnumerable<Type> dependencies = extractor.GetConstructorDependencies(serviceType);
            Type[] types = dependencies.ToArray();
            Assert.AreEqual(2, types.Length);
        }

        [Test]
        public void GetConstructorDependencies_WithInjectAnnotatedConstructor_PrefersAnnotatedConstructor()
        {
            DependencyExtractor extractor = new DependencyExtractor();
            Type serviceType = typeof(ServiceWithInjectConstructor);
            Type[] dependencies = extractor.GetConstructorDependencies(serviceType).ToArray();
            Assert.AreEqual(1, dependencies.Length);
            Assert.AreEqual(typeof(Guid), dependencies[0]);
        }

        [Test]
        public void GetConstructorDependencies_WithNullType_ThrowsArgumentNullException()
        {
            DependencyExtractor extractor = new DependencyExtractor();
            TestDelegate action = () => extractor.GetConstructorDependencies(null).ToArray();
            Assert.Throws<ArgumentNullException>(action);
        }

        [Test]
        public void GetConstructorDependencies_WithMultipleInjectConstructors_ThrowsInvalidOperationException()
        {
            DependencyExtractor extractor = new DependencyExtractor();
            TestDelegate action = () => extractor.GetConstructorDependencies(typeof(ServiceWithMultipleInjectConstructors)).ToArray();
            Assert.Throws<InvalidOperationException>(action);
        }

        [Test]
        public void TypeReference_ConstructorWithNullType_ThrowsArgumentNullException()
        {
            TestDelegate action = () => new TypeReference(null);
            Assert.Throws<ArgumentNullException>(action);
        }

        private class ServiceWithDep
        {
            public ServiceWithDep(string dep) { }
        }

        private class ServiceNoDep
        {
        }

        private class ServiceWithMultipleDeps
        {
            public ServiceWithMultipleDeps(string dep1, int dep2) { }
        }

        private class ServiceWithInjectConstructor
        {
            public ServiceWithInjectConstructor(string dep1, int dep2) { }

            [Inject] private ServiceWithInjectConstructor(Guid id) { }
        }

        private class ServiceWithMultipleInjectConstructors
        {
            [Inject] private ServiceWithMultipleInjectConstructors(string dep1) { }

            [Inject] private ServiceWithMultipleInjectConstructors(Guid id) { }
        }

        [AttributeUsage(AttributeTargets.Constructor)]
        private sealed class InjectAttribute : Attribute
        {
        }
    }
}


