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
    public class GenericConverter<TSource, TTarget> : Converter<TSource, TTarget>
    {
        public GenericConverter(Func<TSource, TTarget> converter)
        {
            if (converter is null) { throw new ArgumentNullException(nameof(converter)); }
            this.converter = converter;
        }

        private Func<TSource, TTarget> converter;

        public override bool CanConvert(TSource source)
        {
            return converter != null;
        }

        public override TTarget Convert(TSource source)
        {
            if (converter != null)
            {
                return converter(source);
            }
            return default;
        }
    }
}





