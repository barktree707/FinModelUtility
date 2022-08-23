﻿using schema;

namespace cmb.schema.cmb {
  [BinarySchema]
  public partial class TexCoords : IDeserializable {
    [IntegerFormat(SchemaIntegerType.BYTE)]
    public TextureMatrixMode matrixMode { get; private set; }
    public byte referenceCameraIndex { get; private set; }
    [IntegerFormat(SchemaIntegerType.BYTE)]
    public TextureMappingType mappingMethod { get; private set; }
    public byte coordinateIndex { get; private set; }
    public float[] scale { get; } = new float[2];
    public float rotation { get; private set; }
    public float[] translation { get; } = new float[2];
  }
}