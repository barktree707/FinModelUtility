﻿using fin.data;
using fin.util.asserts;

using gx;

using modl.schema.modl.bw1.node;
using modl.schema.modl.bw1.node.display_list;

using schema;


namespace modl.schema.modl.bw1 {
  public class Bw1Model : IDeserializable {
    public List<NodeBw1> Nodes { get; } = new();
    public ListDictionary<ushort, ushort> CnctParentToChildren { get; } = new();

    public Dictionary<uint, NodeBw1> NodeByWeirdId { get; } = new();

    public void Read(EndianBinaryReader er) {
      var filenameLength = er.ReadUInt32();
      er.Position += filenameLength;

      er.AssertStringEndian("MODL");

      var size = er.ReadUInt32();
      var expectedEnd = er.Position + size;

      var nodeCount = er.ReadUInt16();
      var additionalDataCount = er.ReadByte();

      var padding = er.ReadByte();

      var someCount = er.ReadUInt32();
      var unknown0 = er.ReadSingles(4);

      var additionalData = er.ReadUInt32s(additionalDataCount);

      this.SkipSection_(er, "XMEM");

      // Reads in nodes (bones)
      {
        this.Nodes.Clear();
        this.NodeByWeirdId.Clear();
        for (var i = 0; i < nodeCount; ++i) {
          var node = new NodeBw1(additionalDataCount);
          node.Read(er);
          this.Nodes.Add(node);

          this.NodeByWeirdId[node.WeirdId] = node;
        }
      }

      // Reads in hierarchy, how nodes are "CoNneCTed" or "CoNCaTenated?"?
      {
        er.AssertStringEndian("CNCT");

        var cnctSize = er.ReadUInt32();
        var cnctCount = cnctSize / 4;

        this.CnctParentToChildren.Clear();
        for (var i = 0; i < cnctCount; ++i) {
          var parent = er.ReadUInt16();
          var child = er.ReadUInt16();

          this.CnctParentToChildren.Add(parent, child);
        }
      }

      Asserts.Equal(expectedEnd, er.Position);
    }

    private void SkipSection_(EndianBinaryReader er, string sectionName) {
      er.AssertStringEndian(sectionName);
      var size = er.ReadUInt32();
      var data = er.ReadBytes((int) size);
      ;
    }
  }

  public class NodeBw1 : IDeserializable {
    private int additionalDataCount_;

    public uint WeirdId { get; set; }

    public BwTransform Transform { get; } = new();
    public Bw1BoundingBox BoundingBox { get; } = new();

    public float Scale { get; set; }

    public List<Bw1Material> Materials { get; } = new();

    public NodeBw1(int additionalDataCount) {
      this.additionalDataCount_ = additionalDataCount;
    }

    public void Read(EndianBinaryReader er) {
      er.AssertStringEndian("NODE");

      var nodeSize = er.ReadUInt32();
      var nodeStart = er.Position;
      var expectedNodeEnd = nodeStart + nodeSize;

      var headerStart = er.Position;
      var expectedHeaderEnd = headerStart + 0x38;
      {
        // TODO: What are these used for?
        var someMin = er.ReadUInt16();
        var someMax = er.ReadUInt16();

        this.WeirdId = someMin;

        // TODO: unknown, probably enum values
        var unknowns0 = er.ReadUInt32s(2);

        this.Transform.Read(er);

        // TODO: unknown, also transform??
        // These look very similar to the values defined in the constructor
        var unknowns1 = er.ReadSingles(4);

        ;
      }
      Asserts.Equal(er.Position, expectedHeaderEnd);

      // TODO: additional data
      var additionalData = er.ReadUInt32s(this.additionalDataCount_);
      ;

      this.BoundingBox.Read(er);

      var sectionName = er.ReadStringEndian(4);
      var sectionSize = er.ReadInt32();

      while (sectionName != "MATL") {
        if (sectionName == "VSCL") {
          Asserts.Equal(4, sectionSize);
          this.Scale = er.ReadSingle();
        } else if (sectionName == "RNOD") {
          this.ReadRnod_(er);
        } else {
          throw new NotImplementedException();
        }

        sectionName = er.ReadStringEndian(4);
        sectionSize = er.ReadInt32();
      }

      Asserts.Equal("MATL", sectionName);

      var materialSize = 0x48;
      Asserts.Equal(0, sectionSize % materialSize);

      this.Materials.Clear();
      for (var i = 0; i < sectionSize / materialSize; ++i) {
        this.Materials.Add(er.ReadNew<Bw1Material>());
      }

      var vertexDescriptorValue = (uint) 0;
      while (er.Position < expectedNodeEnd) {
        sectionName = er.ReadStringEndian(4);
        sectionSize = er.ReadInt32();

        var expectedSectionEnd = er.Position + sectionSize;

        switch (sectionName) {
          case "VUV1":
          case "VUV2":
          case "VUV3":
          case "VUV4": {
            // TODO: Need to keep track of section order
            var uvMapIndex = sectionName[3] - '1';
            this.ReadUvMap_(er, uvMapIndex, sectionSize / (2 * 2));
            break;
          }
          case "VPOS": {
            // TODO: Handle this properly
            // Each new VPOS section seems to correspond to a new LOD mesh, but we only need the first one.
            if (Positions.Count > 0) {
              er.Position = expectedNodeEnd;
              goto BreakEarly;
            }

            var vertexPositionSize = 2 * 3;
            Asserts.Equal(0, sectionSize % vertexPositionSize);
            this.ReadPositions_(er, sectionSize / vertexPositionSize);
            break;
          }
          case "VNRM": {
            var normalSize = 3;
            Asserts.Equal(0, sectionSize % normalSize);
            this.ReadNormals_(er, sectionSize / normalSize);
            break;
          }
          case "VNBT": {
            var endianness = er.Endianness;
            er.Endianness = Endianness.BigEndian;

            var nbtSize = 4 * 9;
            Asserts.Equal(0, sectionSize % nbtSize);
            var nbtCount = sectionSize / nbtSize;
            for (var i = 0; i < nbtCount; ++i) {
              this.Normals.Add(new VertexNormal {
                  X = er.ReadSingle(),
                  Y = er.ReadSingle(),
                  Z = er.ReadSingle(),
              });
              er.Position += 24;
            }

            er.Endianness = endianness;
            break;
          }
          case "XBST": {
            this.ReadOpcodes_(er, sectionSize, ref vertexDescriptorValue);
            break;
          }
          case "SCNT": {
            var endianness = er.Endianness;
            er.Endianness = Endianness.BigEndian;

            // TODO: Support this
            // This explains why multiple VPOS sections are included.
            Asserts.Equal(4, sectionSize);
            var lodCount = er.ReadUInt32();

            er.Endianness = endianness;

            break;
          }
          case "VCOL": {
            er.Position += sectionSize;
            break;
          }
          case "ANIM": {
            er.Position += sectionSize;
            break;
          }
          default: throw new NotImplementedException();
        }

        Asserts.Equal(er.Position, expectedSectionEnd);
      }

      BreakEarly: ;
      Asserts.Equal(er.Position, expectedNodeEnd);
    }


    public Bw1RnodMatrix[] Matrices { get; set; }

    private void ReadRnod_(EndianBinaryReader er) {
      var size = er.ReadUInt32();
      this.Matrices = new Bw1RnodMatrix[size];

      for (var i = 0; i < this.Matrices.Length; ++i) {
        this.Matrices[i] = er.ReadNew<Bw1RnodMatrix>();
      }
    }


    public Uv[][] UvMaps { get; } = new Uv[4][];

    private void ReadUvMap_(EndianBinaryReader er,
                            int uvMapIndex,
                            int uvCount) {
      var endianness = er.Endianness;
      er.Endianness = Endianness.BigEndian;

      var scale = MathF.Pow(2, 11);
      var uvMap = this.UvMaps[uvMapIndex] = new Uv[uvCount];
      for (var i = 0; i < uvCount; ++i) {
        uvMap[i] = new Uv {
            U = er.ReadInt16() / scale,
            V = er.ReadInt16() / scale,
        };
      }

      er.Endianness = endianness;
    }

    public class Uv {
      public float U { get; set; }
      public float V { get; set; }
    }


    public List<VertexPosition> Positions { get; } = new();

    private void ReadPositions_(EndianBinaryReader er, int vertexCount) {
      var endianness = er.Endianness;
      er.Endianness = Endianness.BigEndian;

      for (var i = 0; i < vertexCount; ++i) {
        this.Positions.Add(er.ReadNew<VertexPosition>());
      }

      er.Endianness = endianness;
    }


    public List<VertexNormal> Normals { get; } = new();

    private void ReadNormals_(EndianBinaryReader er, int vertexCount) {
      var endianness = er.Endianness;
      er.Endianness = Endianness.BigEndian;

      for (var i = 0; i < vertexCount; ++i) {
        this.Normals.Add(er.ReadNew<VertexNormal>());
      }

      er.Endianness = endianness;
    }

    public List<BwMesh> Meshes { get; } = new();

    private void ReadOpcodes_(EndianBinaryReader er,
                              int sectionSize,
                              ref uint vertexDescriptorValue) {
      var endianness = er.Endianness;
      er.Endianness = Endianness.BigEndian;

      var start = er.Position;
      var expectedEnd = start + sectionSize;

      var materialIndex = er.ReadUInt32();

      // This may look simple, but it was an ABSOLUTE nightmare to reverse engineer, lol.
      var posMatIdxMap = new int[10];
      {
        var posMatIdxOffsetFlags = er.ReadUInt32();

        var currentOffset = 0;
        var currentPosMatIdx = 0;

        // Loops over each bit in the offset.
        for (var i = 0; i < 32; ++i) {
          var currentBit = ((posMatIdxOffsetFlags >> i) & 1) == 1;

          // If bit is true, then we increment the current posMatIdx.
          if (currentBit) {
            posMatIdxMap[currentPosMatIdx] = currentPosMatIdx + currentOffset;
            currentPosMatIdx++;
          }
          // Otherwise, if bit is false, then we increment the current offset.
          else {
            currentOffset++;
          }
        }
      }

      var gxDataSize = er.ReadUInt32();
      Asserts.Equal(expectedEnd, er.Position + gxDataSize);

      var triangleStrips = new List<BwTriangleStrip>();
      var mesh = new BwMesh {
          MaterialIndex = materialIndex,
          TriangleStrips = triangleStrips
      };
      this.Meshes.Add(mesh);

      while (er.Position < expectedEnd) {
        var opcode = er.ReadByte();
        var opcodeEnum = (GxOpcode) opcode;

        if (opcodeEnum == GxOpcode.LOAD_CP_REG) {
          var command = er.ReadByte();
          var value = er.ReadUInt32();

          if (command == 0x50) {
            vertexDescriptorValue &= ~ ((uint) 0x1FFFF);
            vertexDescriptorValue |= value;
          } else if (command == 0x60) {
            value <<= 17;
            vertexDescriptorValue &= 0x1FFFF;
            vertexDescriptorValue |= value;
          } else {
            throw new NotImplementedException();
          }
        } else if (opcodeEnum == GxOpcode.LOAD_XF_REG) {
          var lengthMinusOne = er.ReadUInt16();
          var length = lengthMinusOne + 1;

          // http://hitmen.c02.at/files/yagcd/yagcd/chap5.html#sec5.11.4
          var firstXfRegisterAddress = er.ReadUInt16();

          var values = er.ReadUInt32s(length);
          // TODO: Implement
        } else if (opcodeEnum == GxOpcode.DRAW_TRIANGLE_STRIP) {
          var vertexAttributeIndicesList = new List<BwVertexAttributeIndices>();

          var vertexDescriptor = new GxVertexDescriptor();
          vertexDescriptor.FromValue(vertexDescriptorValue);

          var triangleStrip = new BwTriangleStrip {
              VertexAttributeIndicesList = vertexAttributeIndicesList,
          };
          triangleStrips.Add(triangleStrip);

          var vertexCount = er.ReadUInt16();
          for (var i = 0; i < vertexCount; ++i) {
            var vertexAttributeIndices = new BwVertexAttributeIndices {
                Fraction = 1d * i / vertexCount
            };
            vertexAttributeIndicesList.Add(vertexAttributeIndices);

            foreach (var (vertexAttribute, vertexFormat) in
                     vertexDescriptor) {
              var value = vertexFormat switch {
                  null => er.ReadByte(),
                  GxAttributeType.INDEX_8 => er.ReadByte(),
                  GxAttributeType.INDEX_16 => er.ReadUInt16(),
                  _ => throw new NotImplementedException(),
              };

              switch (vertexAttribute) {
                case GxVertexAttribute.PosMatIdx: {
                  Asserts.Equal(0, value % 3);
                  value /= 3;
                  vertexAttributeIndices.NodeIndex = posMatIdxMap[value];
                  break;
                }
                case GxVertexAttribute.Position: {
                  vertexAttributeIndices.PositionIndex = value;
                  break;
                }
                case GxVertexAttribute.Normal: {
                  vertexAttributeIndices.NormalIndex = value;
                  break;
                }
                case GxVertexAttribute.Tex0Coord:
                case GxVertexAttribute.Tex1Coord:
                case GxVertexAttribute.Tex2Coord:
                case GxVertexAttribute.Tex3Coord:
                case GxVertexAttribute.Tex4Coord:
                case GxVertexAttribute.Tex5Coord:
                case GxVertexAttribute.Tex6Coord:
                case GxVertexAttribute.Tex7Coord: {
                  var index = vertexAttribute - GxVertexAttribute.Tex0Coord;
                  vertexAttributeIndices.TexCoordIndices[index] = value;
                  break;
                }
                case GxVertexAttribute.Color0:
                case GxVertexAttribute.Color1: {
                  break;
                }
                default: {
                  throw new NotImplementedException();
                }
              }
            }
          }
        } else if (opcodeEnum == GxOpcode.NOP) { } else {
          throw new NotImplementedException();
        }
      }

      er.Endianness = endianness;
      Asserts.Equal(expectedEnd, er.Position);
    }


    public class BwMesh {
      public uint Flags { get; set; }
      public uint MaterialIndex { get; set; }
      public List<BwTriangleStrip> TriangleStrips { get; set; }
    }

    public class BwTriangleStrip {
      public List<BwVertexAttributeIndices> VertexAttributeIndicesList {
        get;
        set;
      }
    }

    public class BwVertexAttributeIndices {
      public double Fraction { get; set; }
      public ushort PositionIndex { get; set; }
      public ushort? NormalIndex { get; set; }
      public int? NodeIndex { get; set; }
      public ushort?[] TexCoordIndices { get; } = new ushort?[8];
    }
  }
}