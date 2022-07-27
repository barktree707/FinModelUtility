﻿using System;
using System.IO;

using cmb.schema.cmb;

using fin.util.strings;

using schema;
using schema.attributes.ignore;
using schema.attributes.offset;


namespace cmb.schema.ctxb {
  [Schema]
  public partial class Ctxb : IBiSerializable {
    public CtxbHeader Header { get; } = new();
    public CtxbTexChunk Chunk { get; } = new();

    [Offset(nameof(BaseDataOffset), nameof(ThisDataOffset))]
    [ArrayLengthSource(nameof(ThisDataLength))]
    public byte[] Data { get; private set; }

    [Ignore] private uint BaseDataOffset => (uint) this.Header.DataOffset;

    [Ignore] private uint ThisDataOffset => this.Chunk.Entry.dataOffset;

    [Ignore] private uint ThisDataLength => this.Chunk.Entry.dataLength;
  }

  [Schema]
  public partial class CtxbHeader : IBiSerializable {
    private readonly string magic_ = "ctxb";
    public int ChunkSize { get; private set; }
    private readonly uint texCount_ = 1;
    private readonly uint padding_ = 0;
    public int ChunkOffset { get; private set; }
    public int DataOffset { get; private set; }
  }

  [Schema]
  public partial class CtxbTexChunk : IBiSerializable {
    private readonly string magic_ = "tex" + AsciiUtil.GetChar(0x20);
    public int ChunkSize { get; private set; }
    private readonly uint texCount_ = 1;
    public CtxbTexEntry Entry { get; } = new();
  }

  public class CtxbTexEntry : IBiSerializable {
    public uint dataLength { get; private set; }
    public ushort mimapCount { get; private set; }
    public bool isEtc1 { get; private set; }
    public bool isCubemap { get; private set; }
    public ushort width { get; private set; }
    public ushort height { get; private set; }
    public GlTextureFormat imageFormat { get; private set; }
    public uint dataOffset { get; private set; }
    public string name { get; private set; }

    public void Read(EndianBinaryReader r) {
      this.dataLength = r.ReadUInt32();
      this.mimapCount = r.ReadUInt16();
      this.isEtc1 = r.ReadByte() != 0;
      this.isCubemap = r.ReadByte() != 0;
      this.width = r.ReadUInt16();
      this.height = r.ReadUInt16();
      this.imageFormat = (GlTextureFormat) r.ReadUInt32();
      this.dataOffset = r.ReadUInt32();
      this.name = r.ReadString(16);
    }

    public void Write(EndianBinaryWriter w)
      => throw new NotImplementedException();
  }
}