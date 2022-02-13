using Microsoft.CodeAnalysis;

using NUnit.Framework;

namespace schema {
  public partial class SchemaStructureParserTest {
    [Test]
    public void TestByte() {
      var structure = SchemaTestUtil.Parse(@"
using schema;

namespace foo.bar {
  [Schema]
  public class ByteWrapper {
    public byte field;
  }
}");

      Assert.IsEmpty(structure.Diagnostics);

      Assert.AreEqual("bar", structure.TypeSymbol.ContainingNamespace.Name);
      Assert.AreEqual("ByteWrapper", structure.TypeSymbol.Name);

      Assert.AreEqual(1, structure.Members.Count);

      var field = structure.Members[0];
      Assert.AreEqual("field", field.Name);

      var memberType = field.MemberType;
      Assert.AreEqual(SpecialType.System_Byte, memberType.TypeSymbol.SpecialType);

      var primitiveType = (memberType as IPrimitiveMemberType)!;
      Assert.AreEqual(SchemaPrimitiveType.BYTE, primitiveType.PrimitiveType);
      Assert.AreEqual(false, primitiveType.IsConst);
      Assert.AreEqual(false, primitiveType.UseAltFormat);
      Assert.AreEqual(SchemaNumberType.UNDEFINED, primitiveType.AltFormat);
    }

    [Test]
    public void TestSByte() {
      var structure = SchemaTestUtil.Parse(@"
using schema;

namespace foo.bar {
  [Schema]
  public class SByteWrapper {
    public sbyte field;
  }
}");

      Assert.IsEmpty(structure.Diagnostics);

      Assert.AreEqual("bar", structure.TypeSymbol.ContainingNamespace.Name);
      Assert.AreEqual("SByteWrapper", structure.TypeSymbol.Name);

      Assert.AreEqual(1, structure.Members.Count);

      var field = structure.Members[0];
      Assert.AreEqual("field", field.Name);

      var memberType = field.MemberType;
      Assert.AreEqual(SpecialType.System_SByte, memberType.TypeSymbol.SpecialType);

      var primitiveType = (memberType as IPrimitiveMemberType)!;
      Assert.AreEqual(SchemaPrimitiveType.SBYTE, primitiveType.PrimitiveType);
      Assert.AreEqual(false, primitiveType.IsConst);
      Assert.AreEqual(false, primitiveType.UseAltFormat);
      Assert.AreEqual(SchemaNumberType.UNDEFINED, primitiveType.AltFormat);
    }

    [Test]
    public void TestInt16() {
      var structure = SchemaTestUtil.Parse(@"
using schema;

namespace foo.bar {
  [Schema]
  public class Int16Wrapper {
    public short field;
  }
}");

      Assert.IsEmpty(structure.Diagnostics);

      Assert.AreEqual("bar", structure.TypeSymbol.ContainingNamespace.Name);
      Assert.AreEqual("Int16Wrapper", structure.TypeSymbol.Name);

      Assert.AreEqual(1, structure.Members.Count);

      var field = structure.Members[0];
      Assert.AreEqual("field", field.Name);

      var memberType = field.MemberType;
      Assert.AreEqual(SpecialType.System_Int16, memberType.TypeSymbol.SpecialType);

      var primitiveType = (memberType as IPrimitiveMemberType)!;
      Assert.AreEqual(SchemaPrimitiveType.INT16, primitiveType.PrimitiveType);
      Assert.AreEqual(false, primitiveType.IsConst);
      Assert.AreEqual(false, primitiveType.UseAltFormat);
      Assert.AreEqual(SchemaNumberType.UNDEFINED, primitiveType.AltFormat);
    }

    [Test]
    public void TestEnum() {
      var structure = SchemaTestUtil.Parse(@"
using schema;

namespace foo.bar {
  public enum ValueType {
    A,
    B,
    C
  }

  [Schema]
  public class EnumWrapper {
    [Format(SchemaNumberType.UINT16)]
    public ValueType field;
  }
}");

      Assert.IsEmpty(structure.Diagnostics);

      Assert.AreEqual("bar", structure.TypeSymbol.ContainingNamespace.Name);
      Assert.AreEqual("EnumWrapper", structure.TypeSymbol.Name);

      Assert.AreEqual(1, structure.Members.Count);

      var field = structure.Members[0];
      Assert.AreEqual("field", field.Name);

      var memberType = field.MemberType;
      Assert.AreEqual(TypeKind.Enum, memberType.TypeSymbol.TypeKind);

      var primitiveType = (memberType as IPrimitiveMemberType)!;
      Assert.AreEqual(SchemaPrimitiveType.ENUM, primitiveType.PrimitiveType);
      Assert.AreEqual(false, primitiveType.IsConst);
      Assert.AreEqual(true, primitiveType.UseAltFormat);
      Assert.AreEqual(SchemaNumberType.UINT16, primitiveType.AltFormat);
    }

    [Test]
    public void TestConstArray() {
      var structure = SchemaTestUtil.Parse(@"
namespace foo.bar {
  [Schema]
  public class ArrayWrapper {
    public readonly int[] field;
  }
}");

      Assert.IsEmpty(structure.Diagnostics);

      var field = structure.Members[0];
      Assert.AreEqual("field", field.Name);

      var memberType = field.MemberType;
      Assert.AreEqual(TypeKind.Array, memberType.TypeSymbol.TypeKind);

      var arrayType = (memberType as ISequenceMemberType)!;
      Assert.AreEqual(SequenceType.ARRAY, arrayType.SequenceType);
      Assert.AreEqual(SequenceLengthType.CONST, arrayType.LengthType);

      var primitiveType = (arrayType.ElementType as IPrimitiveMemberType)!;
      Assert.AreEqual(SchemaPrimitiveType.INT32, primitiveType.PrimitiveType);
      Assert.AreEqual(false, primitiveType.IsConst);
      Assert.AreEqual(false, primitiveType.UseAltFormat);
      Assert.AreEqual(SchemaNumberType.UNDEFINED, primitiveType.AltFormat);
    }

    [Test]
    public void TestField() {
      var structure = SchemaTestUtil.Parse(@"
using schema;

namespace foo.bar {
  [Schema]
  public class ByteWrapper {
    public byte field;
  }
}");

      Assert.IsEmpty(structure.Diagnostics);

      Assert.AreEqual("bar", structure.TypeSymbol.ContainingNamespace.Name);
      Assert.AreEqual("ByteWrapper", structure.TypeSymbol.Name);

      Assert.AreEqual(1, structure.Members.Count);

      var field = structure.Members[0];
      Assert.AreEqual("field", field.Name);

      var memberType = field.MemberType;
      Assert.AreEqual(SpecialType.System_Byte, memberType.TypeSymbol.SpecialType);

      var primitiveType = (memberType as IPrimitiveMemberType)!;
      Assert.AreEqual(SchemaPrimitiveType.BYTE, primitiveType.PrimitiveType);
      Assert.AreEqual(false, primitiveType.IsConst);
      Assert.AreEqual(false, primitiveType.UseAltFormat);
      Assert.AreEqual(SchemaNumberType.UNDEFINED, primitiveType.AltFormat);
    }

    [Test]
    public void TestProperty() {
      var structure = SchemaTestUtil.Parse(@"
using schema;

namespace foo.bar {
  [Schema]
  public class ByteWrapper {
    public byte Field { get; set; }
  }
}");

      Assert.IsEmpty(structure.Diagnostics);

      Assert.AreEqual("bar", structure.TypeSymbol.ContainingNamespace.Name);
      Assert.AreEqual("ByteWrapper", structure.TypeSymbol.Name);

      Assert.AreEqual(1, structure.Members.Count);

      var field = structure.Members[0];
      Assert.AreEqual("Field", field.Name);

      var memberType = field.MemberType;
      Assert.AreEqual(SpecialType.System_Byte, memberType.TypeSymbol.SpecialType);

      var primitiveType = (memberType as IPrimitiveMemberType)!;
      Assert.AreEqual(SchemaPrimitiveType.BYTE, primitiveType.PrimitiveType);
      Assert.AreEqual(false, primitiveType.IsConst);
      Assert.AreEqual(false, primitiveType.UseAltFormat);
      Assert.AreEqual(SchemaNumberType.UNDEFINED, primitiveType.AltFormat);
    }

    [Test]
    public void TestReadonlyPrimitiveField() {
      var structure = SchemaTestUtil.Parse(@"
using schema;

namespace foo.bar {
  [Schema]
  public class ByteWrapper {
    public readonly byte field;
  }
}");

      Assert.IsEmpty(structure.Diagnostics);

      Assert.AreEqual("bar", structure.TypeSymbol.ContainingNamespace.Name);
      Assert.AreEqual("ByteWrapper", structure.TypeSymbol.Name);

      Assert.AreEqual(1, structure.Members.Count);

      var field = structure.Members[0];
      Assert.AreEqual("field", field.Name);

      var memberType = field.MemberType;
      Assert.AreEqual(SpecialType.System_Byte, memberType.TypeSymbol.SpecialType);

      var primitiveType = (memberType as IPrimitiveMemberType)!;
      Assert.AreEqual(SchemaPrimitiveType.BYTE, primitiveType.PrimitiveType);
      Assert.AreEqual(true, primitiveType.IsConst);
      Assert.AreEqual(false, primitiveType.UseAltFormat);
      Assert.AreEqual(SchemaNumberType.UNDEFINED, primitiveType.AltFormat);
    }

    [Test]
    public void TestReadonlyPrimitiveProperty() {
      var structure = SchemaTestUtil.Parse(@"
using schema;

namespace foo.bar {
  [Schema]
  public class ByteWrapper {
    public byte Field { get; }
  }
}");

      Assert.IsEmpty(structure.Diagnostics);

      Assert.AreEqual("bar", structure.TypeSymbol.ContainingNamespace.Name);
      Assert.AreEqual("ByteWrapper", structure.TypeSymbol.Name);

      Assert.AreEqual(1, structure.Members.Count);

      var field = structure.Members[0];
      Assert.AreEqual("Field", field.Name);

      var memberType = field.MemberType;
      Assert.AreEqual(SpecialType.System_Byte, memberType.TypeSymbol.SpecialType);

      var primitiveType = (memberType as IPrimitiveMemberType)!;
      Assert.AreEqual(SchemaPrimitiveType.BYTE, primitiveType.PrimitiveType);
      Assert.AreEqual(true, primitiveType.IsConst);
      Assert.AreEqual(false, primitiveType.UseAltFormat);
      Assert.AreEqual(SchemaNumberType.UNDEFINED, primitiveType.AltFormat);
    }
  }
}