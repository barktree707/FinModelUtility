﻿using System.IO;
using System.Linq;

using fin.util.asserts;

namespace fin.io {
  public static class Files {
    public static DirectoryInfo GetCwd()
      => new(Directory.GetCurrentDirectory());


    // Getting Files
    public static FinFile[] GetFilesWithExtension(
        string extension,
        bool includeSubdirs = false)
      => Files.GetFilesWithExtension(Files.GetCwd(), extension, includeSubdirs);

    public static FinFile[] GetFilesWithExtension(
        DirectoryInfo directory,
        string extension,
        bool includeSubdirs = false)
      => directory.GetFiles($"*.{extension}",
                            includeSubdirs
                                ? SearchOption.AllDirectories
                                : SearchOption.TopDirectoryOnly)
                  .Select(fileInfo => new FinFile(fileInfo))
                  .ToArray();

    public static IFile GetFileWithExtension(
        DirectoryInfo directory,
        string extension,
        bool includeSubdirs = false)
      => new FinFile(
          Files.GetPathWithExtension(directory, extension, includeSubdirs));


    public static string[] GetPathsWithExtension(
        DirectoryInfo directory,
        string extension,
        bool includeSubdirs = false)
      => Files.GetFilesWithExtension(directory, extension, includeSubdirs)
              .Select(file => file.FullName)
              .ToArray();

    public static string GetPathWithExtension(
        DirectoryInfo directory,
        string extension,
        bool includeSubdirs = false) {
      var paths =
          Files.GetPathsWithExtension(directory, extension, includeSubdirs);

      var errorMessage =
          $"Expected to find a single '.{extension}' file within '{Files.GetCwd().FullName}' but found {paths.Length}";
      if (paths.Length == 0) {
        errorMessage += ".";
      } else {
        errorMessage += ":\n";
        errorMessage = paths.Aggregate(errorMessage,
                                       (current, path)
                                           => current + (path + "\n"));
      }

      Asserts.True(paths.Length == 1, errorMessage);

      return paths[0];
    }

    public static string[] GetPathsWithExtension(
        string extension,
        bool includeSubdirs = false)
      => Files.GetPathsWithExtension(Files.GetCwd(), extension, includeSubdirs);

    public static string GetPathWithExtension(
        string extension,
        bool includeSubdirs = false)
      => Files.GetPathWithExtension(Files.GetCwd(), extension, includeSubdirs);
  }
}