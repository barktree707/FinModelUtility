﻿using System;

using Crayon;

using fin.math;
using fin.model;
using fin.model.impl;

namespace fin.util.color {
  public static class ColorUtil {
    public static byte ExtractScaled(ushort col, int offset, int count) {
      var maxPossible = Math.Pow(2, count);
      var factor = 255 / maxPossible;
      return ColorUtil.ExtractScaled(col, offset, count, factor);
    }

    public static byte ExtractScaled(
        ushort col,
        int offset,
        int count,
        double factor) {
      var extracted = BitLogic.ExtractFromRight(col, offset, count) * 1d;
      return (byte) Math.Round(extracted * factor);
    }

    public static void SplitRgb565(
        ushort color,
        out byte r,
        out byte g,
        out byte b) {
      r = ColorUtil.ExtractScaled(color, 11, 5);
      g = ColorUtil.ExtractScaled(color, 5, 6);
      b = ColorUtil.ExtractScaled(color, 0, 5);
    }

    public static IColor ParseRgb565(ushort color) {
      ColorUtil.SplitRgb565(color, out var r, out var g, out var b);
      return ColorImpl.FromRgbaBytes(r, g, b, 255);
    }

    public static IColor Interpolate(IColor from, IColor to, double amt)
      => ColorImpl.FromRgbaBytes(
          (byte) Math.Round(from.Rb * (1 - amt) + to.Rb * amt),
          (byte) Math.Round(from.Gb * (1 - amt) + to.Gb * amt),
          (byte) Math.Round(from.Bb * (1 - amt) + to.Bb * amt),
          (byte) Math.Round(from.Ab * (1 - amt) + to.Ab * amt));
  }
}