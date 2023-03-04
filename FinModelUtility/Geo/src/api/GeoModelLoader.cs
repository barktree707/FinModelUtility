﻿using fin.data;
using fin.data.queue;
using fin.io;
using fin.math;
using fin.math.matrix;
using fin.model;
using fin.model.impl;
using fin.schema.matrix;

using geo.schema.rcb;

namespace geo.api {
  public class GeoModelFileBundle : IModelFileBundle {
    public IFileHierarchyFile? MainFile => RcbFile;
    public required IFileHierarchyFile? RcbFile { get; init; }

    public IReadOnlyList<Tg4ImageFileBundle>? Tg4ImageFileBundles { get; init; }
  }

  public class GeoModelLoader : IModelLoader<GeoModelFileBundle> {
    public IModel LoadModel(GeoModelFileBundle modelFileBundle) {
      var finModel = new ModelImpl();

      // Builds textures
      var textureBundles = modelFileBundle.Tg4ImageFileBundles;
      if (textureBundles != null) {
        var tg4ImageLoader = new Tg4ImageLoader();

        foreach (var textureBundle in textureBundles) {
          var image = tg4ImageLoader.LoadImage(textureBundle);
          var finTexture = finModel.MaterialManager.CreateTexture(image);
          finTexture.Name = textureBundle.Tg4hFile.NameWithoutExtension;
        }
      }

      // Builds skeletons
      var rcbFile = modelFileBundle.RcbFile;
      var rcb = rcbFile.Impl.ReadNew<Rcb>();
      foreach (var rcbSkeleton in rcb.Skeletons) {
        var finRoot = finModel.Skeleton.Root.AddRoot(0, 0, 0);
        finRoot.Name = rcbSkeleton.SkeletonName;

        var rootChildren = new List<int>();
        var childIndices = new ListDictionary<int, int>();
        for (var i = 0; i < rcbSkeleton.BoneParentIdMap.Count; ++i) {
          var parent = rcbSkeleton.BoneParentIdMap[i];
          if (parent == -1) {
            rootChildren.Add(i);
          } else {
            childIndices.Add(parent, i);
          }
        }

        var boneQueue =
            new FinTuple2Queue<IBone, int>(
                rootChildren.Select(id => (finRoot, id)));
        while (boneQueue.TryDequeue(out var finParentBone, out var id)) {
          var parentId = rcbSkeleton.BoneParentIdMap[id];
          var rcbBone = rcbSkeleton.Bones[id];

          var currentMatrix = GetMatrixFromBone(rcbBone.Matrix);
          if (parentId != -1) {
            var rcbParentBone = rcbSkeleton.Bones[parentId];
            var parentMatrix = GetMatrixFromBone(rcbParentBone.Matrix);
            currentMatrix = parentMatrix.InvertInPlace().CloneAndMultiply(currentMatrix);
          }

          currentMatrix.Decompose(out var translation, out var rotation, out var scale);
          var eulerRadians = QuaternionUtil.ToEulerRadians(rotation);

          var finBone =
              finParentBone
                  .AddChild(translation.X, translation.Y, translation.Z)
                  .SetLocalRotationRadians(
                      eulerRadians.X,
                      eulerRadians.Y,
                      eulerRadians.Z)
                  .SetLocalScale(scale.X, scale.Y, scale.Z);

          if (childIndices.TryGetList(id, out var currentChildren)) {
            boneQueue.Enqueue(
                currentChildren!.Select(childId => (finBone, childId)));
          }
        }
      }

      return finModel;
    }

    public IFinMatrix4x4 GetMatrixFromBone(Matrix4x4f matrix)
      => new FinMatrix4x4(matrix.Values).InvertInPlace();
  }
}