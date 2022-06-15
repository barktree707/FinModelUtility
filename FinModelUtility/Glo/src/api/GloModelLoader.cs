﻿using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;

using fin.io;
using fin.math;
using fin.model;
using fin.model.impl;

using glo.schema;

using System.Numerics;

using fin.data;
using fin.util.image;


namespace glo.api {
  public class GloModelFileBundle : IModelFileBundle {
    public GloModelFileBundle(IFileHierarchyFile gloFile,
                              IReadOnlyList<IFileHierarchyDirectory>
                                  textureDirectories) {
      this.GloFile = gloFile;
      this.TextureDirectories = textureDirectories;
    }

    public string FileName => this.GloFile.NameWithoutExtension;

    public IFileHierarchyFile GloFile { get; }
    public IReadOnlyList<IFileHierarchyDirectory> TextureDirectories { get; }
  }

  public class GloModelLoader : IModelLoader<GloModelFileBundle> {
    private readonly string[] hiddenNames_ = new[] {
        "Box01", "puzzle"
    };

    private readonly string[] mirrorTextures_ = new[] {
        "Badg2.bmp"
    };

    public unsafe IModel LoadModel(GloModelFileBundle gloModelFileBundle) {
      var gloFile = gloModelFileBundle.GloFile;
      var textureDirectories = gloModelFileBundle.TextureDirectories;
      var fps = 20;

      var glo = new Glo();
      using (var er =
             new EndianBinaryReader(
                 gloFile.Impl.OpenRead(), Endianness.LittleEndian)) {
        glo.Read(er);
      }

      var textureFilesByName = new Dictionary<string, IFileHierarchyFile>();
      foreach (var textureDirectory in textureDirectories) {
        foreach (var textureFile in textureDirectory.Files) {
          textureFilesByName[textureFile.Name.ToLower()] = textureFile;
        }
      }

      /*new MeshCsvWriter().WriteToFile(
          glo, new FinFile(Path.Join(outputDirectory.FullName, "mesh.csv")));
      new FaceCsvWriter().WriteToFile(
          glo, new FinFile(Path.Join(outputDirectory.FullName, "face.csv")));
      new VertexCsvWriter().WriteToFile(
          glo, new FinFile(Path.Join(outputDirectory.FullName, "vertex.csv")));*/

      var finModel = new ModelImpl();
      var finSkin = finModel.Skin;

      var finRootBone = finModel.Skeleton.Root;

      var finTextureMap = new LazyDictionary<string, ITexture?>(
          textureFilename => {
            if (!textureFilesByName.TryGetValue(textureFilename.ToLower(),
                                                out var textureFile)) {
              return null;
            }

            using var rawTextureImage =
                (Bitmap.FromFile(textureFile.FullName) as Bitmap)!;

            var textureImageWithAlpha =
                GloModelLoader
                    .AddTransparencyToGloImage_(rawTextureImage);

            var finTexture = finModel.MaterialManager.CreateTexture(
                textureImageWithAlpha);
            finTexture.Name = textureFilename;

            if (this.mirrorTextures_.Contains(textureFilename)) {
              finTexture.WrapModeU = WrapMode.MIRROR_REPEAT;
              finTexture.WrapModeV = WrapMode.MIRROR_REPEAT;
            } else {
              finTexture.WrapModeU = WrapMode.REPEAT;
              finTexture.WrapModeV = WrapMode.REPEAT;
            }

            return finTexture;
          });
      var withCullingMap =
          new LazyDictionary<string, IMaterial>(textureFilename => {
            var finTexture = finTextureMap[textureFilename];
            if (finTexture == null) {
              return finModel.MaterialManager.AddStandardMaterial();
            }
            return finModel.MaterialManager.AddTextureMaterial(finTexture);
          });
      var withoutCullingMap = new LazyDictionary<string, IMaterial>(
          textureFilename => {
            var finTexture = finTextureMap[textureFilename];
            IMaterial finMaterial = finTexture == null
                                        ? finModel.MaterialManager
                                                  .AddStandardMaterial()
                                        : finModel.MaterialManager
                                                  .AddTextureMaterial(
                                                      finTexture);
            finMaterial.CullingMode = CullingMode.SHOW_BOTH;
            return finMaterial;
          });

      var firstMeshMap = new Dictionary<string, GloMesh>();

      // TODO: Consider separating these out as separate models
      foreach (var gloObject in glo.Objects) {
        var finObjectRootBone = finRootBone.AddRoot(0, 0, 0);
        var meshQueue = new Queue<(GloMesh, IBone)>();
        foreach (var topLevelGloMesh in gloObject.Meshes) {
          meshQueue.Enqueue((topLevelGloMesh, finObjectRootBone));
        }

        IAnimation? finAnimation = null;
        if (gloObject.AnimSegs.Length > 0) {
          finAnimation = finModel.AnimationManager.AddAnimation();
          finAnimation.FrameRate = fps;
        }

        while (meshQueue.Count > 0) {
          var (gloMesh, parentFinBone) = meshQueue.Dequeue();

          var name = new string(gloMesh.Name).Replace("\0", "");

          GloMesh idealMesh;
          if (!firstMeshMap.TryGetValue(name, out idealMesh)) {
            firstMeshMap[name] = idealMesh = gloMesh;
          }

          var position = gloMesh.MoveKeys[0].Xyz;

          var rotation = gloMesh.RotateKeys[0];
          var quaternion =
              new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
          var xyzRadians = QuaternionUtil.ToEulerRadians(quaternion);

          var scale = gloMesh.ScaleKeys[0].Scale;

          var finBone = parentFinBone
                        .AddChild(position.X, position.Y, position.Z)
                        .SetLocalRotationRadians(
                            xyzRadians.X, xyzRadians.Y, xyzRadians.Z)
                        .SetLocalScale(scale.X, scale.Y, scale.Z);
          finBone.Name = name + "_bone";

          var child = gloMesh.Pointers.Child;
          if (child != null) {
            meshQueue.Enqueue((child, finBone));
          }

          var next = gloMesh.Pointers.Next;
          if (next != null) {
            meshQueue.Enqueue((next, parentFinBone));
          }

          if (finAnimation != null) {
            var finBoneTracks = finAnimation.AddBoneTracks(finBone);
            foreach (var moveKey in gloMesh.MoveKeys) {
              finAnimation.FrameCount =
                  Math.Max(finAnimation.FrameCount, (int) moveKey.Time + 1);

              var moveValue = moveKey.Xyz;
              finBoneTracks.Positions.Set((int) moveKey.Time, 0, moveValue.X);
              finBoneTracks.Positions.Set((int) moveKey.Time, 1, moveValue.Y);
              finBoneTracks.Positions.Set((int) moveKey.Time, 2, moveValue.Z);
            }
            foreach (var rotateKey in gloMesh.RotateKeys) {
              finAnimation.FrameCount =
                  Math.Max(finAnimation.FrameCount, (int) rotateKey.Time + 1);

              var quaternionKey =
                  new Quaternion(rotateKey.X, rotateKey.Y, rotateKey.Z,
                                 rotateKey.W);
              var xyzRadiansKey = QuaternionUtil.ToEulerRadians(quaternionKey);

              finBoneTracks.Rotations.Set((int) rotateKey.Time, 0,
                                          xyzRadiansKey.X);
              finBoneTracks.Rotations.Set((int) rotateKey.Time, 1,
                                          xyzRadiansKey.Y);
              finBoneTracks.Rotations.Set((int) rotateKey.Time, 2,
                                          xyzRadiansKey.Z);
            }
            foreach (var scaleKey in gloMesh.ScaleKeys) {
              finAnimation.FrameCount =
                  Math.Max(finAnimation.FrameCount, (int) scaleKey.Time + 1);

              var scaleValue = scaleKey.Scale;
              finBoneTracks.Scales.Set((int) scaleKey.Time, 0, scaleValue.X);
              finBoneTracks.Scales.Set((int) scaleKey.Time, 1, scaleValue.Y);
              finBoneTracks.Scales.Set((int) scaleKey.Time, 2, scaleValue.Z);
            }
          }

          // Anything with these names are debug objects and can be ignored.
          if (this.hiddenNames_.Contains(name)) {
            continue;
          }

          var finMesh = finSkin.AddMesh();
          finMesh.Name = name;

          var gloVertices = idealMesh.Vertices;

          string previousTextureName = null;
          IMaterial? previousMaterial = null;

          foreach (var gloFace in idealMesh.Faces) {
            // TODO: What can we do if texture filename is empty?
            var textureFilename =
                new string(gloFace.TextureFilename).Replace("\0", "");

            var gloFaceColor = gloFace.Color;
            var finFaceColor = ColorImpl.FromRgbaBytes(
                gloFaceColor.R, gloFaceColor.G, gloFaceColor.B, gloFaceColor.A);

            var enableBackfaceCulling = (gloFace.Flags & 1 << 2) != 0;

            IMaterial? finMaterial;
            if (textureFilename == previousTextureName) {
              finMaterial = previousMaterial;
            } else {
              previousTextureName = textureFilename;
              finMaterial = enableBackfaceCulling
                                ? withCullingMap[textureFilename]
                                : withoutCullingMap[textureFilename];
              previousMaterial = finMaterial;
            }

            // Face flag:
            // 0: potentially some kind of repeat mode??

            var color = (gloFace.Flags & 1 << 6) != 0
                            ? ColorImpl.FromRgbaBytes(255, 0, 0, 255)
                            : ColorImpl.FromRgbaBytes(0, 255, 0, 255);

            var finFaceVertices = new IVertex[3];
            for (var v = 0; v < 3; ++v) {
              var gloVertexRef = gloFace.VertexRefs[v];
              var gloVertex = gloVertices[gloVertexRef.Index];

              var finVertex = finSkin
                              .AddVertex(gloVertex.X, gloVertex.Y, gloVertex.Z)
                              .SetUv(gloVertexRef.U, gloVertexRef.V);
              //.SetColor(color);
              finVertex.SetBoneWeights(finSkin.GetOrCreateBoneWeights(finBone));
              finFaceVertices[v] = finVertex;
            }

            // TODO: Merge triangles together
            var finTriangles = new (IVertex, IVertex, IVertex)[1];
            finTriangles[0] = (finFaceVertices[0], finFaceVertices[2],
                               finFaceVertices[1]);
            finMesh.AddTriangles(finTriangles).SetMaterial(finMaterial!);
          }
        }

        // TODO: Split out animations
      }

      return finModel;
    }

    private static Bitmap AddTransparencyToGloImage_(Bitmap rawImage) {
      var textureWidth = rawImage.Width;
      var textureHeight = rawImage.Height;

      var textureImageWithAlpha = new Bitmap(
          textureWidth, textureHeight, PixelFormat.Format32bppArgb);
      BitmapUtil.ProcessImage(
          rawImage, textureImageWithAlpha, ConvertGloPixel_);
      return textureImageWithAlpha;
    }

    private static void ConvertGloPixel_(byte inR,
                                         byte inG,
                                         byte inB,
                                         byte inA,
                                         out byte outR,
                                         out byte outG,
                                         out byte outB,
                                         out byte outA) {
      outR = inR;
      outG = inG;
      outB = inB;

      if (inR == 255 &&
          inG == 0 &&
          inB == 255) {
        outA = 0;
      } else {
        outA = inA;
      }
    }
  }
}