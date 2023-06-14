﻿using System.Text;

using fin.data.stack;
using fin.io;
using fin.io.archive;

using schema.binary;
using schema.binary.attributes.sequence;

namespace uni.platforms.gcn.tools {
  /// <summary>
  ///   Shamelessly ported from version 1.0 (20050213) of gcmdump by thakis.
  /// </summary>
  public partial class GcmReader : IArchiveReader<SubArchiveContentFile> {
    public IArchiveStream<SubArchiveContentFile> Decompress(
        Stream romStream) {
      var isCiso = MagicTextUtil.Verify(romStream, "CISO");
      romStream.Position = 0;

      return new SubArchiveStream(
          !isCiso ? romStream : new CisoStream(romStream));
    }

    public bool TryToGetFiles(
        IArchiveStream<SubArchiveContentFile> archiveStream,
        out IEnumerable<SubArchiveContentFile> archiveContentFiles) {
      try {
        archiveContentFiles = this.EnumerateFiles(archiveStream);
          return true;
        } catch { }

        archiveContentFiles = default;
        return false;
      }

      public IEnumerable<SubArchiveContentFile> EnumerateFiles(
          IArchiveStream<SubArchiveContentFile> archiveStream) {
        var er = archiveStream.AsEndianBinaryReader(Endianness.BigEndian);

        var diskHeader = er.ReadNew<DiskHeader>();
        var fileEntries = this.ReadFileSystemTable_(er, diskHeader);

        var rootDirectoryFullName = "";

        var directories = new string[fileEntries.Count];
        directories[0] = rootDirectoryFullName;

        //for now, dump directory structure
        var directoryStack =
            new FinStack<(string fullName, uint lastChildIndex)>(
                (rootDirectoryFullName, (uint) fileEntries.Count));

        var fileTableOffset = 12 * fileEntries.Count;
        for (int i = 1; i < fileEntries.Count; ++i) {
          var e = fileEntries[i];

          // Pop to reach parent directory
          while (i >= directoryStack.Top.lastChildIndex) {
            directoryStack.Pop();
          }

          // Get name
          er.Position = diskHeader.FileSystemTableOffset + fileTableOffset +
                        e.FileNameOffset;
          var name = er.ReadStringNT(Encoding.UTF8);

          // Push new directory
          if (e.IsDirectory) {
            var parentDir = directories[e.FileOrParentOffset];
            var childDir = Path.Join(parentDir, name);

            directories[i] = childDir;
            directoryStack.Push((childDir, e.FileLengthOrNextOffset));
          }
          // Export file
          else {
            yield return new SubArchiveContentFile {
                RelativeName = Path.Join(directoryStack.Top.fullName, name),
                Position = (int) e.FileOrParentOffset,
                Length = (int) e.FileLengthOrNextOffset,
            };
          }
        }
      }

      private IList<FileEntry> ReadFileSystemTable_(IEndianBinaryReader er,
                                                    DiskHeader diskHeader) {
        var entries = new List<FileEntry>();

        //read files
        er.Position = diskHeader.FileSystemTableOffset;
        uint numFiles = 1;
        for (int i = 0; i < numFiles; ++i) {
          var entry = er.ReadNew<FileEntry>();
          entries.Add(entry);
          if (i == 0) {
            numFiles = entry.FileLengthOrNextOffset;
          }
        }

        return entries;
      }

      [BinarySchema]

    private partial class Ids : IBinaryConvertible {
      public byte ConsoleId { get; set; }
      public ushort GameId { get; set; }
      public byte CountryId { get; set; }
      public ushort MakerId { get; set; }
    }

    [BinarySchema]
    private partial class DiskHeader : IBinaryConvertible {
      public Ids Ids { get; } = new();
      public byte DiskId { get; set; }
      public byte Version { get; set; }
      public byte AudioStreaming { get; set; }
      public byte StreamBufferSize { get; set; }

      [SequenceLengthSource(0x12)]
      public byte[] Unused { get; set; }

      [StringLengthSource(4)]
      public string DvdMagicWord { get; set; }

      [StringLengthSource(0x3e0)]
      public string GameName { get; set; }

      public uint DebugMonitorOffset { get; set; }
      public uint DebugLoadAddress { get; set; }

      [SequenceLengthSource(0x18)]
      public byte[] Unused2 { get; set; }

      public uint DolOffset { get; set; }

      public uint FileSystemTableOffset { get; set; }
      public uint FileSystemTableSize { get; set; }
      public uint FileSystemTableMaximumSize { get; set; }

      public uint UserPosition { get; set; }
      public uint UserLength { get; set; }

      public uint Unknown { get; set; }
      public uint Unused3 { get; set; }
    }

    [BinarySchema]
    private partial class FileEntry : IBinaryConvertible {
      [IntegerFormat(SchemaIntegerType.BYTE)]
      public bool IsDirectory { get; set; }

      [IntegerFormat(SchemaIntegerType.UINT24)]
      public uint FileNameOffset { get; set; }

      public uint FileOrParentOffset { get; set; }
      public uint FileLengthOrNextOffset { get; set; }
    }
  }
}