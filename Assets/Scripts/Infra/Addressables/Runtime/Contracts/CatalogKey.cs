using System;

namespace Scaffold.Addressables.Contracts
{
    public readonly struct CatalogKey : IEquatable<CatalogKey>
    {
        public CatalogKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("CatalogKey cannot be null or whitespace.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool Equals(CatalogKey other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CatalogKey other && Equals(other);
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
