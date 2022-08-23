﻿using NUnit.Framework;


namespace schema.text {
  internal class IfBooleanDiagnosticsTests {
    [Test]
    public void TestIfBooleanNonReference() {
      var structure = SchemaTestUtil.Parse(@"
using schema;
namespace foo.bar {
  [BinarySchema]
  public partial class BooleanWrapper : IBiSerializable {
    [IfBoolean(SchemaIntegerType.BYTE)]
    public int field;
  }
}");
      SchemaTestUtil.AssertDiagnostics(structure.Diagnostics,
                                       Rules.IfBooleanNeedsNullable);
    }

    [Test]
    public void TestIfBooleanNonNullable() {
      var structure = SchemaTestUtil.Parse(@"
using schema;
namespace foo.bar {
  [BinarySchema]
  public partial class BooleanWrapper : IBiSerializable {
    [IfBoolean(SchemaIntegerType.BYTE)]
    public A field;
  }

  [BinarySchema]
  public partial class A : IBiSerializable {
  }
}");
      SchemaTestUtil.AssertDiagnostics(structure.Diagnostics,
                                       Rules.IfBooleanNeedsNullable);
    }
  }
}