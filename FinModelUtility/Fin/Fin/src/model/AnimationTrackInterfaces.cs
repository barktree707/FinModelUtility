﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;

using fin.data;
using fin.math.interpolation;

namespace fin.model {
  public readonly struct ValueAndTangents<T> {
    public T Value { get; }
    public float? IncomingTangent { get; }
    public float? OutgoingTangent { get; }

    public ValueAndTangents(T value,
                            float? incomingTangent,
                            float? outgoingTangent) {
      this.Value = value;
      this.IncomingTangent = incomingTangent;
      this.OutgoingTangent = outgoingTangent;
    }
  }


  public interface ITrack : IAnimationData {
    bool IsDefined { get; }
  }

  public interface IReadOnlyInterpolatedTrack<TInterpolated> : ITrack {
    bool TryGetInterpolatedFrame(
        float frame,
        out TInterpolated interpolatedValue,
        bool useLoopingInterpolation = false
    );
  }


  public interface IImplTrack<TValue> : ITrack {
    IReadOnlyList<Keyframe<ValueAndTangents<TValue>>> Keyframes { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Set(int frame, TValue value)
      => this.Set(frame, value, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Set(int frame, TValue value, float? tangent)
      => this.Set(frame, value, tangent, tangent);

    void Set(
        int frame,
        TValue value,
        float? incomingTangent,
        float? outgoingTangent);

    bool TryGetInterpolationData(
        float frame,
        out (float frame, TValue value, float? tangent)? fromData,
        out (float frame, TValue value, float? tangent)? toData,
        bool useLoopingInterpolation = false
    );

    Keyframe<ValueAndTangents<TValue>>? GetKeyframe(int frame);
  }


  public interface IInputOutputTrack<T, TInterpolator>
      : IInputOutputTrack<T, T, TInterpolator>
      where TInterpolator : IInterpolator<T> { }

  public interface IInputOutputTrack<TValue, TInterpolated, TInterpolator>
      : IReadOnlyInterpolatedTrack<TInterpolated>,
        IImplTrack<TValue>
      where TInterpolator : IInterpolator<TValue, TInterpolated> {
    TInterpolator Interpolator { get; }

    void Set(IInputOutputTrack<TValue, TInterpolated, TInterpolator> other) { }

    // TODO: Allow setting tangent(s) at each frame.
    // TODO: Allow setting easing at each frame.
    // TODO: Split getting into exactly at frame and interpolated at frame.
    // TODO: Allow getting at fractional frames.
  }

  // TODO: Rethink this, this is getting way too complicated.
  public interface IAxesTrack<TAxis, TInterpolated> : ITrack {
    void Set(int frame, int axis, TAxis value)
      => this.Set(frame, axis, value, null);

    void Set(int frame, int axis, TAxis value, float? optionalTangent)
      => this.Set(frame, axis, value, optionalTangent, optionalTangent);

    void Set(
        int frame,
        int axis,
        TAxis value,
        float? optionalIncomingTangent,
        float? optionalOutgoingTangent);

    TInterpolated GetInterpolatedFrame(
        float frame,
        bool useLoopingInterpolation = false
    );
  }
}