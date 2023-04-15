﻿using System.Drawing;

using fin.util.hash;

namespace f3dzex2.image {
  public struct ImageParams {
    public ImageParams() { }

    public N64ColorFormat ColorFormat { get; set; } = N64ColorFormat.RGBA;
    public BitsPerTexel BitsPerTexel { get; set; } = BitsPerTexel._16BPT;
    public Color Color { get; set; }

    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public uint SegmentedAddress { get; set; }

    public override int GetHashCode() => FluentHash.Start()
                                                   .With(this.ColorFormat)
                                                   .With(this.BitsPerTexel)
                                                   .With(this.Color.ToArgb())
                                                   .With(this.Width)
                                                   .With(this.Height)
                                                   .With(this.SegmentedAddress);

    public bool IsInvalid => this.Width == 0 || this.Height == 0 ||
                             this.SegmentedAddress == 0;

    public override bool Equals(object? other) {
      if (ReferenceEquals(this, other)) {
        return true;
      }

      if (other is ImageParams otherImageParams) {
        return this.ColorFormat == otherImageParams.ColorFormat &&
               this.BitsPerTexel == otherImageParams.BitsPerTexel &&
               this.Color.ToArgb() == otherImageParams.Color.ToArgb() &&
               this.Width == otherImageParams.Width &&
               this.Height == otherImageParams.Height &&
               this.SegmentedAddress == otherImageParams.SegmentedAddress;
      }

      return false;
    }
  }
}
