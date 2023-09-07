﻿using fin.io;
using fin.scene;

using modl.schema.xml;

namespace modl.api {
  public class BwSceneFileBundle : IBattalionWarsFileBundle, ISceneFileBundle {
    public required string GameName { get; init; }
    public IReadOnlyTreeFile MainFile => this.MainXmlFile;

    public required GameVersion GameVersion { get; init; }
    public required IReadOnlyTreeFile MainXmlFile { get; init; }
  }

  public class BwSceneImporter : ISceneImporter<BwSceneFileBundle> {
    public IScene ImportScene(BwSceneFileBundle sceneFileBundle)
      => new LevelXmlParser().Parse(sceneFileBundle.MainXmlFile,
                                    sceneFileBundle.GameVersion);
  }
}