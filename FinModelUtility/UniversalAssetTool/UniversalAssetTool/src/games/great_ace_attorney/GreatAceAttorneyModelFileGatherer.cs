﻿using cmb.api;

using fin.io.bundles;

using uni.platforms.threeDs;

namespace uni.games.great_ace_attorney {
  public class GreatAceAttorneyModelAnnotatedFileGatherer
      : IAnnotatedFileBundleGatherer {
    public IEnumerable<IAnnotatedFileBundle> GatherFileBundles() {
      if (!new ThreeDsFileHierarchyExtractor().TryToExtractFromGame(
              "great_ace_attorney",
              out var fileHierarchy)) {
        yield break;
      }

      yield break;
    }
  }
}