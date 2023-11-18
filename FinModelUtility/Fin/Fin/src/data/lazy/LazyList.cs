﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace fin.data.lazy {
  /// <summary>
  ///   List implementation that lazily populates its entries when accessed.
  /// </summary>
  public class LazyList<T> : ILazyArray<T> {
    private readonly List<T> impl_ = new();
    private readonly List<bool> populated_ = new();
    private readonly Func<int, T> handler_;

    public LazyList(Func<int, T> handler) {
      this.handler_ = handler;
    }

    public LazyList(Func<LazyList<T>, int, T> handler) {
      this.handler_ = (int key) => handler(this, key);
    }

    public int Count => this.impl_.Count;

    public void Clear() {
      this.impl_.Clear();
      this.populated_.Clear();
    }

    public bool ContainsKey(int key) => this.populated_[key];


    public T this[int key] {
      get {
        if (this.Count > key && this.populated_[key]) {
          return this.impl_[key];
        }

        while (this.Count <= key) {
          this.impl_.Add(default!);
          this.populated_.Add(false);
        }

        this.populated_[key] = true;
        return this.impl_[key] = this.handler_(key);
      }
      set {
        while (this.Count <= key) {
          this.impl_.Add(default!);
          this.populated_.Add(false);
        }

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