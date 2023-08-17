﻿using System.Reflection;

using cmb.api;

using fin.io;
using fin.testing.model;

namespace cmb {
  public class CmbModelGoldenTests
      : BModelGoldenTests<CmbModelFileBundle, CmbModelLoader> {
    [Test]
    [TestCaseSource(nameof(GetGoldenDirectories_))]
    public void TestExportsGoldenAsExpected(IFileHierarchyDirectory goldenDirectory)
      => this.AssertGolden(goldenDirectory);

    public override CmbModelFileBundle GetFileBundleFromDirectory(
        IFileHierarchyDirectory directory) {
      var cmbFile = directory.FilesWithExtension(".cmb").Single();
      return new CmbModelFileBundle(
          "foobar",
          cmbFile,
          directory.FilesWithExtension(".csab").ToArray(),
          null,
          null);
    }

    private static IFileHierarchyDirectory[] GetGoldenDirectories_() {
      var rootGoldenDirectory
          = ModelGoldenAssert
            .GetRootGoldensDirectory(Assembly.GetExecutingAssembly())
            .GetSubdir("cmb");
      return ModelGoldenAssert.GetGoldenDirectories(rootGoldenDirectory)
                              .ToArray();
    }
  }
}