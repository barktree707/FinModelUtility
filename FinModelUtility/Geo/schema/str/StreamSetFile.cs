﻿/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using fin.schema.data;

using schema.binary;
using schema.binary.attributes.array;
using schema.binary.attributes.endianness;

namespace geo.schema.str {
  [Endianness(Endianness.LittleEndian)]
  [BinarySchema]
  public partial class StreamSetFile : IBinaryConvertible {
    private readonly string magic_ = "3slo";
    private readonly uint size_ = 12;

    /* Dead Space:
     * unknown00 = 2
     * unknown02 = 259
     * 
     * Dead Space 2:
     * unknown00 = 2
     * unknown02 = 259
     * 
     * Dante's Inferno:
     * unknown00 = 2
     * unknown02 = 1537
     */
    public ushort Unknown00 { get; set; }
    public ushort Unknown02 { get; set; }

    [ArrayUntilEndOfStream]
    public List<Section> Contents { get; } = new();

    [BinarySchema]
    private partial class Section : IBinaryConvertible {
      public SwitchMagicSizedSection<ISectionType> Impl { get; }
        = new(4,
              magic => magic switch {
                "COHS" => new NoopSection(),
                "LLIF" => new NoopSection(),
              });
    }

    public interface ISectionType : IBinaryConvertible { }

    [BinarySchema]
    public partial class ContentSection : ISectionType {
      public SwitchMagicSizedSection<ISectionType> Impl { get; }
        = new(4,
              magic => magic switch {
                  "RDHS" => new NoopSection(),
                  "TADS" => new NoopSection(),
                  "kapR" => new NoopSection(),
              });
    }


    [BinarySchema]
    public partial class NoopSection : ISectionType { }

    /*case StreamSet.BlockType.Content: {
  this.Contents.Add(new StreamSet.ContentInfo() {
      Type = type, Offset = input.Position, Size = blockSize - 12,
  });
  break;
}*/
  }
}