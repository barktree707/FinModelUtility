﻿using fin.image;
using fin.image.io;

using level5.decompression;

using System.Drawing;

using FastBitmapLib;

using fin.image.io.tile;

using SixLabors.ImageSharp.PixelFormats;


namespace level5.schema {
  public class Xi {
    public int Width { get; set; }
    public int Height { get; set; }

    List<int> Tiles { get; set; } = new List<int>();

    public byte ImageFormat { get; set; }

    public byte[] ImageData { get; set; }

    private bool SwitchFile { get; set; } = false;

    public void Open(byte[] data) {
      using (var r =
             new EndianBinaryReader(data, Endianness.LittleEndian)) {
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

        var level5Decompressor = new Level5Decompressor();
        byte[] tileBytes =
            level5Decompressor.Decompress(
                r.ReadBytesAtOffset((uint) someTable, someTableSize));

        if (tileBytes.Length > 2 && tileBytes[0] == 0x53 &&
            tileBytes[1] == 0x04)
          SwitchFile = true;

        using (var tileData =
               new EndianBinaryReader(tileBytes, Endianness.LittleEndian)) {
          int tileCount = 0;
          while (tileData.Position + 2 <= tileData.Length) {
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

        ImageFormat = (byte) type;

        ImageData = level5Decompressor.Decompress(
            r.ReadBytesAtOffset((uint) imageDataOffset,
                                (int) (r.Length - imageDataOffset)));
      }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public unsafe IImage ToBitmap() {
      Bitmap tileSheet;

      var imageFormat = (_3dsImageTools.TexFormat) ImageFormat;
      if (imageFormat is _3dsImageTools.TexFormat.ETC1
                         or _3dsImageTools.TexFormat.ETC1a4) {
        tileSheet = TiledImageReader.New(Tiles.Count * 8,
                                         8,
                                         new Etc1TileReader(
                                             imageFormat is _3dsImageTools
                                                 .TexFormat.ETC1a4))
                                    .Read(ImageData)
                                    .AsBitmap();
      } else {
        tileSheet = _3dsImageTools.DecodeImage(
            ImageData,
            Tiles.Count * 8,
            8,
            imageFormat);
      }

      var tileSheetWidth = tileSheet.Width;

      var img = new Rgba32Image(Width, Height);

      using var inputBmpData = tileSheet.FastLock();
      var inputPtr = (byte*) inputBmpData.Scan0;

      using var dstImgLock = img.Lock();
      var dstPtr = dstImgLock.pixelScan0;

      int y = 0;
      int x = 0;
      for (int i = 0; i < Tiles.Count; i++) {
        int code = Tiles[i];

        if (code != -1) {
          for (int h = 0; h < 8; h++) {
            for (int w = 0; w < 8; w++) {
              var inputIndex = 4 * ((code * 8 + w) + (h) * tileSheetWidth);
              var b = inputPtr[inputIndex];
              var g = inputPtr[inputIndex + 1];
              var r = inputPtr[inputIndex + 2];
              var a = inputPtr[inputIndex + 3];

              dstPtr[(x + w) * Width + y + h] = new Rgba32(r, g, b, a);
            }
          }
        }

        if (code == -1 && (ImageFormat == 0xC || ImageFormat == 0xD)) {
          for (int h = 0; h < 8; h++) {
            for (int w = 0; w < 8; w++) {
              dstPtr[(x + w) * Width + y + h] = new Rgba32(0, 0, 0, 0);
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

      return img;
    }
  }
}