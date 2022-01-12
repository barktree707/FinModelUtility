﻿using System.IO;

using schema;

namespace mod.gcn {
  public interface IVector2<T> : IGcnSerializable {
    T X { get; set; }
    T Y { get; set; }

    string? ToString() => $"{this.X} {this.Y}";
  }

  [Schema]
  public partial class Vector2f : IVector2<float> {
    public float X { get; set; }
    public float Y { get; set; }

    public Vector2f() {}
    public Vector2f(float x, float y) {
      this.X = x;
      this.Y = y;
    }

    public void Write(EndianBinaryWriter writer) {
      writer.Write(this.X);
      writer.Write(this.Y);
    }
  }

  public class Vector2i : IVector2<uint> {
    public uint X { get; set; }
    public uint Y { get; set; }

    public Vector2i() { }
    public Vector2i(uint x, uint y) {
      this.X = x;
      this.Y = y;
    }

    public void Read(EndianBinaryReader reader) {
      this.X = reader.ReadUInt32();
      this.Y = reader.ReadUInt32();
    }

    public void Write(EndianBinaryWriter writer) {
      writer.Write(this.X);
      writer.Write(this.Y);
    }
  }
}