﻿using System.Collections.Generic;
using System.Linq;

using schema.binary.util;


namespace fin.util.enumerables {
  public static class EnumerableExtensions {
    public static int IndexOf<T>(this IEnumerable<T> enumerable,
                                 T value) {
      var index = enumerable.IndexOfOrNegativeOne(value);
      Asserts.True(index > -1);
      return index;
    }

    public static int IndexOfOrNegativeOne<T>(
        this IEnumerable<T> enumerable,
        T value) {
      var index = 0;
      foreach (var item in enumerable) {
        if (value?.Equals(item) ?? (value == null && item == null)) {
          return index;
        }

        index++;
      }

      return -1;
    }

    public static IEnumerable<T> ConcatIfNonnull<T>(
        this IEnumerable<T> enumerable,
        IEnumerable<T>? other)
      => other == null ? enumerable : enumerable.Concat(other);

    public static IEnumerable<T> ConcatIfNonnull<T>(
        this IEnumerable<T> enumerable,
        T? other)
      => other == null ? enumerable : enumerable.Concat(other.Yield());

    public static IEnumerable<T> Yield<T>(this T item) {
      yield return item;
    }

    public static List<T> AsList<T>(this T item)
      => item.Yield().ToList();


    public static IEnumerable<(T, T)> SeparatePairs<T>(
        this IEnumerable<T> enumerable) {
      using var iterator = enumerable.GetEnumerator();
      while (iterator.MoveNext()) {
        var v1 = iterator.Current;

        iterator.MoveNext();
        var v2 = iterator.Current;

        yield return (v1, v2);
      }
    }

    public static IEnumerable<(T, T, T)> SeparateTriplets<T>(
        this IEnumerable<T> enumerable) {
      using var iterator = enumerable.GetEnumerator();
      while (iterator.MoveNext()) {
        var v1 = iterator.Current;

        iterator.MoveNext();
        var v2 = iterator.Current;

        iterator.MoveNext();
        var v3 = iterator.Current;

        yield return (v1, v2, v3);
      }
    }
  }
}