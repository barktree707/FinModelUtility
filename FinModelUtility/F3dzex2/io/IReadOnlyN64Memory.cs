﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

using fin.data;
using fin.decompression;
using fin.util.hex;

using schema.binary.util;


namespace f3dzex2.io {
  public interface IReadOnlyN64Memory {
    Endianness Endianness { get; }
    IEndianBinaryReader OpenAtSegmentedAddress(uint segmentedAddress);

    IEnumerable<IEndianBinaryReader> OpenPossibilitiesAtSegmentedAddress(
        uint segmentedAddress);

    bool TryToOpenPossibilitiesAtSegmentedAddress(
        uint segmentedAddress,
        out IEnumerable<IEndianBinaryReader> possibilities);

    IEndianBinaryReader OpenSegment(uint segmentIndex);

    IEnumerable<IEndianBinaryReader> OpenPossibilitiesForSegment(
        uint segmentIndex);

    bool IsValidSegment(uint segmentIndex);
    bool IsValidSegmentedAddress(uint segmentedAddress);
    bool IsSegmentCompressed(uint segmentIndex);
  }

  public interface IN64Memory : IReadOnlyN64Memory {
    void AddSegment(uint segmentIndex,
                    uint offset,
                    uint length,
                    IDecompressor? decompressor = null);

    byte[] Bytes { get; }
  }

  public class N64Memory : IN64Memory {
    private readonly ListDictionary<uint, Segment> segments_ = new();

    public N64Memory(byte[] data, Endianness endianness) {
      this.Bytes = data;
      this.Endianness = endianness;
    }

    public Endianness Endianness { get; }

    public byte[] Bytes { get; }

    public IEndianBinaryReader OpenAtSegmentedAddress(uint segmentedAddress)
      => this.OpenPossibilitiesAtSegmentedAddress(segmentedAddress).Single();

    public IEnumerable<IEndianBinaryReader> OpenPossibilitiesAtSegmentedAddress(
        uint segmentedAddress) {
      Asserts.True(
          this.TryToOpenPossibilitiesAtSegmentedAddress(
              segmentedAddress,
              out var possibilities),
          $"Expected 0x{segmentedAddress.ToHex()} to be a valid segmented address.");
      return possibilities;
    }

    public bool TryToOpenPossibilitiesAtSegmentedAddress(
        uint segmentedAddress,
        out IEnumerable<IEndianBinaryReader> possibilities) {
      if (!this.TryToGetSegmentsAtSegmentedAddress_(
              segmentedAddress,
              out var offset,
              out var validSegments)) {
        possibilities = default;
        return false;
      }

      possibilities = validSegments.Select(segment => {
        var memoryStream =
            new MemoryStream(this.Bytes,
                             (int) segment.Offset,
                             (int) segment.Length);
        var er = new EndianBinaryReader(memoryStream, this.Endianness);
        er.Position = offset;
        return er;
      });
      return true;
    }

    public IEndianBinaryReader OpenSegment(uint segmentIndex)
      => this.OpenPossibilitiesForSegment(segmentIndex).Single();

    public IEnumerable<IEndianBinaryReader> OpenPossibilitiesForSegment(
        uint segmentIndex)
      => this
         .segments_[segmentIndex]
         .Select(segment => {
           var memoryStream =
               new MemoryStream(this.Bytes,
                                (int) segment.Offset,
                                (int) segment.Length);
           var er = new EndianBinaryReader(memoryStream, this.Endianness);
           return er;
         });


    public bool IsValidSegment(uint segmentIndex)
      => this.segments_.HasList(segmentIndex);

    public bool IsValidSegmentedAddress(uint segmentedAddress) {
      IoUtils.SplitSegmentedAddress(segmentedAddress,
                                    out var segmentIndex,
                                    out var offset);
      if (!this.segments_.TryGetList(segmentIndex, out var segments)) {
        return false;
      }

      var offsetInSegment = offset;
      return segments!.Any(segment => offsetInSegment < segment.Length);
    }

    public bool IsSegmentCompressed(uint segmentIndex)
      => this.segments_[segmentIndex].Single().Decompressor != null;

    public void AddSegment(uint segmentIndex,
                           uint offset,
                           uint length,
                           IDecompressor? decompressor = null)
      => this.segments_.Add(segmentIndex,
                            new Segment {
                                Offset = offset,
                                Length = length,
                                Decompressor = decompressor,
                            });

    private bool TryToGetSegmentsAtSegmentedAddress_(
        uint segmentedAddress,
        out uint offset,
        out IEnumerable<Segment> validSegments) {
      IoUtils.SplitSegmentedAddress(segmentedAddress,
                                    out var segmentIndex,
                                    out offset);
      var offsetInSegment = offset;

      if (!this.segments_.TryGetList(segmentIndex, out var segments)) {
        validSegments = default;
        return false;
      }

      validSegments =
          segments!.Where(segment => offsetInSegment < segment.Length);
      return segments!.Any();
    }

    private class Segment {
      public uint Offset { get; set; }
      public uint Length { get; set; }
      public IDecompressor? Decompressor { get; set; }
    }
  }
}