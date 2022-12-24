﻿using System;
using System.Collections.Generic;
using System.Numerics;
using fin.data;
using fin.math.matrix;
using fin.model;
using fin.model.impl;
using fin.ui;
using fin.util.optional;


namespace fin.math {
  public interface IBoneTransformManager {
    (IBoneTransformManager, IBone)? Parent { get; }

    void Clear();

    IDictionary<IBone, int> CalculateMatrices(
        IBone rootBone,
        IReadOnlyList<IBoneWeights> boneWeightsList,
        (IAnimation, float)? animationAndFrame,
        bool useLoopingInterpolation = false
    );

    public IReadOnlyFinMatrix4x4 GetLocalMatrix(IBone bone);

    public IReadOnlyFinMatrix4x4 GetWorldMatrix(IBone bone);

    public IReadOnlyFinMatrix4x4? GetTransformMatrix(IVertex vertex,
      bool forcePreproject = false);

    void ProjectVertex(
        IVertex vertex,
        IPosition outPosition,
        INormal? outNormal = null,
        bool forcePreproject = false);

    void ProjectVertex(IBone bone,
                       ref float x,
                       ref float y,
                       ref float z);

    void ProjectNormal(IBone bone,
                       ref float x,
                       ref float y,
                       ref float z);
  }

  public class BoneTransformManager : IBoneTransformManager {
    // TODO: This is going to be slow, can we put this somewhere else for O(1) access?
    private readonly IndexableDictionary<IBone, IFinMatrix4x4>
        bonesToLocalMatrices_ = new();

    private readonly IndexableDictionary<IBone, IFinMatrix4x4>
        bonesToWorldMatrices_ = new();

    private readonly IndexableDictionary<IBone, IReadOnlyFinMatrix4x4>
        bonesToInverseBindMatrices_ = new();

    private readonly IndexableDictionary<IBoneWeights, IFinMatrix4x4>
        boneWeightsToWorldMatrices_ = new();

    public (IBoneTransformManager, IBone)? Parent { get; }

    public BoneTransformManager((IBoneTransformManager, IBone)? parent = null) {
      this.Parent = parent;
    }

    public void Clear() {
      this.bonesToLocalMatrices_.Clear();
      this.bonesToWorldMatrices_.Clear();
      this.bonesToInverseBindMatrices_.Clear();
      this.boneWeightsToWorldMatrices_.Clear();
    }

    public IDictionary<IBone, int> CalculateMatrices(
        IBone rootBone,
        IReadOnlyList<IBoneWeights> boneWeightsList,
        (IAnimation, float)? animationAndFrame,
        bool useLoopingInterpolation = false
    ) {
      var isFirstPass = animationAndFrame == null;

      var animation = animationAndFrame?.Item1;
      var frame = animationAndFrame?.Item2;

      var translationBuffer = new ModelImpl.PositionImpl();
      var scaleBuffer = new ModelImpl.ScaleImpl();

      IReadOnlyFinMatrix4x4 managerMatrix;
      if (this.Parent == null) {
        managerMatrix = FinMatrix4x4.IDENTITY;
      } else {
        var (parentManager, parentBone) = this.Parent.Value;
        managerMatrix = parentManager.GetWorldMatrix(parentBone);
      }

      // TODO: Cache this directly on the bone itself instead.
      var bonesToIndex = new Dictionary<IBone, int>();
      var boneIndex = -1;

      var boneQueue = new Queue<(IBone, IReadOnlyFinMatrix4x4)>();
      boneQueue.Enqueue((rootBone, managerMatrix));
      while (boneQueue.Count > 0) {
        var (bone, parentMatrix) = boneQueue.Dequeue();

        if (!this.bonesToLocalMatrices_.TryGetValue(bone, out var localMatrix)) {
          this.bonesToLocalMatrices_[bone] = localMatrix = new FinMatrix4x4();
        }
        if (!this.bonesToWorldMatrices_.TryGetValue(bone, out var matrix)) {
          this.bonesToWorldMatrices_[bone] = matrix = new FinMatrix4x4();
        }

        localMatrix.SetIdentity();
        matrix.CopyFrom(parentMatrix);

        // The root pose of the bone.
        var boneLocalPosition = bone.LocalPosition;
        var boneLocalRotation = bone.LocalRotation != null
                                    ? QuaternionUtil.Create(bone.LocalRotation)
                                    : (Quaternion?)null;
        var boneLocalScale = bone.LocalScale;

        IPosition? animationLocalPosition = null;
        Quaternion? animationLocalRotation = null;
        IScale? animationLocalScale = null;

        // The pose of the animation, if available.
        IBoneTracks? boneTracks = null;
        animation?.BoneTracks.TryGetValue(bone, out boneTracks);
        if (boneTracks != null) {
          // Need to pass in default pose of the bone to fill in for any axes that may be undefined.
          var defaultPosition = Optional.Of(new[] {
              boneLocalPosition.X, boneLocalPosition.Y, boneLocalPosition.Z,
          });
          var defaultRotation = Optional.Of(new[] {
              bone.LocalRotation?.XRadians ?? 0,
              bone.LocalRotation?.YRadians ?? 0,
              bone.LocalRotation?.ZRadians ?? 0,
          });
          var defaultScale = Optional.Of(new[] {
              boneLocalScale?.X ?? 0, boneLocalScale?.Y ?? 0,
              boneLocalScale?.Z ?? 0,
          });

          // Only gets the values from the animation if the frame is at least partially defined.
          animationLocalPosition =
              boneTracks?.Positions.IsDefined ?? false
                  ? boneTracks?.Positions.GetInterpolatedFrame(
                      (float)frame, defaultPosition, useLoopingInterpolation)
                  : null;
          animationLocalRotation =
              boneTracks?.Rotations.IsDefined ?? false
                  ? boneTracks?.Rotations.GetInterpolatedFrame(
                      (float)frame, defaultRotation, useLoopingInterpolation)
                  : null;
          animationLocalScale =
              boneTracks?.Scales.IsDefined ?? false
                  ? boneTracks?.Scales.GetInterpolatedFrame(
                      (float)frame, defaultScale, useLoopingInterpolation)
                  : null;
        }

        // Uses the animation pose instead of the root pose when available.
        var localPosition = animationLocalPosition ?? boneLocalPosition;
        var localRotation = animationLocalRotation ?? boneLocalRotation;
        var localScale = animationLocalScale ?? boneLocalScale;

        if (!bone.IgnoreParentScale && !bone.FaceTowardsCamera) {
          MatrixTransformUtil.FromTrs(localPosition,
                                      localRotation,
                                      localScale, 
                                      localMatrix);
          matrix.MultiplyInPlace(localMatrix);
        } else {
          // Applies translation first, so it's affected by parent rotation/scale.
          var localTranslationMatrix =
              MatrixTransformUtil.FromTranslation(localPosition);
          matrix.MultiplyInPlace(localTranslationMatrix);

          // Extracts translation/rotation/scale.
          matrix.CopyTranslationInto(translationBuffer);
          Quaternion rotationBuffer;
          if (bone.FaceTowardsCamera) {
            var camera = Camera.Instance;
            var angle = camera.Yaw / 180f * MathF.PI;
            var rotateYaw =
                Quaternion.CreateFromYawPitchRoll(angle, 0, 0);

            rotationBuffer = rotateYaw * bone.FaceTowardsCameraAdjustment;
          } else {
            matrix.CopyRotationInto(out rotationBuffer);
          }
          if (bone.IgnoreParentScale) {
            scaleBuffer.X = scaleBuffer.Y = scaleBuffer.Z = 1;
          } else {
            matrix.CopyScaleInto(scaleBuffer);
          }

          // Creates child matrix.
          MatrixTransformUtil.FromTrs(localPosition,
                                      localRotation,
                                      localScale,
                                      localMatrix);

          // Gets final matrix.
          MatrixTransformUtil.FromTrs(
              translationBuffer,
              rotationBuffer,
              scaleBuffer,
              matrix);
          matrix.MultiplyInPlace(MatrixTransformUtil.FromTrs(null,
                                   localRotation,
                                   localScale));
        }

        if (isFirstPass) {
          this.bonesToInverseBindMatrices_[bone] = matrix.CloneAndInvert();
        }
        bonesToIndex[bone] = boneIndex++;

        foreach (var child in bone.Children) {
          // TODO: Use a pool of matrices to prevent unneeded instantiations.
          boneQueue.Enqueue((child, matrix.Clone()));
        }
      }

      foreach (var boneWeights in boneWeightsList) {
        if (!this.boneWeightsToWorldMatrices_.TryGetValue(
                boneWeights, out var boneWeightMatrix)) {
          this.boneWeightsToWorldMatrices_[boneWeights] =
              boneWeightMatrix = new FinMatrix4x4();
        }
        boneWeightMatrix.SetZero();

        var weights = boneWeights.Weights;
        if (weights.Count == 1) {
          var weight = weights[0];
          var bone = weight.Bone;

          var skinToBoneMatrix = weight.SkinToBone ??
                                 this.bonesToInverseBindMatrices_[bone];
          var boneMatrix = this.GetWorldMatrix(bone);

          boneMatrix.MultiplyIntoBuffer(skinToBoneMatrix, boneWeightMatrix);
        } else {
          foreach (var weight in weights) {
            var bone = weight.Bone;

            var skinToBoneMatrix = weight.SkinToBone ??
                                   this.bonesToInverseBindMatrices_[bone];
            var boneMatrix = this.GetWorldMatrix(bone);

            boneMatrix.MultiplyIntoBuffer(skinToBoneMatrix,
                                          this.tempSkinToWorldMatrix_);
            this.tempSkinToWorldMatrix_.MultiplyInPlace(weight.Weight);

            boneWeightMatrix.AddInPlace(this.tempSkinToWorldMatrix_);
          }
        }
      }

      return bonesToIndex;
    }

    private readonly FinMatrix4x4 tempSkinToWorldMatrix_ = new();

    public IReadOnlyFinMatrix4x4 GetLocalMatrix(IBone bone)
      => this.bonesToLocalMatrices_[bone];

    public IReadOnlyFinMatrix4x4 GetWorldMatrix(IBone bone)
      => this.bonesToWorldMatrices_[bone];

    public IReadOnlyFinMatrix4x4? GetTransformMatrix(IVertex vertex,
      bool forcePreproject = false) {
      var boneWeights = vertex.BoneWeights;
      var weights = vertex.BoneWeights?.Weights;
      var preproject =
          (boneWeights?.PreprojectMode != PreprojectMode.NONE ||
           forcePreproject) &&
          weights?.Count > 0;

      if (!preproject) {
        return null;
      }

      var transformMatrix = boneWeights.PreprojectMode switch {
        // If preproject mode is none, then the vertices are already in the same position as the bones.
        // To calculate the animation, we have to first "undo" the root pose via an inverted matrix. 
        PreprojectMode.NONE => this.boneWeightsToWorldMatrices_[
            vertex.BoneWeights!],
        // If preproject mode is bone, then we need to transform the vertex by one or more bones.
        PreprojectMode.BONE => this.boneWeightsToWorldMatrices_[
            vertex.BoneWeights!],
        // If preproject mode is root, then the vertex needs to be transformed relative to
        // some root bone.
        PreprojectMode.ROOT => this.GetWorldMatrix(weights[0].Bone.Root),
        _ => throw new ArgumentOutOfRangeException()
      };

      return transformMatrix;
    }

    public void ProjectVertex(
        IVertex vertex,
        IPosition outPosition,
        INormal? outNormal = null,
        bool forcePreproject = false) {
      var transformMatrix = this.GetTransformMatrix(vertex, forcePreproject);

      var localPosition = vertex.LocalPosition;
      var localNormal = vertex.LocalNormal;
      if (transformMatrix == null) {
        outPosition.X = localPosition.X;
        outPosition.Y = localPosition.Y;
        outPosition.Z = localPosition.Z;

        if (outNormal != null && localNormal != null) {
          outNormal.X = localNormal.X;
          outNormal.Y = localNormal.Y;
          outNormal.Z = localNormal.Z;
        }
        return;
      }

      float x = localPosition.X;
      float y = localPosition.Y;
      float z = localPosition.Z;
      GlMatrixUtil.ProjectVertex(transformMatrix,
                                 ref x,
                                 ref y,
                                 ref z);
      outPosition.X = x;
      outPosition.Y = y;
      outPosition.Z = z;

      if (outNormal != null && localNormal != null) {
        float nX = localNormal.X;
        float nY = localNormal.Y;
        float nZ = localNormal.Z;
        GlMatrixUtil.ProjectNormal(transformMatrix,
                                   ref nX,
                                   ref nY,
                                   ref nZ);

        outNormal.X = nX;
        outNormal.Y = nY;
        outNormal.Z = nZ;
      }
    }

    public void ProjectVertex(IBone bone,
                              ref float x,
                              ref float y,
                              ref float z) {
      GlMatrixUtil.ProjectVertex(
          this.GetWorldMatrix(bone),
          ref x, ref y, ref z);
    }

    public void ProjectNormal(IBone bone,
                              ref float x,
                              ref float y,
                              ref float z) {
      GlMatrixUtil.ProjectNormal(
          this.GetWorldMatrix(bone),
          ref x, ref y, ref z);
    }
  }
}