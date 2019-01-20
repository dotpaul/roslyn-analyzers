// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal static class HashUtilities
    {
        internal static int GetHashCodeOrDefault(this object objectOpt) => objectOpt?.GetHashCode() ?? 0;

        internal static int Combine(int newKey, int currentKey)
        {
            return unchecked((currentKey * (int)0xA5555529) + newKey);
        }

        internal static int Combine<T>(ImmutableArray<T> array) => Combine(array, 0);
        internal static int Combine<T>(ImmutableArray<T> array, int currentKey) => Combine(array, array.Length, currentKey);

        public static int Combine<T>(IEnumerable<T> sequence, int length, int currentKey)
        {
            var hashCode = Combine(length, currentKey);
            foreach (var element in sequence)
            {
                hashCode = Combine(element.GetHashCode(), hashCode);
            }

            return hashCode;
        }

        internal static int Combine<T>(ImmutableStack<T> stack) => Combine(stack, 0);
        internal static int Combine<T>(ImmutableStack<T> stack, int currentKey)
        {
            var hashCode = currentKey;

            var stackSize = 0;
            foreach (var element in stack)
            {
                hashCode = Combine(element.GetHashCode(), hashCode);
                stackSize++;
            }

            return Combine(stackSize, hashCode);
        }

        internal static int Combine<T>(ImmutableHashSet<T> set) => Combine(set, 0);
        internal static int Combine<T>(ImmutableHashSet<T> set, int currentKey)
        {
            int result;
            ArrayBuilder<int> builder = ArrayBuilder<int>.GetInstance(set.Count);
            try
            {
                foreach (T element in set)
                {
                    builder.Add(element.GetHashCode());
                }

                builder.Sort();
                result = Combine(builder, builder.Count, currentKey);
            }
            finally
            {
                builder.Free();
            }

            return result;
        }

        internal static int Combine<TKey, TValue>(ImmutableDictionary<TKey, TValue> dictionary) => Combine(dictionary, 0);
        internal static int Combine<TKey, TValue>(ImmutableDictionary<TKey, TValue> dictionary, int currentKey)
        {
            int result;
            ArrayBuilder<int> builder = ArrayBuilder<int>.GetInstance(dictionary.Count);
            try
            {
                foreach (KeyValuePair<TKey, TValue> kvp in dictionary)
                {
                    builder.Add(Combine(kvp.Key.GetHashCode(), kvp.Value.GetHashCodeOrDefault()));
                }

                builder.Sort();
                result = Combine(builder, builder.Count, currentKey);
            }
            finally
            {
                builder.Free();
            }

            return result;
        }
    }
}