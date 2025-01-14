﻿using System.Runtime.CompilerServices;

namespace fin.math.interpolation {
  public readonly struct FloatInterpolator : IInterpolator<float> {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Interpolate(float fromValue, float toValue, float progress) 
      => (1 - progress) * fromValue + progress * toValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Interpolate(float fromTime,
                             float fromValue,
                             float fromTangent,
                             float toTime,
                             float toValue,
                             float toTangent,
                             float time)
      => InterpolatorWithTangents.InterpolateFloats(
          fromTime,
          fromValue,
          fromTangent,
          toTime,
          toValue,
          toTangent,
          time);
  }
}