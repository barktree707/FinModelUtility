﻿using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

using fin.data;
using fin.math.floats;
using fin.math.interpolation;
using fin.math.rotations;

namespace fin.model.impl {
  public partial class ModelImpl<TVertex> {
    public class EulerRadiansRotationTrack3dImpl
        : IEulerRadiansRotationTrack3d {
      private readonly IBone bone_;

      private readonly IInputOutputTrack<float, RadianInterpolator>[]
          axisTracks_;

      public EulerRadiansRotationTrack3dImpl(
          IAnimation animation,
          IBone bone,
          ReadOnlySpan<int> initialCapacityPerAxis) {
        this.Animation = animation;
        this.bone_ = bone;
        this.axisTracks_ =
            new InputOutputTrackImpl<float, RadianInterpolator>[3];
        for (var i = 0; i < 3; ++i) {
          this.axisTracks_[i] =
              new InputOutputTrackImpl<float, RadianInterpolator>(
                  animation,
                  initialCapacityPerAxis[i],
                  new RadianInterpolator());
        }
      }

      public IAnimation Animation { get; }
      public bool IsDefined => this.axisTracks_.Any(axis => axis.IsDefined);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public void Set(
          int frame,
          int axis,
          float incomingRadians,
          float outgoingRadians,
          float? optionalIncomingTangent,
          float? optionalOutgoingTangent)
        => this.axisTracks_[axis]
               .SetKeyframe(frame,
                    incomingRadians,
                    outgoingRadians,
                    optionalIncomingTangent,
                    optionalOutgoingTangent);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public Keyframe<ValueAndTangents<float>>?[] GetAxisListAtKeyframe(
          int keyframe)
        => this.axisTracks_.Select(axis => axis.GetKeyframe(keyframe))
               .ToArray();

      public bool TryGetInterpolatedFrame(float frame,
                                          out Quaternion interpolatedValue,
                                          bool useLoopingInterpolation =
                                              false) {
        interpolatedValue =
            GetInterpolatedFrame(frame, useLoopingInterpolation);
        return true;
      }

      public Quaternion GetInterpolatedFrame(
          float frame,
          bool useLoopingInterpolation = false) {
        var xTrack = this.axisTracks_[0];
        var yTrack = this.axisTracks_[1];
        var zTrack = this.axisTracks_[2];

        var localRotation = this.bone_.LocalRotation;
        var defaultX = localRotation?.XRadians ?? 0;
        var defaultY = localRotation?.YRadians ?? 0;
        var defaultZ = localRotation?.ZRadians ?? 0;

        xTrack.TryGetInterpolationData(
            frame,
            out var fromXFrame,
            out var toXFrame,
            useLoopingInterpolation);
        yTrack.TryGetInterpolationData(
            frame,
            out var fromYFrame,
            out var toYFrame,
            useLoopingInterpolation);
        zTrack.TryGetInterpolationData(
            frame,
            out var fromZFrame,
            out var toZFrame,
            useLoopingInterpolation);

        Span<(float frame, float value, float? tangent)?> fromsAndTos =
            stackalloc (float frame, float value, float? tangent)?[6];
        fromsAndTos[0] = fromXFrame;
        fromsAndTos[1] = fromYFrame;
        fromsAndTos[2] = fromZFrame;
        fromsAndTos[3] = toXFrame;
        fromsAndTos[4] = toYFrame;
        fromsAndTos[5] = toZFrame;

        Span<bool> areAxesStatic = stackalloc bool[3];
        AreAxesStatic_(fromsAndTos, areAxesStatic);

        if (this.bone_.Name == "base") {
          ;
        }

        if (!CanInterpolateWithQuaternions_(
                fromsAndTos,
                areAxesStatic)) {
          if (!xTrack.TryGetInterpolatedFrame(frame,
                                              out var xRadians,
                                              useLoopingInterpolation)) {
            xRadians = defaultX;
          }

          if (!yTrack.TryGetInterpolatedFrame(frame,
                                              out var yRadians,
                                              useLoopingInterpolation)) {
            yRadians = defaultY;
          }

          if (!zTrack.TryGetInterpolatedFrame(frame,
                                              out var zRadians,
                                              useLoopingInterpolation)) {
            zRadians = defaultZ;
          }

          return ConvertRadiansToQuaternionImpl(xRadians, yRadians, zRadians);
        }

        if (GetFromAndToFrameIndex_(fromsAndTos,
                                    areAxesStatic,
                                    out var fromFrame,
                                    out var toFrame)) {
          if (toFrame < fromFrame) {
            toFrame += this.Animation.FrameCount;
          }

          var frameDelta = (frame - fromFrame) / (toFrame - fromFrame);

          var q1 = ConvertRadiansToQuaternionImpl(
              fromXFrame?.value ?? defaultX,
              fromYFrame?.value ?? defaultY,
              fromZFrame?.value ?? defaultZ);
          var q2 = ConvertRadiansToQuaternionImpl(
              toXFrame?.value ?? defaultX,
              toYFrame?.value ?? defaultY,
              toZFrame?.value ?? defaultZ);

          if (Quaternion.Dot(q1, q2) < 0) {
            q2 = -q2;
          }

          var interp = Quaternion.Slerp(q1, q2, frameDelta);
          return Quaternion.Normalize(interp);
        }

        return Quaternion.Normalize(ConvertRadiansToQuaternionImpl(
                                        fromXFrame?.value ?? defaultX,
                                        fromYFrame?.value ?? defaultY,
                                        fromZFrame?.value ?? defaultZ));
      }

      private static void AreAxesStatic_(
          ReadOnlySpan<(float frame, float value, float? tangent)?> fromsAndTos,
          Span<bool> areAxesStatic) {
        for (var i = 0; i < 3; ++i) {
          var fromOrNull = fromsAndTos[i];
          var toOrNull = fromsAndTos[3 + i];

          if (fromOrNull == null && toOrNull == null) {
            areAxesStatic[i] = true;
          } else if (fromOrNull != null && toOrNull != null) {
            var from = fromOrNull.Value;
            var to = toOrNull.Value;

            if (from.value.IsRoughly(to.value)) {
              if (!SUPPORTS_TANGENTS_IN_QUATERNIONS) {
                areAxesStatic[i] = true;
              } else {
                var fromTangentOrNull = from.tangent;
                var toTangentOrNull = from.tangent;
                if (fromTangentOrNull == null && toTangentOrNull == null) {
                  areAxesStatic[i] = true;
                } else if (fromTangentOrNull != null &&
                           toTangentOrNull != null) {
                  var fromTangent = fromTangentOrNull.Value;
                  var toTangent = toTangentOrNull.Value;
                  areAxesStatic[i] = fromTangent.IsRoughly(toTangent);
                }
              }
            }
          }
        }
      }

      private static bool GetFromAndToFrameIndex_(
          ReadOnlySpan<(float frame, float value, float? tangent)?> fromsAndTos,
          ReadOnlySpan<bool> areAxesStatic,
          out float fromFrameIndex,
          out float toFrameIndex) {
        for (var i = 0; i < 3; ++i) {
          if (!areAxesStatic[i]) {
            fromFrameIndex = fromsAndTos[i].Value.frame;
            toFrameIndex = fromsAndTos[3 + i].Value.frame;
            return true;
          }
        }

        fromFrameIndex = default;
        toFrameIndex = default;
        return false;
      }

      private static bool CanInterpolateWithQuaternions_(
          ReadOnlySpan<(float frame, float value, float? tangent)?> fromsAndTos,
          ReadOnlySpan<bool> areAxesStatic) {
        for (var i = 0; i < 6; ++i) {
          if (areAxesStatic[i % 3]) {
            continue;
          }

          if (fromsAndTos[i] == null) {
            return false;
          }

          if (!SUPPORTS_TANGENTS_IN_QUATERNIONS) {
            if ((fromsAndTos[i].Value.tangent ?? 0) != 0) {
              return false;
            }
          }
        }

        for (var i = 0; i < 3; ++i) {
          if (areAxesStatic[i]) {
            continue;
          }

          var from = fromsAndTos[i].Value;
          for (var oi = i + 1; oi < 3; ++oi) {
            if (areAxesStatic[oi]) {
              continue;
            }

            var to = fromsAndTos[oi].Value;
            if (!from.frame.IsRoughly(to.frame)) {
              return false;
            }

            if (SUPPORTS_TANGENTS_IN_QUATERNIONS) {
              var fromTangentOrNull = from.tangent;
              var toTangentOrNull = to.tangent;
              if ((fromTangentOrNull == null) != (toTangentOrNull == null)) {
                return false;
              }

              if (fromTangentOrNull != null && toTangentOrNull != null &&
                  !fromTangentOrNull.Value.IsRoughly(toTangentOrNull.Value)) {
                return false;
              }
            }
          }
        }

        return true;
      }

      public IEulerRadiansRotationTrack3d.ConvertRadiansToQuaternion
          ConvertRadiansToQuaternionImpl { get; set; } =
        QuaternionUtil.CreateZyx;


      // TODO: Add support for tangents in quaternions
      private const bool SUPPORTS_TANGENTS_IN_QUATERNIONS = false;
    }
  }
}