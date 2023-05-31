﻿using schema.defaultinterface;

namespace fin.io {
  [IncludeDefaultInterfaceMethods]
  public readonly partial struct FinDirectory : ISystemDirectory {
    public string FullName { get; }

    public FinDirectory(string fullName) {
      this.FullName = fullName;
    }

    public override string ToString() => this.FullName;
  }
}