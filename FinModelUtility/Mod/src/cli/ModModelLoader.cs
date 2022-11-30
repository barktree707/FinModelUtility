﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

using fin.color;
using fin.io;
using fin.math;
using fin.model;
using fin.model.impl;
using fin.util.asserts;

using gx;

using mod.schema;
using mod.schema.animation;
using mod.util;

using Endianness = mod.util.Endianness;


namespace mod.cli {
  public class ModModelFileBundle : IModelFileBundle {
    public IFileHierarchyFile MainFile => this.ModFile;

    public IFileHierarchyFile ModFile { get; set; }
    public IFileHierarchyFile? AnmFile { get; set; }
  }

  public class ModModelLoader : IModelLoader<ModModelFileBundle> {
    /// <summary>
    ///   GX's active matrices. These are deferred to when a vertex matrix is
    ///   -1, which corresponds to using an active matrix from a previous
    ///   display list.
    /// </summary>
    private short[] activeMatrices_ = new short[10];

    public enum TilingMode {
      REPEAT = 0,
      CLAMP = 1,
      MIRROR_REPEAT = 2,
    }

    public static WrapMode ConvertGcnToFin(TilingMode tilingMode)
      => tilingMode switch {
          TilingMode.CLAMP         => WrapMode.CLAMP,
          TilingMode.MIRROR_REPEAT => WrapMode.MIRROR_REPEAT,
          _                        => WrapMode.REPEAT,
      };

    public IModel LoadModel(ModModelFileBundle modelFileBundle) {
      var mod = new Mod();
      {
        using var r = new EndianBinaryReader(
            modelFileBundle.ModFile.Impl.OpenRead(),
            System.IO.Endianness.BigEndian);
        mod.Read(r);
      }

      Anm? anm = null;
      if (modelFileBundle.AnmFile != null) {
        anm = new Anm();
        using var r = new EndianBinaryReader(
            modelFileBundle.AnmFile.Impl.OpenRead(),
            System.IO.Endianness.BigEndian);
        anm.Read(r);
      }

      // Resets the active matrices to -1. This lets us catch issues when
      // attempting to use an invalid active matrix.
      for (var i = 0; i < 10; ++i) {
        this.activeMatrices_[i] = -1;
      }

      var finModCache = new FinModCache(mod);

      var model = new ModelImpl();

      var hasVertices = mod.vertices.Any();
      var hasNormals = mod.vnormals.Any();
      var hasFaces = mod.colltris.collinfo.Any();

      if (!hasVertices && !hasNormals && !hasFaces) {
        Asserts.Fail("Loaded file has nothing to export!");
      }

      /*var colInfos = mod.colltris.collinfo;
      if (colInfos.Count != 0) {
        os.WriteLine();
        os.WriteLine("o collision mesh");
        foreach (var colInfo in colInfos) {
          os.WriteLine(
              $"f ${colInfo.indice.X + 1} ${colInfo.indice.Y + 1} ${colInfo.indice.Z + 1}");
        }
      }*/

      // Writes textures
      var finTexturesAndAttrs =
          new (ITexture, TextureAttributes)[mod.texattrs.Count];
      for (var i = 0; i < mod.texattrs.Count; ++i) {
        var textureAttr = mod.texattrs[i];

        var textureIndex = textureAttr.index;
        var texture = mod.textures[textureIndex];
        var image = texture.ToImage();

        var finTexture =
            model.MaterialManager.CreateTexture(image);
        finTexture.Name = $"texattr {i}";

        // TODO: These might be backwards
        var tilingS =
            (TilingMode) BitLogic.ExtractFromRight(
                textureAttr.tilingMode,
                0,
                8);
        var tilingT =
            (TilingMode) BitLogic.ExtractFromRight(
                textureAttr.tilingMode,
                8,
                8);

        finTexture.WrapModeU = ModModelLoader.ConvertGcnToFin(tilingS);
        finTexture.WrapModeV = ModModelLoader.ConvertGcnToFin(tilingT);
        // TODO: Set attributes

        finTexturesAndAttrs[i] = (finTexture, textureAttr);
      }

      // Writes materials
      var finMaterials = new List<IMaterial>();
      for (var i = 0; i < mod.materials.materials.Count; ++i) {
        var material = mod.materials.materials[i];

        ITexture? finTexture = null;

        var texturesInMaterial = material.texInfo.unknown4;
        if (texturesInMaterial.Length > 0) {
          var textureInMaterial = texturesInMaterial[0];

          var texAttrIndex = textureInMaterial.unknown1;
          TextureAttributes texAttr;
          (finTexture, texAttr) = finTexturesAndAttrs[texAttrIndex];

          ;
        }

        IMaterial finMaterial = finTexture != null
                                    ? model.MaterialManager.AddTextureMaterial(
                                        finTexture)
                                    : model.MaterialManager.AddNullMaterial();

        finMaterial.Name = $"material {i}";
        finMaterials.Add(finMaterial);
      }

      // Writes bones
      // TODO: Simplify these loops
      var jointCount = mod.joints.Count;
      // Pass 1: Creates lists at each index in joint children
      var jointChildren = new List<int>[jointCount];
      for (var i = 0; i < jointCount; ++i) {
        jointChildren[i] = new();
      }
      // Pass 2: Gathers up children of each bone via parent index
      for (var i = 0; i < jointCount; ++i) {
        var joint = mod.joints[i];
        var parentIndex = (int) joint.parentIdx;
        if (parentIndex != -1) {
          jointChildren[parentIndex].Add(i);
        }
      }

      // Pass 3: Creates skeleton
      var bones = new IBone[jointCount];

      // TODO: Is 0 as the first a safe assumption?
      var jointQueue = new Queue<(int, IBone?)>();
      jointQueue.Enqueue((0, null));
      while (jointQueue.Count > 0) {
        var (jointIndex, parent) = jointQueue.Dequeue();

        var joint = mod.joints[jointIndex];

        var bone = (parent ?? model.Skeleton.Root).AddChild(
            joint.position.X,
            joint.position.Y,
            joint.position.Z);
        bone.SetLocalRotationRadians(joint.rotation.X,
                                     joint.rotation.Y,
                                     joint.rotation.Z);
        bone.SetLocalScale(joint.scale.X, joint.scale.Y, joint.scale.Z);

        if (mod.jointNames.Count > 0) {
          var jointName = mod.jointNames[jointIndex];
          bone.Name = jointName;
        } else {
          bone.Name = $"bone {jointIndex}";
        }

        bones[jointIndex] = bone;

        foreach (var childIndex in jointChildren[jointIndex]) {
          jointQueue.Enqueue((childIndex, bone));
        }
      }

      // Pass 4: Writes each bone's meshes as skin
      var envelopeBoneWeights =
          mod.envelopes.Select(
                 envelope =>
                     model.Skin.CreateBoneWeights(
                         PreprojectMode.NONE,
                         envelope.indicesAndWeights
                                 .Select(
                                     indexAndWeight =>
                                         new BoneWeight(
                                             bones[indexAndWeight.index],
                                             null,
                                             indexAndWeight.weight)
                                 )
                                 .ToArray()))
             .ToArray();

      foreach (var joint in mod.joints) {
        foreach (var jointMatPoly in joint.matpolys) {
          var meshIndex = jointMatPoly.meshIdx;
          var mesh = mod.meshes[meshIndex];

          var material = finMaterials[jointMatPoly.matIdx];
          this.AddMesh_(mod,
                        mesh,
                        material,
                        model,
                        bones,
                        envelopeBoneWeights,
                        finModCache);
        }
      }

      // Converts animations
      if (anm != null) {
        foreach (var dcxWrapper in anm.Wrappers) {
          DcxHelpers.AddAnimation(bones, model.AnimationManager,
                                  dcxWrapper.Dcx);
        }
      }

      return model;
    }

    private void AddMesh_(
        Mod mod,
        Mesh mesh,
        IMaterial material,
        IModel model,
        IBone[] bones,
        IBoneWeights[] envelopeBoneWeights,
        FinModCache finModCache) {
      //var currentBone = bones[mesh.boneIndex];
      var currentColor = FinColor.FromRgbaBytes(255, 255, 255, 255);

      var vertexDescriptor = new VertexDescriptor();
      vertexDescriptor.FromPikmin1(mesh.vtxDescriptor, mod.hasNormals);

      var finSkin = model.Skin;
      var finMesh = finSkin.AddMesh();

      foreach (var meshPacket in mesh.packets) {
        foreach (var dlist in meshPacket.displaylists) {
          var reader = new VectorReader(dlist.dlistData, 0, Endianness.Big);

          while (reader.GetRemaining() != 0) {
            var opcodeByte = reader.ReadU8();
            var opcode = (GxOpcode) opcodeByte;

            if (opcode == GxOpcode.NOP) {
              continue;
            }

            if (opcode != GxOpcode.DRAW_TRIANGLE_STRIP &&
                opcode != GxOpcode.DRAW_TRIANGLE_FAN) {
              continue;
            }

            var faceCount = reader.ReadU16();
            var positionIndices = new List<ushort>();
            var allVertexWeights = new List<VertexWeights>();
            var normalIndices = new List<ushort>();
            var color0Indices = new List<ushort>();

            var texCoordIndices = new List<ushort>[8];
            for (var t = 0; t < 8; ++t) {
              texCoordIndices[t] = new List<ushort>();
            }

            for (var f = 0; f < faceCount; f++) {
              foreach (var (attr, format) in vertexDescriptor) {
                if (format == null) {
                  var unused = reader.ReadU8();

                  if (attr == GxAttribute.PNMTXIDX) {
                    // Internally, this represents which of the 10 active
                    // matrices to bind to.
                    var activeMatrixIndex = unused / 3;

                    Asserts.Equal(0, unused % 3);

                    // This represents which vertex matrix the active matrix is
                    // sourced from.
                    var vertexMatrixIndex =
                        meshPacket.indices[activeMatrixIndex];

                    // -1 means no active matrix set by this display list,
                    // defers to whatever the existing matrix is in this slot.
                    if (vertexMatrixIndex == -1) {
                      vertexMatrixIndex =
                          this.activeMatrices_[activeMatrixIndex];
                      Asserts.False(vertexMatrixIndex == -1);
                    }
                    this.activeMatrices_[activeMatrixIndex] = vertexMatrixIndex;

                    // TODO: Is there a real name for this?
                    // Remaps from vertex matrix to "attachment" index.
                    var attachmentIndex =
                        mod.vtxMatrix[vertexMatrixIndex].index;

                    // Positive indices refer to joints/bones.
                    if (attachmentIndex >= 0) {
                      var boneIndex = attachmentIndex;

                      var vertexWeights = new VertexWeights {
                          BoneWeights =
                              finSkin.GetOrCreateBoneWeights(
                                  PreprojectMode.BONE, bones[boneIndex])
                      };
                      allVertexWeights.Add(vertexWeights);
                    }
                    // Negative indices refer to envelopes.
                    else {
                      var envelopeIndex = -1 - attachmentIndex;

                      var vertexWeights = new VertexWeights {
                          BoneWeights = envelopeBoneWeights[envelopeIndex],
                      };

                      allVertexWeights.Add(vertexWeights);
                    }
                  }

                  continue;
                }

                if (attr == GxAttribute.POS) {
                  positionIndices.Add(ModModelLoader.Read_(reader, format));
                } else if (attr == GxAttribute.NRM) {
                  normalIndices.Add(ModModelLoader.Read_(reader, format));
                } else if (attr == GxAttribute.CLR0) {
                  color0Indices.Add(ModModelLoader.Read_(reader, format));
                } else if (attr is >= GxAttribute.TEX0
                                   and <= GxAttribute.TEX7) {
                  texCoordIndices[attr - GxAttribute.TEX0]
                      .Add(ModModelLoader.Read_(reader, format));
                } else if (format == GxAttributeType.INDEX_16) {
                  reader.ReadU16();
                } else {
                  Asserts.Fail(
                      $"Unexpected attribute/format ({attr}/{format})");
                }
              }
            }

            var finVertexList = new List<IVertex>();
            for (var v = 0; v < positionIndices.Count; ++v) {
              var position = finModCache.PositionsByIndex[positionIndices[v]];
              var finVertex =
                  model.Skin.AddVertex(position);

              if (allVertexWeights.Count > 0) {
                finVertex.SetBoneWeights(allVertexWeights[v].BoneWeights);
              }

              // TODO: For collision models, there can be normal indices when
              // there are 0 normals. What does this mean? Is this how surface
              // types are defined?
              if (normalIndices.Count > 0 && mod.vnormals.Count > 0) {
                var normalIndex = normalIndices[v];

                if (!vertexDescriptor.useNbt) {
                  var normal = finModCache.NormalsByIndex[normalIndex];
                  finVertex.SetLocalNormal(normal);
                } else {
                  var normal = finModCache.NbtNormalsByIndex[normalIndex];
                  var tangent = finModCache.TangentsByIndex[normalIndex];
                  finVertex.SetLocalNormal(normal);
                  finVertex.SetLocalTangent(tangent);
                }
              }

              if (color0Indices.Count > 0) {
                var color = finModCache.ColorsByIndex[color0Indices[v]];
                finVertex.SetColor(color);
              } else {
                finVertex.SetColor(finModCache.Default);
              }

              for (var t = 0; t < 8; ++t) {
                if (texCoordIndices[t].Count > 0) {
                  var texCoord =
                      finModCache.TexCoordsByIndex[t][texCoordIndices[t][v]];
                  finVertex.SetUv(t, texCoord);
                }
              }

              finVertexList.Add(finVertex);
            }

            var finVertices = finVertexList.ToArray();
            if (opcode == GxOpcode.DRAW_TRIANGLE_FAN) {
              finMesh.AddTriangleFan(finVertices).SetMaterial(material);
            } else if (opcode == GxOpcode.DRAW_TRIANGLE_STRIP) {
              finMesh.AddTriangleStrip(finVertices).SetMaterial(material);
            }
          }
        }
      }
    }

    private record FinModCache {
      public IPosition[] PositionsByIndex { get; }

      public INormal[] NormalsByIndex { get; }

      public INormal[] NbtNormalsByIndex { get; }
      public ITangent[] TangentsByIndex { get; }

      public IColor[] ColorsByIndex { get; }

      public IColor Default { get; } =
        FinColor.FromRgbaBytes(255, 255, 255, 255);

      public ITexCoord[][] TexCoordsByIndex { get; }

      public FinModCache(Mod mod) {
        this.PositionsByIndex =
            mod.vertices.Select(
                   position => new ModelImpl.PositionImpl {
                       X = position.X,
                       Y = position.Y,
                       Z = position.Z,
                   })
               .ToArray();
        this.NormalsByIndex =
            mod.vnormals.Select(
                   vnormals => new ModelImpl.NormalImpl {
                       X = vnormals.X,
                       Y = vnormals.Y,
                       Z = vnormals.Z,
                   })
               .ToArray();
        this.NbtNormalsByIndex =
            mod.vertexnbt.Select(vertexnbt => new ModelImpl.NormalImpl {
                   X = vertexnbt.Normal.X,
                   Y = vertexnbt.Normal.Y,
                   Z = vertexnbt.Normal.Z,
               })
               .ToArray();
        this.TangentsByIndex = mod.vertexnbt.Select(
                                      vertexnbt => new ModelImpl.TangentImpl {
                                          X = vertexnbt.Tangent.X,
                                          Y = vertexnbt.Tangent.Y,
                                          Z = vertexnbt.Tangent.Z,
                                          W = 0,
                                      })
                                  .ToArray();
        this.ColorsByIndex =
            mod.vcolours.ToArray();
        this.TexCoordsByIndex =
            mod.texcoords.Select(
                   texcoords
                       => texcoords.Select(
                                       texcoord => new ModelImpl.TexCoordImpl {
                                           U = texcoord.X,
                                           V = texcoord.Y,
                                       })
                                   .ToArray())
               .ToArray();
      }
    }

    private static ushort Read_(VectorReader reader, GxAttributeType? format) {
      if (format == GxAttributeType.INDEX_16) {
        return reader.ReadU16();
      } else if (format == GxAttributeType.INDEX_8) {
        return reader.ReadU8();
      }
      Asserts.Fail($"Unsupported format: {format}");
      return 0;
    }

    private class VertexWeights {
      public IBoneWeights BoneWeights { get; set; }
    }
  }
}