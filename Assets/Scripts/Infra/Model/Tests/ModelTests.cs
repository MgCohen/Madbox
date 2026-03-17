using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;
using System;
using NUnit.Framework;
using Scaffold.MVVM.Binding;
namespace Scaffold.MVVM.Tests
{
    public class ModelTests
    {
        [Test]
        public void Model_InheritsObservableObject()
        {
            System.Type observableObjectType = typeof(ObservableObject);
            System.Type modelType = typeof(global::Scaffold.MVVM.Model);
            bool isAssignable = observableObjectType.IsAssignableFrom(modelType);
            Assert.IsTrue(isAssignable);
        }

        [Test]
        public void ModelType_UsesNestedObservableObjectAttribute()
        {
            bool hasAttribute = typeof(global::Scaffold.MVVM.Model).IsDefined(typeof(global::Scaffold.MVVM.Binding.NestedObservableObjectAttribute), true);
            Assert.IsTrue(hasAttribute);
        }

        [Test]
        public void NestedPropertyAttribute_TargetsFieldsAndProperties()
        {
            AttributeUsageAttribute usage = GetUsage(typeof(global::Scaffold.MVVM.Binding.NestedPropertyAttribute));
            Assert.AreEqual(AttributeTargets.Field | AttributeTargets.Property, usage.ValidOn);
            Assert.IsFalse(usage.AllowMultiple);
            Assert.IsTrue(usage.Inherited);
        }

        [Test]
        public void NestedObservableObjectAttribute_TargetsClasses()
        {
            AttributeUsageAttribute usage = GetUsage(typeof(global::Scaffold.MVVM.Binding.NestedObservableObjectAttribute));
            Assert.AreEqual(AttributeTargets.Class, usage.ValidOn);
            Assert.IsFalse(usage.AllowMultiple);
            Assert.IsTrue(usage.Inherited);
        }

        [Test]
        public void INestedObservableProperties_RegisterNestedProperties_IsInvokable()
        {
            NestedPropertiesProbe probe = new NestedPropertiesProbe();
            INestedObservableProperties contract = probe;
            contract.RegisterNestedProperties();
            Assert.IsTrue(probe.WasRegistered);
        }

        private AttributeUsageAttribute GetUsage(Type attributeType)
        {
            return (AttributeUsageAttribute)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));
        }

        private sealed class NestedPropertiesProbe : INestedObservableProperties
        {
            public bool WasRegistered { get; private set; }

            public void RegisterNestedProperties()
            {
                WasRegistered = true;
            }
        }
    }
}




