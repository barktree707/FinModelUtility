﻿using System.IO;

using uni.platforms;

namespace uni.games.ocarina_of_time {
  public class OcarinaOfTimeExtractor {
    public void ExtractAll() {
      var ocarinaOfTimeRom =
          DirectoryConstants.ROMS_DIRECTORY.GetExistingFile(
              "ocarina_of_time.z64");

      OcarinaOfTimeBanks banks;
      {
        using var er = new EndianBinaryReader(ocarinaOfTimeRom.OpenRead());
        banks = new OcarinaOfTimeBanks(er);
      }
    }
  }
}