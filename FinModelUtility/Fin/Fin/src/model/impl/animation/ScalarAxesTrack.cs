﻿using fin.data;
using fin.math.interpolation;

using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace fin.model.impl {
  public partial class ModelImpl<TVertex> {
    public abstract class BScalarAxesTrack<TAxes, TAxis, TInterpolator> 
        : BScalarAxesTrack<TAxes, TAxis, TAxes, TInterpolator>
        where TInterpolator : IInterpolator<TAxis> {
      public BScalarAxesTrack(
          int axisCount,
          ReadOnlySpan<int> initialKeyframeCapacitiesPerAxis,
          TInterpolator interpolator)
          : base(axisCount,
                 initialKeyframeCapacitiesPerAxis,
                 interpolator) { }
    }

    public abstract class BScalarAxesTrack<TAxes, TAxis, TInterpolated, TInterpolator>
        : IAxesTrack<TAxis, TInterpolated>
        where TInterpolator : IInterpolator<TAxis> {
      protected InputOutputTrackImpl<TAxis, TInterpolator>[] axisTracks;

      public BScalarAxesTrack(
          int axisCount,
          ReadOnlySpan<int> initialKeyframeCapacitiesPerAxis,
          TInterpolator interpolator) {
        this.axisTracks = new InputOutputTrackImpl<TAxis, TInterpolator>[axisCount];
        for (var i = 0; i < axisCount; ++i) {
          this.axisTracks[i] =
              new InputOutputTrackImpl<TAxis, TInterpolator>(initialKeyframeCapacitiesPerAxis[i],
                                   interpolator);
        }
      }

      public bool IsDefined => this.axisTracks.Any(axis => axis.IsDefined);

      public int FrameCount {
        set {
          foreach (var axis in this.axisTracks) {
            axis.FrameCount = value;
          }
        }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public void Set(
          int frame,
          int axis,
          TAxis value,
          float? optionalIncomingTangent,
          float? optionalOutgoingTangent)
        => this.axisTracks[axis]
               .Set(frame,
                    value,
                    optionalIncomingTangent,
                    optionalOutgoingTangent);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public Keyframe<ValueAndTangents<TAxis>>? GetKeyframe(int keyframe, int axis)
        => this.axisTracks[axis].GetKeyframe(keyframe);

      public abstract TInterpolated GetInterpolatedFrame(
          float frame,
          TAxis[] defaultValue,
          bool useLoopingInterpolation = false
      );
    }
  }
}