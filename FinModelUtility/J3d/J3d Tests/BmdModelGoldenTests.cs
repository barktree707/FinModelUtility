﻿using System.Reflection;

using fin.io;
using fin.testing;
using fin.testing.model;

using j3d.api;
using j3d.GCN;

namespace j3d {
  [TestFixtureSource(nameof(GetGoldenDirectories_))]
  public class BmdModelGoldenTests
      : BModelGoldenTests<BmdModelFileBundle, BmdModelReader> {
    private readonly IFileHierarchyDirectory goldenDirectory_;
    private readonly BMD bmd_;

    public BmdModelGoldenTests(
        IFileHierarchyDirectory goldenDirectory) {
      this.goldenDirectory_ = goldenDirectory;
      this.bmd_ = new BMD(
          GetFileBundleFromDirectory(
                  this.goldenDirectory_.GetExistingSubdir("input"))
              .BmdFile.ReadAllBytes());
    }

    [Test]
    public void TestExportsGoldenAsExpected()
      => this.AssertGolden(this.goldenDirectory_);

    [Test]
    public async Task TestExportBmdDrw1s()
      => await SchemaTesting.WritesAndReadsIdentically(this.bmd_.DRW1);

    [Test]
    public async Task TestExportBmdInf1s()
      => await SchemaTesting.WritesAndReadsIdentically(this.bmd_.INF1);

    [Test]
    public async Task TestExportBmdJnt1s()
      => await SchemaTesting.WritesAndReadsIdentically(this.bmd_.JNT1);

    public override BmdModelFileBundle GetFileBundleFromDirectory(
        IFileHierarchyDirectory directory)
      => new() {
          GameName = "foobar",
          BmdFile = directory.FilesWithExtension(".bmd").Single(),
          BcxFiles = directory.FilesWithExtensions(".bca", ".bck").ToArray(),
          BtiFiles = directory.FilesWithExtension(".bti").ToArray(),
      };

    private static IFileHierarchyDirectory[] GetGoldenDirectories_() {
      var rootGoldenDirectory
          = ModelGoldenAssert
              .GetRootGoldensDirectory(Assembly.GetExecutingAssembly());
      return ModelGoldenAssert.GetGoldenDirectories(rootGoldenDirectory)
                              .ToArray();
    }
  }
}