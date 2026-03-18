using System;

namespace Madbox.Addressables.Contracts
{
    public readonly struct AssetKey : IEquatable<AssetKey>
    {
        public AssetKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("AssetKey cannot be null or whitespace.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public override bool Equals(object obj)
        {
            return obj is AssetKey other && Equals(other);
        }

        public bool Equals(AssetKey other)
        {
            if (string.IsNullOrWhiteSpace(Value) || string.IsNullOrWhiteSpace(other.Value)) { return false; }
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
