﻿using schema.binary;
using schema.binary.attributes;

namespace uni.platforms.threeDs.tools.cia {
  public partial class CiaTickets : IChildOf<Cia>, IBinaryDeserializable {
    public Cia Parent { get; set; }

    public void Read(IEndianBinaryReader er) {
      er.Position += this.Parent.Header.TicketSize;
    }
  }
}