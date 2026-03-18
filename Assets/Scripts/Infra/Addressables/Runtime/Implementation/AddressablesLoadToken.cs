using System;
using Scaffold.Addressables.Contracts;

namespace Scaffold.Addressables
{
    internal readonly struct AddressablesLoadToken : IEquatable<AddressablesLoadToken>
    {
        public AddressablesLoadToken(Type assetType, AssetKey key)
        {
            AssetType = assetType ?? throw new ArgumentNullException(nameof(assetType));
            Key = key;
            Id = $"{AssetType.FullName}|{Key.Value}";
        }

        public Type AssetType { get; }
        public AssetKey Key { get; }
        public string Id { get; }

        public bool Equals(AddressablesLoadToken other)
        {
            return AssetType == other.AssetType && Key.Equals(other.Key);
        }

        public override bool Equals(object obj)
        {
            return obj is AddressablesLoadToken other && Equals(other);
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
