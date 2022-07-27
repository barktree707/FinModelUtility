﻿using NUnit.Framework;


namespace schema.text {
  internal class HalfGeneratorTests {
    [Test]
    public void TestHalf() {
      SchemaTestUtil.AssertGenerated(@"
using schema;

namespace foo.bar {
  [Schema]
  public partial class HalfWrapper {
    [Format(SchemaNumberType.HALF)]
    public float field1;

    [Format(SchemaNumberType.HALF)]
    public readonly float field2;

    [Format(SchemaNumberType.HALF)]
    public readonly float[] field3 = new float[5];
  }
}",
                                     @"using System;
using System.IO;
namespace foo.bar {
  public partial class HalfWrapper {
    public void Read(EndianBinaryReader er) {
      this.field1 = (Single) er.ReadHalf();
      er.AssertHalf((float) this.field2);
      for (var i = 0; i < this.field3.Length; ++i) {
        this.field3[i] = (Single) er.ReadHalf();
      }
    }
  }
}
",
                                     @"using System;
using System.IO;
namespace foo.bar {
  public partial class HalfWrapper {
    public void Write(EndianBinaryWriter ew) {
      ew.WriteHalf((float) this.field1);
      ew.WriteHalf((float) this.field2);
      for (var i = 0; i < this.field3.Length; ++i) {
        ew.WriteHalf((Single) this.field3[i]);
      }
    }
  }
}
");
    }
  }
}