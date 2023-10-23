﻿using System.Numerics;

using fin.data.lazy;
using fin.data.queues;
using fin.io;
using fin.math.rotations;
using fin.model;
using fin.model.impl;
using fin.model.io.importers;

using modl.schema.anim;
using modl.schema.anim.bw1;
using modl.schema.anim.bw2;
using modl.schema.modl;
using modl.schema.modl.bw1;
using modl.schema.modl.bw1.node;
using modl.schema.modl.bw2;
using modl.schema.res.texr;

using schema.binary;

namespace modl.api {
  public class ModlModelImporter : IAsyncModelImporter<ModlModelFileBundle> {
    public Task<IModel> ImportModelAsync(ModlModelFileBundle modelFileBundle)
      => this.ImportModelAsync(modelFileBundle.ModlFile,
                               modelFileBundle.AnimFiles?.ToArray(),
                               modelFileBundle.GameVersion);

    public async Task<IModel> ImportModelAsync(
        IReadOnlyTreeFile modlFile,
        IList<IReadOnlyTreeFile>? animFiles,
        GameVersion gameVersion) {
      var flipSign = ModlFlags.FLIP_HORIZONTALLY ? -1 : 1;

      using var br = new SchemaBinaryReader(modlFile.OpenRead(),
                                            Endianness.BigEndian);
      var bwModel = gameVersion switch {
          GameVersion.BW1 => (IModl) br.ReadNew<Bw1Modl>(),
          GameVersion.BW2 => br.ReadNew<Bw2Modl>(),
      };

      var model = new ModelImpl<Normal1Color1UvVertexImpl>(
          (index, position) => new Normal1Color1UvVertexImpl(index, position));
      var finMesh = model.Skin.AddMesh();

      var finBones = new IBone[bwModel.Nodes.Count];
      var finBonesByModlNode = new Dictionary<IBwNode, IBone>();
      var finBonesByIdentifier = new Dictionary<string, IBone>();

      {
        var nodeQueue =
            new FinTuple2Queue<IBone, ushort>((model.Skeleton.Root, 0));

        while (nodeQueue.TryDequeue(out var parentFinBone,
                                    out var modlNodeId)) {
          var modlNode = bwModel.Nodes[modlNodeId];

          var transform = modlNode.Transform;
          var bonePosition = transform.Position;

          // TODO: This is a major, dumb hack to fix alignment of the rig with vertices for a single model.
          // It seems like this might be possible to fix by using the inverse binding matrices from RNOD matrices?
          if (gameVersion == GameVersion.BW2 &&
              modlFile.Name == "SG_HI_LOD.modl" &&
              modlNodeId == 0) {
            bonePosition.Z = 0;
          }

          var modlRotation = transform.Rotation;
          var rotation = new Quaternion(
              flipSign * modlRotation.X,
              modlRotation.Y,
              modlRotation.Z,
              flipSign * modlRotation.W);
          var eulerRadians = QuaternionUtil.ToEulerRadians(rotation);

          var finBone =
              parentFinBone
                  .AddChild(flipSign * bonePosition.X,
                            bonePosition.Y,
                            bonePosition.Z)
                  .SetLocalRotationRadians(
                      eulerRadians.X,
                      eulerRadians.Y,
                      eulerRadians.Z);

          var identifier = modlNode.GetIdentifier();
          finBone.Name = identifier;
          finBones[modlNodeId] = finBone;
          finBonesByModlNode[modlNode] = finBone;
          finBonesByIdentifier[identifier] = finBone;

          if (bwModel.CnctParentToChildren.TryGetList(
                  modlNodeId,
                  out var modlChildIds)) {
            nodeQueue.Enqueue(
                modlChildIds!.Select(modlChildId => (finBone, modlChildId)));
          }
        }

        foreach (var animFile in animFiles ?? Array.Empty<ISystemFile>()) {
          AddAnimFileToModel_(model,
                              animFile,
                              gameVersion,
                              flipSign,
                              finBonesByIdentifier);
        }

        var levelDir = modlFile.AssertGetParent();
        var baseLevelDir = levelDir.AssertGetParent();
        var textureDictionary = new LazyDictionary<string, Task<ITexture>>(
            async textureNameWithoutExtension => {
              var textureName = $"{textureNameWithoutExtension}.texr";
              IReadOnlyTreeFile textureFile;
              if (!levelDir.TryToGetExistingFile(
                      textureName,
                      out textureFile)) {
                textureFile = baseLevelDir
                              .GetFilesWithNameRecursive(textureName)
                              .First();
              }

              var texr = gameVersion == GameVersion.BW2
                  ? (ITexr) textureFile.ReadNew<Gtxd>()
                  : textureFile.ReadNew<Text>();
              var image = texr.Image;

              var finTexture =
                  model.MaterialManager.CreateTexture(image);
              finTexture.Name = textureName;

              // TODO: Need to handle wrapping
              finTexture.WrapModeU = WrapMode.REPEAT;
              finTexture.WrapModeV = WrapMode.REPEAT;

              return finTexture;
            });

        foreach (var modlNode in bwModel.Nodes) {
          if (modlNode.IsHidden) {
            continue;
          }

          var modlMaterials = modlNode.Materials;
          var finMaterials = new ITextureMaterial[modlMaterials.Count];
          await Task.WhenAll(modlMaterials.Select(async (modlMaterial, i) => {
                      var textureName = modlMaterial.Texture1.ToLower();
                      if (textureName == "") {
                        return;
                      }

                      var finTexture = await textureDictionary[textureName];
                      finMaterials[i] = model.MaterialManager
                                             .AddTextureMaterial(finTexture);
                    }))
                    .ConfigureAwait(false);

          foreach (var modlMesh in modlNode.Meshes) {
            var finMaterial = finMaterials[modlMesh.MaterialIndex];

            foreach (var triangleStrip in modlMesh.TriangleStrips) {
              var vertices =
                  new IVertex[triangleStrip.VertexAttributeIndicesList.Count];
              for (var i = 0; i < vertices.Length; i++) {
                var vertexAttributeIndices =
                    triangleStrip.VertexAttributeIndicesList[i];

                var position =
                    modlNode.Positions[vertexAttributeIndices.PositionIndex];
                var vertex = model.Skin.AddVertex(
                    flipSign * position.X * modlNode.Scale,
                    position.Y * modlNode.Scale,
                    position.Z * modlNode.Scale);
                vertices[i] = vertex;

                if (vertexAttributeIndices.NormalIndex != null) {
                  var normal =
                      modlNode.Normals[
                          vertexAttributeIndices.NormalIndex.Value];
                  vertex.SetLocalNormal(flipSign * normal.X,
                                        normal.Y,
                                        normal.Z);
                }

                if (vertexAttributeIndices.NodeIndex != null) {
                  var finBone =
                      finBones[vertexAttributeIndices.NodeIndex.Value];
                  vertex.SetBoneWeights(
                      model.Skin
                           .GetOrCreateBoneWeights(
                               VertexSpace.WORLD,
                               new BoneWeight(finBone, null, 1)));
                } else {
                  var finBone = finBonesByModlNode[modlNode];
                  vertex.SetBoneWeights(
                      model.Skin.GetOrCreateBoneWeights(
                          VertexSpace.BONE,
                          finBone));
                }

                var texCoordIndex0 = vertexAttributeIndices.TexCoordIndices[0];
                var texCoordIndex1 = vertexAttributeIndices.TexCoordIndices[1];
                if (texCoordIndex1 != null) {
                  int texCoordIndex;
                  if (texCoordIndex0 != null) {
                    texCoordIndex =
                        (texCoordIndex0.Value << 8) | texCoordIndex1.Value;
                  } else {
                    texCoordIndex = texCoordIndex1.Value;
                  }

                  var uv = modlNode.UvMaps[0][texCoordIndex];
                  vertex.SetUv(uv.U, uv.V);
                }
              }

              var triangleStripPrimitive = finMesh.AddTriangleStrip(vertices);
              if (finMaterial != null) {
                triangleStripPrimitive.SetMaterial(finMaterial);
              }
            }
          }
        }
      }

      return model;
    }

    private static void AddAnimFileToModel_(IModel model,
                                            IReadOnlyTreeFile animFile,
                                            GameVersion gameVersion,
                                            int flipSign,
                                            IDictionary<string, IBone>
                                                finBonesByIdentifier) {
      var anim = gameVersion switch {
          GameVersion.BW1 => (IAnim) animFile.ReadNew<Bw1Anim>(
              Endianness.BigEndian),
          GameVersion.BW2 => animFile.ReadNew<Bw2Anim>(
              Endianness.BigEndian)
      };

      var maxFrameCount = -1;
      foreach (var animBone in anim.AnimBones) {
        maxFrameCount = (int) Math.Max(maxFrameCount,
                                       Math.Max(
                                           animBone
                                               .PositionKeyframeCount,
                                           animBone
                                               .RotationKeyframeCount));
      }

      var finAnimation = model.AnimationManager.AddAnimation();
      finAnimation.Name = animFile.NameWithoutExtension;
      finAnimation.FrameRate = 30;
      finAnimation.FrameCount = maxFrameCount;

      for (var b = 0; b < anim.AnimBones.Count; ++b) {
        var animBone = anim.AnimBones[b];
        var animBoneFrames = anim.AnimBoneFrames[b];

        var animNodeIdentifier = animBone.GetIdentifier();
        if (!finBonesByIdentifier.TryGetValue(
                animNodeIdentifier,
                out var finBone)) {
          // TODO: Gross hack for the vet models, what's the real fix???
          if (animNodeIdentifier == Bw1Node.GetIdentifier(33)) {
            finBone = finBonesByIdentifier[Bw1Node.GetIdentifier(34)];
          } else if (finBonesByIdentifier.TryGetValue(
                         animNodeIdentifier + 'X',
                         out var xBone)) {
            finBone = xBone;
          } else if (finBonesByIdentifier.TryGetValue(
                         "BONE_" + animNodeIdentifier,
                         out var prefixBone)) {
            finBone = prefixBone;
          } else if (animNodeIdentifier == "WF_GRUNT_BACKPAC") {
            // TODO: Is this right?????
            finBone = finBonesByIdentifier["BONE_BCK_MISC"];
          } else {
            ;
          }
        }

        var finBoneTracks = finAnimation.AddBoneTracks(
            finBone!);

        var fbtPositions =
            finBoneTracks.UseCombinedPositionAxesTrack(
                (int) animBone.PositionKeyframeCount);
        for (var f = 0; f < animBone.PositionKeyframeCount; ++f) {
          var (fPX, fPY, fPZ) = animBoneFrames.PositionFrames[f];
          fbtPositions.Set(f, new Position(flipSign * fPX, fPY, fPZ));
        }

        var fbtRotations =
            finBoneTracks.UseQuaternionRotationTrack(
                (int) animBone.RotationKeyframeCount);
        for (var f = 0; f < animBone.RotationKeyframeCount; ++f) {
          var (fRX, fRY, fRZ, frW) = animBoneFrames.RotationFrames[f];

          var animationQuaternion =
              new Quaternion(flipSign * fRX, fRY, fRZ, flipSign * frW);

          fbtRotations.Set(f, animationQuaternion);
        }
      }
    }
  }
}