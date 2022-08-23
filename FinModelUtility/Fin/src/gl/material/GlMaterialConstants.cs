﻿using System.Drawing;

using fin.image;
using fin.util.image;


namespace fin.gl.material {
  public static class GlMaterialConstants {
    public static GlTexture NULL_WHITE_TEXTURE;
    public static GlTexture NULL_GRAY_TEXTURE;
    public static GlTexture NULL_BLACK_TEXTURE;

    static GlMaterialConstants() {
      NULL_WHITE_TEXTURE ??=
          new GlTexture(FinImage.Create1x1WithColor(Color.White));
      NULL_GRAY_TEXTURE ??=
          new GlTexture(FinImage.Create1x1WithColor(Color.Gray));
      NULL_BLACK_TEXTURE ??=
          new GlTexture(FinImage.Create1x1WithColor(Color.Black));
    }

    public static void DisposeIfNotCommon(GlTexture texture) {
      if (texture != NULL_WHITE_TEXTURE && 
          texture != NULL_GRAY_TEXTURE && 
          texture != NULL_BLACK_TEXTURE) {
        texture.Dispose();
      }
    }
  }
}