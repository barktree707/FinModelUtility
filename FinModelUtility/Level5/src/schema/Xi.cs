﻿using fin.image;
using fin.util.image;
using System.Drawing;

namespace level5.schema {
  public class Xi {
    public int Width { get; set; }
    public int Height { get; set; }

    List<int> Tiles { get; set; } = new List<int>();

    public byte ImageFormat { get; set; }

    public byte[] ImageData { get; set; }

    private bool SwitchFile { get; set; } = false;

    /*public static GenericTexture ToGenericTexture(byte[] xifile) {
      GenericTexture tex = new GenericTexture();
      Level5_XI xi = new Level5_XI();
      xi.Open(xifile);
      if (xi.SwitchFile && xi.ImageFormat == 0x1D) {
        tex.Mipmaps.Add(xi.BuildImageDataFromBlock_(8)[0]);
        tex.InternalFormat = OpenTK.Graphics.OpenGL.PixelInternalFormat.CompressedRgbS3tcDxt1Ext;
        tex.Width = (uint)xi.Height;
        tex.Height = (uint)xi.Width;
      } else
      if (xi.SwitchFile && xi.ImageFormat == 0x1F) {
        tex.Mipmaps.Add(xi.BuildImageDataFromBlock_(16)[0]);
        tex.InternalFormat = OpenTK.Graphics.OpenGL.PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;
        tex.Width = (uint)xi.Height;
        tex.Height = (uint)xi.Width;
      } else
      if (xi.SwitchFile && xi.ImageFormat == 0x1) {
        tex.Mipmaps.Add(xi.BuildImageData_()[0]);
        tex.InternalFormat = OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgb8;
        tex.PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.Rgb;
        tex.Width = (uint)xi.Height;
        tex.Height = (uint)xi.Width;
      } else {
        var texture = xi.ToBitmap();
        tex.FromBitmap(texture);
        texture.Dispose();
      }
      return tex;
    }*/

    public void Open(byte[] data) {
      using (var r = new EndianBinaryReader(new MemoryStream(data), Endianness.LittleEndian)) {
        r.Position = 0x10;
        Width = r.ReadInt16();
        Height = r.ReadInt16();

        r.Position = 0xA;
        int type = r.ReadByte();

        r.Position = 0x1C;
        int someTable = r.ReadInt16();

        r.Position = 0x38;
        int someTableSize = r.ReadInt32();

        int imageDataOffset = someTable + someTableSize;

        byte[] tileBytes = Decompress.Level5Decom(r.ReadBytesAtOffset((uint)someTable, someTableSize));

        if (tileBytes.Length > 2 && tileBytes[0] == 0x53 && tileBytes[1] == 0x04)
          SwitchFile = true;

        using (var tileData = new EndianBinaryReader(new MemoryStream(tileBytes), Endianness.LittleEndian)) {
          int tileCount = 0;
          while (tileData.Position + 2 <= tileData.BaseStream.Length) {
            int i = SwitchFile ? tileData.ReadInt32() : tileData.ReadInt16();
            if (i > tileCount) tileCount = i;
            Tiles.Add(i);
          }
        }

        switch (type) {
          case 0x1:
            type = 0x4;
            break;
          case 0x3:
            type = 0x1;
            break;
          case 0x4:
            type = 0x3;
            break;
          case 0x1B:
            type = 0xC;
            break;
          case 0x1C:
            type = 0xD;
            break;
          case 0x1D:
          case 0x1F:
            break;
          default:
            //File.WriteAllBytes("texture.bin", Decompress.Level5Decom(r.GetSection((uint)imageDataOffset, (int)(r.BaseStream.Length - imageDataOffset))));
            throw new Exception("Unknown Texture Type " + type.ToString("x"));
            //break;
        }

        ImageFormat = (byte)type;

        ImageData = Decompress.Level5Decom(r.ReadBytesAtOffset((uint)imageDataOffset, (int)(r.BaseStream.Length - imageDataOffset)));
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="blockSize"></param>
    /// <returns></returns>
    private List<byte[]> BuildImageData_() {
      List<byte[]> pixels = new List<byte[]>();

      var mip1 = new byte[Width * Height * 3];

      var tileSize = 64;
      var bpp = 3;

      var x = 0;
      var y = 0;

      for (int i = 2; i < Tiles.Count; i++) {
        int code = Tiles[i];

        // only need the first mip for now really...
        if ((i - 2) * tileSize * bpp >= mip1.Length)
          break;

        for (int h = 0; h < tileSize; h++) {
          var x1 = h / 8;
          var y1 = h % 8;
          for (int j = 0; j < bpp; j++)
            mip1[((x + x1) * Height + (y + y1)) * 3 + j] = ImageData[code * (tileSize * bpp) + h * bpp + j];
        }
        y += 8;

        if (y >= Height) {
          y = 0;
          x += 8;

          // TODO: This skips early, may not use all of the tiles. Is this right?
          if (x >= Width) {
            break;
          }
        }
      }

      pixels.Add(mip1);

      return pixels;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="blockSize"></param>
    /// <returns></returns>
    private List<byte[]> BuildImageDataFromBlock_(int blockSize) {
      List<byte[]> pixels = new List<byte[]>();

      var mip1 = new byte[Width * Height * (blockSize / 8) / 2];

      for (int i = 2; i < Tiles.Count; i++) {
        int code = Tiles[i];

        // only need the first mip for now really...
        if ((i - 2) * blockSize * 4 + blockSize * 4 > mip1.Length)
          break;

        for (int h = 0; h < blockSize * 4; h++)
          mip1[(i - 2) * blockSize * 4 + h] = ImageData[code * blockSize * 4 + h];
      }

      pixels.Add(mip1);

      return pixels;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public unsafe IImage ToBitmap() {
      Bitmap tileSheet = _3dsImageTools.DecodeImage(ImageData, Tiles.Count * 8, 8, (_3dsImageTools.TexFormat)ImageFormat);
      var img = new Rgba32Image(Width, Height);

      BitmapUtil.InvokeAsLocked(tileSheet, inputBmpData => {
        var inputPtr = (byte*)inputBmpData.Scan0;

        img.Mutate((_, setOutputHandlerRaw) => {
          Action<int, int, byte, byte, byte, byte> setOutputHandler = (x, y, r, g, b, a) =>
            setOutputHandlerRaw(y, x, r, g, b, a);

          int y = 0;
          int x = 0;
          for (int i = 0; i < Tiles.Count; i++) {
            int code = Tiles[i];

            if (code != -1) {
              for (int h = 0; h < 8; h++) {
                for (int w = 0; w < 8; w++) {
                  var inputIndex = 4 * ((code * 8 + w) + (h) * tileSheet.Width);
                  var b = inputPtr[inputIndex];
                  var g = inputPtr[inputIndex + 1];
                  var r = inputPtr[inputIndex + 2];
                  var a = inputPtr[inputIndex + 3];

                  setOutputHandler(x + w, y + h, r, g, b, a);
                }
              }
            }
            if (code == -1 && (ImageFormat == 0xC || ImageFormat == 0xD)) {
              for (int h = 0; h < 8; h++) {
                for (int w = 0; w < 8; w++) {
                  setOutputHandler(x + w, y + h, 0, 0, 0, 0);
                }
              }
            }
            y += 8;

            if (y >= Width) {
              y = 0;
              x += 8;

              // TODO: This skips early, may not use all of the tiles. Is this right?
              if (x >= Height) {
                break;
              }
            }
          }
        });
      });

      return img;
    }

  }
}