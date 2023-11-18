﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace fin.data.lazy {
  /// <summary>
  ///   Array implementation that lazily populates its entries when accessed.
  /// </summary>
  public class LazyArray<T> : ILazyArray<T> {
    private readonly T[] impl_;
    private readonly bool[] populated_;
    private readonly Func<int, T> handler_;

    public LazyArray(
        int count,
        Func<int, T> handler) {
      this.impl_ = new T[count];
      this.populated_ = new bool[count];
      this.handler_ = handler;
    }

    public LazyArray(
        int count,
        Func<LazyArray<T>, int, T> handler) {
      this.impl_ = new T[count];
      this.populated_ = new bool[count];
      this.handler_ = (int key) => handler(this, key);
    }

    public int Count => this.impl_.Length;

    public void Clear() {
      for (var i = 0; i < this.Count; ++i) {
        this.populated_[i] = false;
      }
    }

    public bool ContainsKey(int key) => this.populated_[key];


    public T this[int key] {
      get {
        if (this.populated_[key]) {
          return this.impl_[key];
        }

        this.populated_[key] = true;
        return this.impl_[key] = this.handler_(key);
      }
      set {
        this.populated_[key] = true;
        this.impl_[key] = value;
      }
    }

    public IEnumerable<int> Keys
      => Enumerable.Range(0, this.Count).Where(this.ContainsKey);

    public IEnumerable<T> Values
      => this.impl_.Where((value, i) => ContainsKey(i));
  }
}