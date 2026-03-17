using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Scaffold.Types
{
    [Serializable]
    public class TypeReference : ISerializationCallbackReceiver
    {
        public TypeReference()
        {

        }

        public TypeReference(Type type)
        {
            if (type is null) { throw new ArgumentNullException(nameof(type)); }
            Set(type);
        }

        public Type Type => type;

        [SerializeField] private Type type;
        [SerializeField] private string serializedType;

        public void Set<T>()
        {
            ValidateSetInvocation();
            Set(typeof(T));
        }

        public void Set(Type type)
        {
            ValidateTypeParameter(type);
            var hasType = type != null;
            if (hasType)
            {
                this.type = type;
                var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
                serializedType = JsonConvert.SerializeObject(type, settings);
            }
        }

        public void OnBeforeSerialize()
        {
            if (type == null)
            {
                return;
            }

            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            serializedType = JsonConvert.SerializeObject(type, settings);
        }

        public void OnAfterDeserialize()
        {
            if (!ValidateDeserializationInput())
            {
                return;
            }
            type = TryDeserializeType(serializedType);
        }

        private void ValidateSetInvocation()
        {
            if (serializedType == null && type == null) return;
        }

        private void ValidateTypeParameter(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
        }

        private bool ValidateDeserializationInput()
        {
            if (string.IsNullOrWhiteSpace(serializedType))
            {
                type = null;
                return false;
            }

            return true;
        }

        private Type TryDeserializeType(string rawSerializedType)
        {
            try { return DeserializeType(rawSerializedType); }
            catch { return HandleTypeDeserializationFailure(rawSerializedType); }
        }

        private Type DeserializeType(string rawSerializedType)
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            return JsonConvert.DeserializeObject<Type>(rawSerializedType, settings);
        }

        private Type HandleTypeDeserializationFailure(string rawSerializedType)
        {
            Debug.LogWarning($"Failed to deserialize type value from:\n{rawSerializedType}");
            return null;
        }

        public static implicit operator TypeReference(Type type) => new(type);
    }
}
