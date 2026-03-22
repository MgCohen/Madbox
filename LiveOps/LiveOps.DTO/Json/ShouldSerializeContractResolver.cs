using System;
using System.Collections;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Madbox.LiveOps.DTO.Json
{
    /// <summary>
    /// Omits default/empty values when serializing to keep payloads small.
    /// </summary>
    public sealed class ShouldSerializeContractResolver : DefaultContractResolver
    {
        public static ShouldSerializeContractResolver Instance { get; } = new ShouldSerializeContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty prop = base.CreateProperty(member, memberSerialization);

            if (prop.PropertyType == typeof(string))
            {
                prop.ShouldSerialize = instance =>
                {
                    string value = prop.ValueProvider.GetValue(instance) as string;
                    return !string.IsNullOrEmpty(value);
                };
                return prop;
            }

            if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
            {
                prop.ShouldSerialize = instance =>
                {
                    object value = prop.ValueProvider.GetValue(instance);
                    if (value == null)
                    {
                        return false;
                    }

                    if (value is IDictionary dict)
                    {
                        return dict.Count > 0;
                    }

                    if (value is ICollection coll)
                    {
                        return coll.Count > 0;
                    }

                    if (value is IEnumerable en)
                    {
                        IEnumerator e = en.GetEnumerator();
                        try
                        {
                            return e.MoveNext();
                        }
                        finally
                        {
                            (e as IDisposable)?.Dispose();
                        }
                    }

                    return true;
                };
                return prop;
            }

            if (IsValueOrNullableValueType(prop.PropertyType))
            {
                prop.ShouldSerialize = instance =>
                {
                    object value = prop.ValueProvider.GetValue(instance);
                    if (value == null)
                    {
                        return false;
                    }

                    (Type underlying, _) = GetUnderlyingType(prop.PropertyType);
                    object def = GetDefaultValue(underlying);
                    return !Equals(value, def);
                };
                return prop;
            }

            return prop;
        }

        private static bool IsValueOrNullableValueType(Type t)
        {
            if (t.IsValueType)
            {
                return true;
            }
            return Nullable.GetUnderlyingType(t) != null;
        }

        private static (Type underlying, bool isNullable) GetUnderlyingType(Type t)
        {
            Type u = Nullable.GetUnderlyingType(t);
            return (u ?? t, u != null);
        }

        private static object GetDefaultValue(Type t)
        {
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }
    }
}
