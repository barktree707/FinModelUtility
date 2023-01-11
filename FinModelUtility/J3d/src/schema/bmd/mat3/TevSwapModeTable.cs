﻿using gx;
using schema;


namespace j3d.schema.bmd.mat3 {
  [BinarySchema]
  public partial class TevSwapModeTable : ITevSwapModeTable, IBiSerializable {
    public ChannelId R { get; set; }
    public ChannelId G { get; set; }
    public ChannelId B { get; set; }
    public ChannelId A { get; set; }
  }
}