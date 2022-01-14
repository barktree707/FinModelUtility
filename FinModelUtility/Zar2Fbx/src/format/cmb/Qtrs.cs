﻿using schema;

namespace zar.format.cmb {
  [Schema]
  public partial class Qtrs : IDeserializable {
    public readonly string magic = "qtrs";
    public uint chunkSize { get; private set; }

    [ArrayLengthSource(IntType.UINT32)]
    public BoundingBox[] boundingBoxes { get; private set; }
  }
}
