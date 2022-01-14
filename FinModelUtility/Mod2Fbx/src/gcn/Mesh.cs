﻿using System;
using System.Collections.Generic;
using System.IO;

using fin.math;

using schema;

namespace mod.gcn {
  // THANKS:
  // https://github.com/KillzXGaming/010-Templates/blob/816cfc57e2ee998b953cf488e4fed25c54e7861a/Pikmin/MOD.bt#L312
  public class DisplayListFlagsByteView {
    public byte b1;
    public byte b2;
    public byte b3;
    public byte cullMode;
  }

  public class DisplayListFlags {
    public DisplayListFlagsByteView byteView = new();

    public uint intView {
      get => BitLogic.ToUint32(this.byteView.b1,
                               this.byteView.b2,
                               this.byteView.b3,
                               this.byteView.cullMode);
      set => (this.byteView.b1,
              this.byteView.b2,
              this.byteView.b3,
              this.byteView.cullMode) = BitLogic.FromUint32(value);
    }
  }

  public class DisplayList : IGcnSerializable {
    public DisplayListFlags flags = new();

    // THANKS: Yoshi2's mod2obj
    public uint cmdCount = 0;
    public readonly List<byte> dlistData = new();

    public void Read(EndianBinaryReader reader) {
      this.flags.intView = reader.ReadUInt32();
      this.cmdCount = reader.ReadUInt32();

      var numDlists = reader.ReadUInt32();
      reader.Align(0x20);
      for (var i = 0; i < numDlists; ++i) {
        this.dlistData.Add(reader.ReadByte());
      }
    }

    public void Write(EndianBinaryWriter writer) {
      writer.Write(this.flags.intView);
      writer.Write(this.cmdCount);

      writer.Write(this.dlistData.Count);
      writer.Align(0x20);
      foreach (var b in this.dlistData) {
        writer.Write(b);
      }
    }
  }

  [Schema]
  public partial class MeshPacket : IGcnSerializable {
    [ArrayLengthSource(IntType.UINT32)]
    public short[] indices;
    [ArrayLengthSource(IntType.UINT32)]
    public DisplayList[] displaylists;

    public void Write(EndianBinaryWriter writer) {
      writer.Write(this.indices.Length);
      foreach (var index in this.indices) {
        writer.Write(index);
      }

      writer.Write(this.displaylists.Length);
      foreach (var dlist in this.displaylists) {
        dlist.Write(writer);
      }
    }
  }

  [Schema]
  public partial class Mesh : IGcnSerializable {
    public uint boneIndex = 0;
    public uint vtxDescriptor = 0;

    [ArrayLengthSource(IntType.UINT32)]
    public MeshPacket[] packets;

    public void Write(EndianBinaryWriter writer) {
      writer.Write(this.boneIndex);
      writer.Write(this.vtxDescriptor);

      writer.Write(this.packets.Length);
      foreach (var packet in this.packets) {
        packet.Write(writer);
      }
    }
  }
}