namespace PommaLabs.KVLite.Utilities
{
    internal static class Option
    {
        public static Option<T> None<T>()
        {
            return new Option<T>();
        }

        public static Option<T> Some<T>(T value)
        {
            return new Option<T>(value);
        }
    }

    internal struct Option<T> : System.IEquatable<Option<T>>
    {
        private readonly bool _hasValue;
        private readonly T _value;

        public Option(T value)
        {
            _hasValue = true;
            _value = value;
        }

        public bool HasValue
        {
            get { return _hasValue; }
        }

        public T Value
        {
            get
            {
                Diagnostics.Raise<System.InvalidOperationException>.IfNot(_hasValue);
                return _value;
            }
        }

        public Option<TResult> Select<TResult>(System.Func<T, TResult> getter) where TResult : class
        {
            return _hasValue ? new Option<TResult>(getter(_value)) : Option.None<TResult>();
        }

        public TResult Select<TResult>(System.Func<T, TResult> getter, TResult alternative)
        {
            return _hasValue ? alternative : getter(_value);
        }

        public void Do(System.Action<T> action)
        {
            if (_hasValue)
            {
                action(_value);
            }
        }

        #region Equality Members

        /// <summary>
        ///   Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///   true if the current object is equal to the <paramref name="other"/> parameter;
        ///   otherwise, false.
        /// </returns>
        public bool Equals(Option<T> other)
        {
            return Equals(ref other);
        }

        /// <summary>
        ///   Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///   true if the current object is equal to the <paramref name="other"/> parameter;
        ///   otherwise, false.
        /// </returns>
        public bool Equals(ref Option<T> other)
        {
            return System.Collections.Generic.EqualityComparer<T>.Default.Equals(_value, other._value);
        }

        /// <summary>
        ///   Determines whether the specified <see cref="System.Object"/>, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Option<T> && Equals((Option<T>) obj);
        }

        /// <summary>
        ///   Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        ///   A hash code for this instance, suitable for use in hashing algorithms and data
        ///   structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return System.Collections.Generic.EqualityComparer<T>.Default.GetHashCode(_value);
        }

        /// <summary>
        ///   Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(Option<T> left, Option<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///   Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(Option<T> left, Option<T> right)
        {
            return !left.Equals(right);
        }

        #endregion Equality Members
    }
}