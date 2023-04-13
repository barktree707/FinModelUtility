﻿using System;
using System.Drawing;
using System.Linq;

using f3dzex2.displaylist;
using f3dzex2.displaylist.opcodes;
using f3dzex2.image;
using f3dzex2.io;

using fin.data.lazy;
using fin.image;
using fin.math;
using fin.math.matrix;
using fin.model;
using fin.model.impl;
using fin.util.enums;
using fin.util.hash;

using IImage = fin.image.IImage;


namespace f3dzex2.model {
  public struct ImageParams {
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public uint SegmentedAddress { get; set; }

    public override int GetHashCode() => FluentHash.Start()
                                                   .With(this.Width)
                                                   .With(this.Height)
                                                   .With(SegmentedAddress);

    public bool IsInvalid => this.Width == 0 || this.Height == 0 ||
                             this.SegmentedAddress == 0;

    public override bool Equals(object? other) {
      if (ReferenceEquals(this, other)) {
        return true;
      }

      if (other is ImageParams otherImageParams) {
        return Width == otherImageParams.Width &&
               Height == otherImageParams.Height &&
               SegmentedAddress == otherImageParams.SegmentedAddress;
      }

      return false;
    }
  }

  public struct TextureParams {
    public ImageParams ImageParams { get; private set; }

    public ushort Width {
      get => this.ImageParams.Width;
      set {
        ImageParams imageParams = this.ImageParams;
        imageParams.Width = value;
        this.ImageParams = imageParams;
      }
    }

    public ushort Height {
      get => this.ImageParams.Height;
      set {
        ImageParams imageParams = this.ImageParams;
        imageParams.Height = value;
        this.ImageParams = imageParams;
      }
    }

    public uint SegmentedAddress {
      get => this.ImageParams.SegmentedAddress;
      set {
        ImageParams imageParams = this.ImageParams;
        imageParams.SegmentedAddress = value;
        this.ImageParams = imageParams;
      }
    }

    public WrapMode WrapModeU { get; set; }
    public WrapMode WrapModeV { get; set; }

    public override int GetHashCode() => FluentHash.Start()
                                                   .With(this.ImageParams)
                                                   .With(WrapModeU)
                                                   .With(WrapModeV);

    public override bool Equals(object? other) {
      if (ReferenceEquals(this, other)) {
        return true;
      }

      if (other is TextureParams otherTextureParams) {
        return ImageParams.Equals(otherTextureParams.ImageParams) &&
               WrapModeU == otherTextureParams.WrapModeU &&
               WrapModeV == otherTextureParams.WrapModeV;
      }

      return false;
    }
  }

  public class DlModelBuilder {
    private readonly IN64Memory n64Memory_;
    private IMesh currentMesh_;
    private IMaterial? currentMaterial_;

    private GeometryMode geometryMode_ = (GeometryMode) 0x22205;
    private TextureParams textureParams_;
    private float texScaleX_ = 1f, texScaleY_ = 1f;
    private N64ColorFormat textureColorFormat_;
    private BitSize textureBitSize_;

    private readonly LazyDictionary<ImageParams, IImage>
        lazyImageDictionary_;

    private readonly LazyDictionary<TextureParams, ITexture>
        lazyTextureDictionary_;

    private readonly LazyDictionary<TextureParams, IMaterial>
        lazyMaterialDictionary_;

    private const int VERTEX_COUNT = 32;

    private readonly F3dVertex[] vertexDefinitions_ =
        new F3dVertex[VERTEX_COUNT];

    private readonly IVertex?[] vertices_ = new IVertex?[VERTEX_COUNT];

    public DlModelBuilder(IN64Memory n64Memory) {
      this.n64Memory_ = n64Memory;
      this.currentMesh_ = this.Model.Skin.AddMesh();
      this.currentMaterial_ = this.Model.MaterialManager.AddNullMaterial();

      lazyImageDictionary_ =
          new(imageParams => {
            if (imageParams.IsInvalid) {
              return FinImage.Create1x1FromColor(Color.White);
            }

            using var er =
                this.n64Memory_.OpenAtSegmentedAddress(
                    this.textureParams_.SegmentedAddress);
            var imageData =
                er.ReadBytes(this.textureParams_.Width *
                             this.textureParams_.Height * 4);

            return new N64ImageParser().Parse(this.textureColorFormat_,
                                              this.textureBitSize_,
                                              imageData,
                                              this.textureParams_.Width,
                                              this.textureParams_.Height,
                                              new ushort[] { },
                                              false);
          });

      lazyTextureDictionary_ =
          new(textureParams
                  => {
                var texture = this.Model.MaterialManager.CreateTexture(
                    this.lazyImageDictionary_[textureParams.ImageParams]);
                texture.Name =
                    String.Format("0x{0:X8}", textureParams.SegmentedAddress);
                texture.WrapModeU = textureParams.WrapModeU;
                texture.WrapModeV = textureParams.WrapModeV;
                return texture;
              });

      lazyMaterialDictionary_ =
          new(textureParams
                  => {
                var texture = this.lazyTextureDictionary_[textureParams];
                var material =
                    this.Model.MaterialManager.AddTextureMaterial(texture);
                material.Name = texture.Name;
                return material;
              });
    }

    public IModel Model { get; } = new ModelImpl();

    public IReadOnlyFinMatrix4x4 Matrix { get; set; } = FinMatrix4x4.IDENTITY;

    public int GetNumberOfTriangles() =>
        this.Model.Skin.Meshes
            .SelectMany(mesh => mesh.Primitives)
            .Select(primitive => primitive.Vertices.Count / 3)
            .Sum();

    public void AddDl(IDisplayList dl, IN64Memory n64Memory) {
      foreach (var opcodeCommand in dl.OpcodeCommands) {
        switch (opcodeCommand) {
          case NoopOpcodeCommand _:
            break;
          case DlOpcodeCommand dlOpcodeCommand: {
            foreach (var childDl in dlOpcodeCommand.PossibleBranches) {
              AddDl(childDl, n64Memory);
            }

            if (!dlOpcodeCommand.PushCurrentDlToStack) {
              return;
            }

            break;
          }
          case EndDlOpcodeCommand _: {
            return;
          }
          case MtxOpcodeCommand mtxOpcodeCommand:
            break;
          case PopMtxOpcodeCommand popMtxOpcodeCommand:
            break;
          case SetEnvColorOpcodeCommand setEnvColorOpcodeCommand:
            break;
          case SetFogColorOpcodeCommand setFogColorOpcodeCommand:
            break;
          // Geometry mode commands
          case SetGeometryModeOpcodeCommand setGeometryModeOpcodeCommand: {
            this.geometryMode_ |= setGeometryModeOpcodeCommand.FlagsToEnable;
            break;
          }
          case ClearGeometryModeOpcodeCommand clearGeometryModeOpcodeCommand: {
            this.geometryMode_ &=
                ~clearGeometryModeOpcodeCommand.FlagsToDisable;
            break;
          }
          case SetTileOpcodeCommand setTileOpcodeCommand: {
            // TODO: Match returning/control flow logic from Fast3DScripts version
            if (setTileOpcodeCommand.TileDescriptor ==
                TileDescriptor.TX_RENDERTILE) {
              this.textureColorFormat_ = setTileOpcodeCommand.ColorFormat;
              this.textureBitSize_ = setTileOpcodeCommand.BitSize;
              this.currentMaterial_ = null;
            }
            // TODO: Support wrap modes
            break;
          }
          case SetTileSizeOpcodeCommand setTileSizeOpcodeCommand: {
            this.textureParams_.Width = setTileSizeOpcodeCommand.Width;
            this.textureParams_.Height = setTileSizeOpcodeCommand.Height;
            this.currentMaterial_ = null;
            break;
          }
          case SetTimgOpcodeCommand setTimgOpcodeCommand: {
            this.textureParams_.SegmentedAddress =
                setTimgOpcodeCommand.TextureSegmentedAddress;
            this.currentMaterial_ = null;
            break;
          }
          case TextureOpcodeCommand textureOpcodeCommand: {
            var tsX = textureOpcodeCommand.HorizontalScaling;
            var tsY = textureOpcodeCommand.VerticalScaling;

            if (this.geometryMode_.HasFlag(GeometryMode.G_TEXTURE_GEN)) {
              this.textureParams_.Width = (ushort) ((tsX >> 6));
              this.textureParams_.Height = (ushort) ((tsY >> 6));
              if (this.textureParams_.Width == 31) this.textureParams_.Width = 32;
              else if (this.textureParams_.Width == 62) this.textureParams_.Width = 64;
              if (this.textureParams_.Height == 31) this.textureParams_.Height = 32;
              else if (this.textureParams_.Height == 62) this.textureParams_.Height = 64;
            } else {
              if (tsX != 0xFFFF)
                texScaleX_ = (float) tsX / 65536.0f;
              else
                texScaleX_ = 1.0f;
              if (tsY != 0xFFFF)
                texScaleY_ = (float) tsY / 65536.0f;
              else
                texScaleY_ = 1.0f;
            }
            break;
          }
          case SetCombineOpcodeCommand setCombineOpcodeCommand: {
            if (setCombineOpcodeCommand.ClearTextureSegmentedAddress) {
              this.textureParams_.SegmentedAddress = 0;
              this.currentMaterial_ = null;
            }
            break;
          }
          case VtxOpcodeCommand vtxOpcodeCommand: {
            var newVertices = vtxOpcodeCommand.Vertices;
            for (var i = 0; i < newVertices.Count; ++i) {
              this.vertexDefinitions_[
                      vtxOpcodeCommand.IndexToBeginStoringVertices + i] =
                  newVertices[i];
              this.vertices_[i] = null;
            }

            break;
          }
          case Tri1OpcodeCommand tri1OpcodeCommand: {
            var vertices =
                tri1OpcodeCommand.VertexIndicesInOrder.Select(
                    GetOrCreateVertexAtIndex_);
            this.currentMesh_.AddTriangles(vertices.ToArray())
                .SetMaterial(this.GetOrCreateMaterial_())
                .SetVertexOrder(VertexOrder.NORMAL);
            break;
          }
          default:
            throw new ArgumentOutOfRangeException(nameof(opcodeCommand));
        }
      }
    }

    private IVertex GetOrCreateVertexAtIndex_(byte index) {
      var existing = this.vertices_[index];
      if (existing != null) {
        return existing;
      }

      var definition = this.vertexDefinitions_[index];

      var position = definition.GetPosition();
      GlMatrixUtil.ProjectPosition(Matrix.Impl, ref position);

      var newVertex = this.Model.Skin.AddVertex(position)
                          .SetUv(definition.GetUv(
                                     this.texScaleX_,
                                     this.texScaleY_));

      if (this.geometryMode_.CheckFlag(GeometryMode.G_LIGHTING)) {
        var normal = definition.GetNormal();
        GlMatrixUtil.ProjectNormal(Matrix.Impl, ref normal);
        newVertex.SetLocalNormal(normal);

        // TODO: Support color in this case
      } else {
        newVertex.SetColor(definition.GetColor());
      }

      this.vertices_[index] = newVertex;
      return newVertex;
    }

    public IMaterial GetOrCreateMaterial_() {
      if (this.currentMaterial_ != null) {
        return this.currentMaterial_;
      }

      return this.lazyMaterialDictionary_[this.textureParams_];
    }
  }
}