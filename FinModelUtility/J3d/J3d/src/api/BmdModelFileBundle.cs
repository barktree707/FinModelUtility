﻿using System.Collections.Generic;

using fin.io;
using fin.model.io;
using fin.util.enumerables;

namespace j3d.api {
  public class BmdModelFileBundle : IModelFileBundle {
    public required string GameName { get; init; }
    public IReadOnlyTreeFile MainFile => this.BmdFile;

    public IEnumerable<IReadOnlyGenericFile> Files
      => this.BmdFile.Yield()
             .ConcatIfNonnull(this.BcxFiles)
             .ConcatIfNonnull(this.BtiFiles);

    public IReadOnlyTreeFile BmdFile { get; set; }
    public IReadOnlyList<IReadOnlyTreeFile>? BcxFiles { get; set; }
    public IReadOnlyList<IReadOnlyTreeFile>? BtiFiles { get; set; }
    public float FrameRate { get; set; } = 30;
  }
}