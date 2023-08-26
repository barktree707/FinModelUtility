﻿using fin.io;
using fin.util.asserts;
using fin.util.enumerables;
using fin.util.linq;

using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

namespace uni.platforms.desktop {
  internal static class SteamUtils {
    private static string? InstallPath_ { get; } =
      RegistryExtensions.GetSoftwareValueEither32Or64Bit(
          @"Valve\Steam",
          "InstallPath") as string;

    private static IReadOnlySystemDirectory? InstallDirectory_ { get; } =
      SteamUtils.InstallPath_ != null
          ? new FinDirectory(SteamUtils.InstallPath_)
          : null;

    private static IReadOnlySystemFile? LibraryFoldersVdf_
      => (InstallDirectory_?.TryToGetExistingFile(
          "config/libraryfolders.vdf",
          out var libraryFoldersVdf) ?? false)
          ? libraryFoldersVdf
          : null;

    private static ISystemDirectory[] Libraries_ { get; } =
      !(LibraryFoldersVdf_?.Exists ?? false)
          ? Array.Empty<ISystemDirectory>()
          : VdfConvert
            .Deserialize(LibraryFoldersVdf_.OpenReadAsText())
            .Value
            .Children()
            .SelectMany(section => {
              try {
                return
                    section
                        .Value<VProperty>()
                        .Value
                        .Select(section => section["path"])
                        .Nonnull()
                        .Select(token => token.ToString());
              } catch {
                return Enumerable.Empty<string>();
              }
            })
            .Select(path => new FinDirectory(path))
            .CastTo<FinDirectory, ISystemDirectory>()
            // A steam library directory may not exist if it lives on an
            // external hard drive
            .Where(steamDirectory => steamDirectory.Exists)
            .ToArray();

    private static ISystemDirectory[] CommonDirectories_ { get; } =
      Libraries_
          .SelectMany(
              libraryFolder
                  => libraryFolder.GetExistingSubdirs()
                                  .Where(dir => dir.Name == "steamapps"))
          .SelectMany(
              steamApps
                  => steamApps.GetExistingSubdirs()
                              .Where(dir => dir.Name == "common"))
          .ToArray();

    public static ISystemDirectory[] GameDirectories { get; }
      = CommonDirectories_
        .SelectMany(common => common.GetExistingSubdirs())
        .ToArray();

    public static bool TryGetGameDirectory(
        string name,
        out ISystemDirectory directory,
        bool assert = false) {
      if (GameDirectories.TryGetFirst(game => game.Name == name,
                                      out directory)) {
        return true;
      }

      Asserts.False(assert, $"Could not find \"{name}\" installed in Steam.");
      return false;
    }
  }
}