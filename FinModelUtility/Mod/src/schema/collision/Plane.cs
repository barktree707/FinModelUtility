﻿using fin.schema.vector;

using schema.binary;

namespace mod.schema.collision {
  [BinarySchema]
  public partial class Plane : IBiSerializable {
    public readonly Vector3f position = new();
    public float diameter;
  }
}
