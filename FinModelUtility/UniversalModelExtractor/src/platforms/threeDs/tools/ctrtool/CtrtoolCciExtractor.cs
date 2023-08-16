﻿using fin.io;
using fin.log;
using fin.util.asserts;

using uni.platforms.gcn.tools;
using uni.util.cmd;

namespace uni.platforms.threeDs.tools.ctrtool {
  public static partial class Ctrtool {
    public class CciExtractor {
      public bool Run(ISystemFile romFile, out IFileHierarchy hierarchy) {
        Asserts.True(
            romFile.Exists,
            $"Cannot dump ROM because it does not exist: {romFile}");

        var didChange = false;

        var directory = new FinDirectory(romFile.FullNameWithoutExtension);
        if (!directory.Exists || directory.IsEmpty) {
          didChange = true;

          if (!directory.Exists) {
            this.DumpRom_(romFile, directory);
            Asserts.False(directory.IsEmpty,
                          $"Failed to extract contents from the ROM: {romFile.FullName}");
          }
        }

        hierarchy = new FileHierarchy(directory);
        return didChange;
      }

      private void DumpRom_(ISystemFile romFile,
                            ISystemDirectory dstDirectory) {
        var logger = Logging.Create<CciExtractor>();
        logger.LogInformation($"Dumping ROM {romFile}...");

        Ctrtool.RunInCtrDirectoryAndCleanUp_(
            () => {
              ProcessUtil
                  .ExecuteBlockingSilently(
                      ThreeDsToolsConstants
                          .EXTRACT_CCI_BAT,
                      $"\"{romFile.FullName}\"",
                      $"\"{dstDirectory.FullName}\"");
            });
      }
    }
  }
}