﻿using System;
using System.Runtime.CompilerServices;

using fin.math.floats;

namespace fin.math.rotations {
  public static class RadiansUtil {
    private const float PI = MathF.PI;
    private const float PI2 = 2 * RadiansUtil.PI;
    private const float PI3 = 3 * RadiansUtil.PI;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CalculateRadiansTowards(float from, float to) {
      var difference = to - from;
      if (Math.Abs(difference).IsRoughly(RadiansUtil.PI)) {
        return difference;
      }

      return (((difference % RadiansUtil.PI2) + RadiansUtil.PI3) % RadiansUtil.PI2) - RadiansUtil.PI;
    }
  }
}