﻿using System.Collections.Generic;

using fin.math;
using fin.model;
using fin.model.impl;
using fin.util.asserts;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using mod.util;

namespace mod.gcn.image {
  public class Rgb565 : BImageFormat {
    public Rgb565(IList<byte> rawData, int imageWidth, int imageHeight) : base(
        rawData,
        imageWidth,
        imageHeight,
        4,
        4,
        16) {}

    protected override IColor[] DecodeBlock(IList<byte> block, int position) {
      var bl = new IColor[4 * 4];
      var s = new VectorReader(block, position, Endianness.Big);

      for (var j = 0; j < 4; ++j) {
        for (var i = 0; i < 4; ++i) {
          var pix = s.ReadU16();
          bl[j * 4 + i] = ColorUtil.ParseRgb565(pix);
        }
      }

      return bl;
    }
  }
}