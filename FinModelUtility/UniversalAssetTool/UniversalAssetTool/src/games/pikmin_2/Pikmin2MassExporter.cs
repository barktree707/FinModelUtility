﻿using j3d.api;

namespace uni.games.pikmin_2 {
  public class Pikmin2MassExporter : IMassExporter {
    public void ExportAll()
      => ExporterUtil.ExportAllForCli(new Pikmin2AnnotatedFileGatherer(),
                                  new BmdModelImporter());
  }
}