﻿using gx;
using schema;


namespace bmd.schema.bmd.mat3 {
  public enum RenderOrder {
    PRE_ORDER = 1,
    POST_ORDER = 4,
  }

  /// <summary>
  ///   https://github.com/LordNed/WindEditor/wiki/BMD-and-BDL-Model-Format#material-entry
  /// </summary>
  [BinarySchema]
  public partial class MaterialEntry : IBiSerializable {
    public RenderOrder RenderOrder { get; set; }
    public byte CullModeIndex { get; set; }
    public byte ColorChannelControlsCountIndex { get; set; }
    public byte TexGensCountIndex { get; set; }
    public byte TevStagesCountIndex { get; set; }
    public byte ZCompLocIndex { get; set; }
    public byte ZModeIndex { get; set; }
    public byte DitherIndex { get; set; }

    public short[] MaterialColorIndexes { get; } = new short[2];
    public ushort[] ColorChannelControlIndexes { get; } = new ushort[4];
    public ushort[] AmbientColorIndexes { get; } = new ushort[2];
    public ushort[] LightColorIndexes { get; } = new ushort[8];

    public short[] TexGenInfo { get; } = new short[8];

    public ushort[] TexGenInfo2 { get; } = new ushort[8];
    public short[] TexMatrices { get; } = new short[10];
    public ushort[] DttMatrices { get; } = new ushort[20];
    public short[] TextureIndexes { get; } = new short[8];
    public short[] TevKonstColorIndexes { get; } = new short[4];
    public GxKonstColorSel[] KonstColorSel { get; } = new GxKonstColorSel[16];
    public GxKonstAlphaSel[] KonstAlphaSel { get; } = new GxKonstAlphaSel[16];
    public short[] TevOrderInfoIndexes { get; } = new short[16];
    public short[] TevColorIndexes { get; } = new short[4];
    public short[] TevStageInfoIndexes { get; } = new short[16];
    public short[] TevSwapModeInfo { get; } = new short[16];
    public short[] TevSwapModeTable { get; } = new short[4];
    public ushort[] Unknown2 { get; } = new ushort[12];
    public short FogInfoIndex;
    public short AlphaCompareIndex;
    public short BlendModeIndex;
    public short UnknownIndex;
  }
}