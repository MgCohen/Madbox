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
    internal static class BindedPropertyUtility
    {
        public static IBindedProperty<TSource, TTarget> WithConverter<TSource, TTarget>(this IBindedProperty<TSource, TTarget> property, Func<TSource, TTarget> converter)
        {
            GenericConverter<TSource, TTarget> genericConverter = new GenericConverter<TSource, TTarget>(converter);
            return property.WithConverter(genericConverter);
        }
    }
}






