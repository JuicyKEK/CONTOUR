using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JuicyDI.Utils
{
    /// <summary>
    /// Сравнение строго по ссылке.
    /// Нужен потому, что UnityEngine.Object переопределяет Equals/== (fake-null),
    /// из-за чего уничтоженные объекты "схлопываются" и ломают HashSet/Dictionary.
    /// </summary>
    public sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

        private ReferenceEqualityComparer()
        {
        }

        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}

