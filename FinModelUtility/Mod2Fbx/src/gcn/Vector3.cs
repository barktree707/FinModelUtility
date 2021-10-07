﻿using System.IO;

namespace mod.gcn {
  public interface IVector3<T> : IGcnSerializable {
    T X { get; set; }
    T Y { get; set; }
    T Z { get; set; }

    void Set(T x, T y, T z);
    void Reset();

    string? ToString() => $"{this.X} {this.Y} {this.Z}";
  }

  public class Vector3f : IVector3<float> {
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3f() {}
    public Vector3f(float x, float y, float z) => this.Set(x, y, z);

    public void Set(float x, float y, float z) {
      this.X = x;
      this.Y = y;
      this.Z = z;
    }

    public void Reset() => this.Set(0, 0, 0);

    public void Read(EndianBinaryReader reader) {
      this.X = reader.ReadSingle();
      this.Y = reader.ReadSingle();
      this.Z = reader.ReadSingle();
    }

    public void Write(EndianBinaryWriter writer) {
      writer.Write(this.X);
      writer.Write(this.Y);
      writer.Write(this.Z);
    }
  }

  public class Vector3i : IVector3<uint> {
    public uint X { get; set; }
    public uint Y { get; set; }
    public uint Z { get; set; }

    public Vector3i() { }
    public Vector3i(uint x, uint y, uint z) => this.Set(x, y, z);

    public void Set(uint x, uint y, uint z) {
      this.X = x;
      this.Y = y;
      this.Z = z;
    }

    public void Reset() => this.Set(0, 0, 0);

    public void Read(EndianBinaryReader reader) {
      this.X = reader.ReadUInt32();
      this.Y = reader.ReadUInt32();
      this.Z = reader.ReadUInt32();
    }

    public void Write(EndianBinaryWriter writer) {
      writer.Write(this.X);
      writer.Write(this.Y);
      writer.Write(this.Z);
    }
  }
}