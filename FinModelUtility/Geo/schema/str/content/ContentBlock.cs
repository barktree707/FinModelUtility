﻿using fin.schema.data;

using schema.binary;

namespace geo.schema.str.content {
  [BinarySchema]
  public partial class ContentBlock : IBlock {
    public SwitchMagicWrapper<ContentType, IContent> Impl { get; }
      = new(er => (ContentType) er.ReadUInt32(),
            (ew, magic) => ew.WriteUInt32((uint) magic),
            magic => magic switch {
                ContentType.Header         => new FileInfo(),
                ContentType.Data           => new NoopContent(),
                ContentType.CompressedData => new NoopContent(),
            });

    public override string ToString() => this.Impl.ToString();
  }
}