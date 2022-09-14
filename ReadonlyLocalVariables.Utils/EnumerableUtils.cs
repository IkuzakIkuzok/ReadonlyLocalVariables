
// (c) 2022 Kazuki KOHZUKI

using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadonlyLocalVariables
{
    public static class EnumerableUtils
    {
        /// <summary>
        /// Excludes the elements of an <see cref="IEnumerable{T}"/> based on a specified type.
        /// </summary>
        /// <typeparam name="T">The type of elements of the sequence.</typeparam>
        /// <typeparam name="TExclude">The type of elements to be exluded.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> whose elements to be excluded.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> that contains from the input sequence that is not <typeparamref name="TExclude"/>.</returns>
        public static IEnumerable<T> NotOfType<T, TExclude>(this IEnumerable<T> source) where TExclude : T
            => source.Where(element => element is not TExclude);

        /// <summary>
        /// Determines whether any element of a sequence is the specifies type.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> whose elements to check type.</param>
        /// <param name="type">The type to compare with.</param>
        /// <returns><c>true</c> if the source sequence is not empty and at least one of its elements is <paramref name="type"/>;
        /// otherwise, <c>false</c>.</returns>
        public static bool Contains<T>(this IEnumerable<T> source, Type type)
            => source.Any(element => element?.GetType() == type);
    } // public static class EnumerableUtils
} // namespace ReadonlyLocalVariables
