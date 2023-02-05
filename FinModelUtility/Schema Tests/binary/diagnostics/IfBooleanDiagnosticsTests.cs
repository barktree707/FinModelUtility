﻿using NUnit.Framework;


namespace schema.binary.text {
  internal class IfBooleanDiagnosticsTests {
    [Test]
    public void TestIfBooleanNonReference() {
      var structure = BinarySchemaTestUtil.ParseFirst(@"
using schema.binary;
namespace foo.bar {
  [BinarySchema]
  public partial class BooleanWrapper : IBiSerializable {
    [IfBoolean(SchemaIntegerType.BYTE)]
    public int field;
  }
}");
      BinarySchemaTestUtil.AssertDiagnostics(structure.Diagnostics,
                                       Rules.IfBooleanNeedsNullable);
    }

    [Test]
    public void TestIfBooleanNonNullable() {
      var structure = BinarySchemaTestUtil.ParseFirst(@"
using schema.binary;
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
      BinarySchemaTestUtil.AssertDiagnostics(structure.Diagnostics,
                                       Rules.IfBooleanNeedsNullable);
    }

    [Test]
    public void TestOutOfOrder() {
      var structure = BinarySchemaTestUtil.ParseFirst(@"
using schema.binary;

namespace foo.bar {
  [BinarySchema]
  public partial class ByteWrapper : IBiSerializable {
    [IfBoolean(nameof(Field))]
    public int? OtherValue { get; set; }

    [IntegerFormat(SchemaIntegerType.BYTE)]
    private bool Field { get; set; }
  }

  public class A : IBiSerializable { }
}");
      BinarySchemaTestUtil.AssertDiagnostics(structure.Diagnostics,
                                       Rules.DependentMustComeAfterSource);
    }

    [Test]
    public void TestPublicPropertySource() {
      var structure = BinarySchemaTestUtil.ParseFirst(@"
using schema.binary;

namespace foo.bar {
  [BinarySchema]
  public partial class ByteWrapper : IBiSerializable {
    [IntegerFormat(SchemaIntegerType.BYTE)]
    public bool Field { get; set; }

    [IfBoolean(nameof(Field))]
    public int? OtherValue { get; set; }
  }

  public class A : IBiSerializable { }
}");
      BinarySchemaTestUtil.AssertDiagnostics(structure.Diagnostics,
                                       Rules.SourceMustBePrivate);
    }

    [Test]
    public void TestProtectedPropertySource() {
      var structure = BinarySchemaTestUtil.ParseFirst(@"
using schema.binary;

namespace foo.bar {
  [BinarySchema]
  public partial class ByteWrapper : IBiSerializable {
    [IntegerFormat(SchemaIntegerType.BYTE)]
    protected bool Field { get; set; }

    [IfBoolean(nameof(Field))]
    public int? OtherValue { get; set; }
  }

  public class A : IBiSerializable { }
}");
      BinarySchemaTestUtil.AssertDiagnostics(structure.Diagnostics,
                                       Rules.SourceMustBePrivate);
    }

    [Test]
    public void TestInternalPropertySource() {
      var structure = BinarySchemaTestUtil.ParseFirst(@"
using schema.binary;

namespace foo.bar {
  [BinarySchema]
  public partial class ByteWrapper : IBiSerializable {
    [IntegerFormat(SchemaIntegerType.BYTE)]
    internal bool Field { get; set; }

    [IfBoolean(nameof(Field))]
    public int? OtherValue { get; set; }
  }

  public class A : IBiSerializable { }
}");
      BinarySchemaTestUtil.AssertDiagnostics(structure.Diagnostics,
                                       Rules.SourceMustBePrivate);
    }

    [Test]
    public void TestPublicFieldSource() {
      var structure = BinarySchemaTestUtil.ParseFirst(@"
using schema.binary;

namespace foo.bar {
  [BinarySchema]
  public partial class ByteWrapper : IBiSerializable {
    [IntegerFormat(SchemaIntegerType.BYTE)]
    public bool Field;

    [IfBoolean(nameof(Field))]
    public int? OtherValue { get; set; }
  }

  public class A : IBiSerializable { }
}");
      BinarySchemaTestUtil.AssertDiagnostics(structure.Diagnostics,
                                       Rules.SourceMustBePrivate);
    }

    [Test]
    public void TestProtectedFieldSource() {
      var structure = BinarySchemaTestUtil.ParseFirst(@"
using schema.binary;

namespace foo.bar {
  [BinarySchema]
  public partial class ByteWrapper : IBiSerializable {
    [IntegerFormat(SchemaIntegerType.BYTE)]
    protected bool Field;

    [IfBoolean(nameof(Field))]
    public int? OtherValue { get; set; }
  }

  public class A : IBiSerializable { }
}");
      BinarySchemaTestUtil.AssertDiagnostics(structure.Diagnostics,
                                       Rules.SourceMustBePrivate);
    }

    [Test]
    public void TestInternalFieldSource() {
      var structure = BinarySchemaTestUtil.ParseFirst(@"
using schema.binary;

namespace foo.bar {
  [BinarySchema]
  public partial class ByteWrapper : IBiSerializable {
    [IntegerFormat(SchemaIntegerType.BYTE)]
    internal bool Field;

    [IfBoolean(nameof(Field))]
    public int? OtherValue { get; set; }
  }

  public class A : IBiSerializable { }
}");
      BinarySchemaTestUtil.AssertDiagnostics(structure.Diagnostics,
                                       Rules.SourceMustBePrivate);
    }
  }
}