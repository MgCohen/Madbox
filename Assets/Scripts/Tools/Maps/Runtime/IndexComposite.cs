using System;

namespace Scaffold.Maps
{
    public readonly struct Index<TPrimary, TSecondary> : IEquatable<Index<TPrimary, TSecondary>>
    {
        public Index(TPrimary primary, TSecondary secondary)
        {
            if (primary is null) { throw new ArgumentNullException(nameof(primary)); }
            if (secondary is null) { throw new ArgumentNullException(nameof(secondary)); }
            this.Primary = primary;
            this.Secondary = secondary;
        }

        public readonly TPrimary Primary;
        public readonly TSecondary Secondary;

        public bool Equals(Index<TPrimary, TSecondary> other)
        {
            GuardComparison(other);
            return Equals(Primary, other.Primary) && Equals(Secondary, other.Secondary);
        }

        public override bool Equals(object obj)
        {
            return obj is Index<TPrimary, TSecondary> other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + (Primary != null ? Primary.GetHashCode() : 0);
            hash = hash * 31 + (Secondary != null ? Secondary.GetHashCode() : 0);
            return hash;
        }

        private void GuardComparison(Index<TPrimary, TSecondary> other)
        {
        }

        public static bool operator ==(Index<TPrimary, TSecondary> left, Index<TPrimary, TSecondary> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Index<TPrimary, TSecondary> left, Index<TPrimary, TSecondary> right)
        {
            return left.Equals(right) == false;
        }
    }
}
