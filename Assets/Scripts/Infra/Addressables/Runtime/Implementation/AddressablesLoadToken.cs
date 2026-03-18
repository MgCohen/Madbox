using System;
using Madbox.Addressables.Contracts;

namespace Madbox.Addressables
{
    internal readonly struct AddressablesLoadToken : IEquatable<AddressablesLoadToken>
    {
        public AddressablesLoadToken(Type assetType, AssetKey key)
        {
            if (assetType == null) { throw new ArgumentNullException(nameof(assetType)); }
            if (string.IsNullOrWhiteSpace(key.Value)) { throw new ArgumentException("Key value cannot be empty.", nameof(key)); }
            AssetType = assetType;
            Key = key;
            Id = $"{AssetType.FullName}|{Key.Value}";
        }

        public Type AssetType { get; }
        public AssetKey Key { get; }
        public string Id { get; }

        public override bool Equals(object obj)
        {
            return obj is AddressablesLoadToken other && Equals(other);
        }

        public bool Equals(AddressablesLoadToken other)
        {
            if (AssetType == null || other.AssetType == null) { return false; }
            if (string.IsNullOrWhiteSpace(Key.Value) || string.IsNullOrWhiteSpace(other.Key.Value)) { return false; }
            return AssetType == other.AssetType && Key.Equals(other.Key);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((AssetType != null ? AssetType.GetHashCode() : 0) * 397) ^ Key.GetHashCode();
            }
        }
    }
}
