﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace fin.data.dictionaries {
  public class SortedSetDictionary<TKey, TValue>
      : IFinCollection<(TKey Key, SortedSet<TValue> Value)> {
    private readonly NullFriendlyDictionary<TKey, SortedSet<TValue>> impl_ =
        new();

    public void Clear() => this.impl_.Clear();
    public void ClearSet(TKey key) => this.impl_.Remove(key);

    public int Count => this.impl_.Values.Select(list => list.Count).Sum();

    public bool HasSet(TKey key) => this.impl_.ContainsKey(key);

    public void Add(TKey key, TValue value) {
      SortedSet<TValue> set;
      if (!this.impl_.TryGetValue(key, out set)) {
        this.impl_[key] = set = new SortedSet<TValue>();
      }

      set.Add(value);
    }

    public SortedSet<TValue> this[TKey key] => this.impl_[key];

    public bool TryGetSet(TKey key, out SortedSet<TValue>? set)
      => this.impl_.TryGetValue(key, out set);

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public IEnumerator<(TKey Key, SortedSet<TValue> Value)> GetEnumerator()
      => this.impl_.GetEnumerator();
  }
}