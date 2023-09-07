﻿using System;
using System.Collections.Generic;
using System.Linq;

using fin.util.enumerables;

namespace fin.util.linq {
  public static class LinqExtensions {
    public static IEnumerable<TTo> CastTo<TFrom, TTo>(
        this IEnumerable<TFrom> enumerable) where TFrom : TTo
      => enumerable.Select(value => (TTo) value);

    public static IEnumerable<TTo> WhereIs<TFrom, TTo>(
        this IEnumerable<TFrom> enumerable) where TTo : TFrom
      => enumerable.Where(value => value is TTo)
                   .Select(value => (TTo) value!);

    public delegate bool SelectWhereHandler<in TFrom, TTo>(
        TFrom from,
        out TTo to);

    public static IEnumerable<TTo> SelectWhere<TFrom, TTo>(
        this IEnumerable<TFrom> enumerable,
        SelectWhereHandler<TFrom, TTo> selectWhereHandler) where TTo : TFrom
      => enumerable
         .Select(fromValue => {
           var returnValue =
               selectWhereHandler(fromValue, out var toValue);
           return (returnValue, toValue);
         })
         .Where(tuple => tuple.returnValue)
         .Select(tuple => tuple.toValue);

    public static IEnumerable<TTo> SelectNonnull<TFrom, TTo>(
        this IEnumerable<TFrom> enumerable,
        Func<TFrom, TTo?> selectWhereHandler)
        where TTo : notnull
      => enumerable
         .Select(selectWhereHandler)
         .Nonnull();

    public static bool TryGetFirst<T>(this IEnumerable<T> enumerable,
                                      out T first) {
      try {
        first = enumerable.First();
        return true;
      } catch { }

      first = default;
      return false;
    }

    public static bool TryGetFirst<T>(this IEnumerable<T> enumerable,
                                      Func<T, bool> predicate,
                                      out T first) {
      try {
        first = enumerable.First(predicate);
        return true;
      } catch { }

      first = default;
      return false;
    }

    public static T First<T>(this IEnumerable<T> enumerable,
                             string errorMessage) {
      try {
        var first = enumerable.First();
        return first;
      } catch {
        throw new Exception(errorMessage);
      }
    }

    public static T First<T>(this IEnumerable<T> enumerable,
                             Func<T, bool> predicate,
                             string errorMessage) {
      try {
        var first = enumerable.First(predicate);
        return first;
      } catch {
        throw new Exception(errorMessage);
      }
    }
  }
}