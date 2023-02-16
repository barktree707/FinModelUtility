﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using fin.util.asserts;

namespace fin.io {
  public static class FinDirectoryStatic {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Exists(string fullName)
      => FinFileSystem.Directory.Exists(fullName);

    public static bool Create(string fullName) {
      if (Exists(fullName)) {
        return false;
      }

      FinFileSystem.Directory.CreateDirectory(fullName);
      return true;
    }

    public static bool Delete(string fullName, bool recursive = false) {
      if (!Exists(fullName)) {
        return false;
      }

      FinFileSystem.Directory.Delete(fullName, recursive);
      return true;
    }

    public static void MoveTo(string fullName, string path) {
      try {
        FinFileSystem.Directory.Move(fullName, path);
      }
      // Sometimes the first move throws a permission denied error, so we just need to try again.
      catch {
        FinFileSystem.Directory.Move(fullName, path);
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<string> GetExistingSubdirs(string fullName)
      => FinFileSystem.Directory.EnumerateDirectories(fullName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetSubdir(string fullName,
                                   string relativePath,
                                   bool create = false) {
      var subdirs = relativePath.Split('/', '\\');
      var current = fullName;

      foreach (var subdir in subdirs) {
        if (subdir == "") {
          continue;
        }

        if (subdir == "..") {
          current = Asserts.CastNonnull(Path.GetDirectoryName(current));
          continue;
        }

        var matches = FinFileSystem.Directory.GetDirectories(current, subdir);
        if (matches.Length == 1) {
          current = matches.Single();
        } else {
          current = Path.Join(current, subdir);
          if (create) {
            FinFileSystem.Directory.CreateDirectory(current);
          }
        }
      }

      return current;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<string> GetExistingFiles(string fullName)
      => FinFileSystem.Directory.EnumerateFiles(fullName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<string> SearchForFiles(
        string fullName,
        string searchPattern,
        bool includeSubdirs = false)
      => FinFileSystem
         .Directory.GetFiles(
             fullName,
             searchPattern,
             includeSubdirs
                 ? SearchOption.AllDirectories
                 : SearchOption.TopDirectoryOnly);

    public static bool TryToGetExistingFile(
        string fullName,
        string path,
        out string file) {
      // TODO: Handle subdirectories automatically.
      var fileInfo = FinFileSystem.Directory.GetFiles(fullName, path)
                                  .SingleOrDefault();
      if (fileInfo != null) {
        file = fileInfo;
        return true;
      }

      file = default;
      return false;
    }

    public static string GetExistingFile(string fullName, string path) {
      if (TryToGetExistingFile(fullName, path, out var file)) {
        return file;
      }

      throw new Exception(
          $"Expected to find file: '{path}' in directory '{fullName}'");
    }

    public static string? PossiblyAssertExistingFile(
        string fullName,
        string relativePath,
        bool assert) {
      if (assert) {
        return GetExistingFile(fullName, relativePath);
      }

      TryToGetExistingFile(fullName, relativePath, out var file);
      return file;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<string> GetFilesWithExtension(
        string fullName,
        string extension,
        bool includeSubdirs = false)
      => FinFileSystem.Directory.GetFiles(
                          fullName,
                          $"*{Files.AssertValidExtension(extension)}",
                          includeSubdirs
                              ? SearchOption.AllDirectories
                              : SearchOption.TopDirectoryOnly);
  }
}