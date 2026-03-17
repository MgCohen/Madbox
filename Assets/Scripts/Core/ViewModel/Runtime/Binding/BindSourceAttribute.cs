using System.Linq.Expressions;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Collections;
using Scaffold.Maps;
using CommunityToolkit.Mvvm.ComponentModel;
using UnityEngine;
using Scaffold.Navigation.Contracts;
using Scaffold.MVVM.Contracts;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Scaffold.MVVM.Binding
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class BindSourceAttribute : Attribute
    {
        public BindSourceAttribute(Type bindingType)
        {
            if (bindingType is null) { throw new ArgumentNullException(nameof(bindingType)); }
            BindingType = bindingType;
        }

        public Type BindingType { get; }
    }
}





