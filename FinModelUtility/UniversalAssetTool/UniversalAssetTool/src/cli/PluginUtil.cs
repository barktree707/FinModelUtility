﻿using cmb.api;

using fin.model.io;
using fin.model.io.importer.assimp;

using glo.api;

using j3d.api;

using mod.api;

namespace uni.cli {
  public static class PluginUtil {
    public static IReadOnlyList<IModelImporterPlugin> Plugins { get; } =
      new IModelImporterPlugin[] {
          new AssimpModelImporterPlugin(),
          new BmdModelImporterPlugin(),
          new CmbModelImporterPlugin(),
          new GloModelImporterPlugin(),
          new ModModelImporterPlugin(),
      };
  }
}