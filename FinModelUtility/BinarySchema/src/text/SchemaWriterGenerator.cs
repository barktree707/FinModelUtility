﻿using System;
using Microsoft.CodeAnalysis;
using schema.util;


namespace schema.text {
  public class SchemaWriterGenerator {
    public string Generate(ISchemaStructure structure) {
      var typeSymbol = structure.TypeSymbol;

      var typeNamespace = SymbolTypeUtil.MergeContainingNamespaces(typeSymbol);

      var declaringTypes =
          SymbolTypeUtil.GetDeclaringTypesDownward(typeSymbol);

      var cbsb = new CurlyBracketStringBuilder();
      cbsb.WriteLine("using System;")
          .WriteLine("using System.IO;");

      // TODO: Handle fancier cases here
      cbsb.EnterBlock($"namespace {typeNamespace}");
      foreach (var declaringType in declaringTypes) {
        cbsb.EnterBlock(SymbolTypeUtil.GetQualifiersAndNameFor(declaringType));
      }
      cbsb.EnterBlock(SymbolTypeUtil.GetQualifiersAndNameFor(typeSymbol));

      cbsb.EnterBlock("public void Write(EndianBinaryWriter ew)");
      {
        var hasEndianness = structure.Endianness != null;
        if (hasEndianness) {
          cbsb.WriteLine(
              $"ew.PushStructureEndianness({SchemaGeneratorUtil.GetEndiannessName(structure.Endianness.Value)});");
        }

        foreach (var member in structure.Members) {
          SchemaWriterGenerator.WriteMember_(cbsb, typeSymbol, member);
        }

        if (hasEndianness) {
          cbsb.WriteLine("ew.PopEndianness();");
        }
      }
      cbsb.ExitBlock();

      // TODO: Handle fancier cases here

      // type
      cbsb.ExitBlock();

      // parent types
      foreach (var declaringType in declaringTypes) {
        cbsb.ExitBlock();
      }

      // namespace
      cbsb.ExitBlock();

      var generatedCode = cbsb.ToString();
      return generatedCode;
    }

    private static void WriteMember_(
        ICurlyBracketStringBuilder cbsb,
        ITypeSymbol sourceSymbol,
        ISchemaMember member) {
      if (member.IsPosition) {
        return;
      }

      if (member.Offset != null) {
        cbsb.WriteLine("throw new NotImplementedException();");
        return;
      }

      SchemaWriterGenerator.Align_(cbsb, member);

      var ifBoolean = member.IfBoolean;
      if (ifBoolean != null) {
        if (ifBoolean.SourceType == IfBooleanSourceType.IMMEDIATE_VALUE) {
          var booleanNumberType =
              SchemaPrimitiveTypesUtil.ConvertIntToNumber(
                  ifBoolean.ImmediateBooleanType);
          var booleanPrimitiveType =
              SchemaPrimitiveTypesUtil.ConvertNumberToPrimitive(
                  booleanNumberType);
          var booleanNumberLabel =
              SchemaGeneratorUtil.GetTypeName(booleanNumberType);
          var booleanPrimitiveLabel =
              SchemaGeneratorUtil.GetPrimitiveLabel(booleanPrimitiveType);
          cbsb.WriteLine(
                  $"ew.Write{booleanPrimitiveLabel}(({booleanNumberLabel}) (this.{member.Name} != null ? 1 : 0));")
              .EnterBlock($"if (this.{member.Name} != null)");
        } else {
          cbsb.EnterBlock($"if (this.{ifBoolean.BooleanMember.Name})");
        }
      }

      var memberType = member.MemberType;
      if (memberType is IGenericMemberType genericMemberType) {
        memberType = genericMemberType.ConstraintType;
      }

      switch (memberType) {
        case IPrimitiveMemberType: {
          SchemaWriterGenerator.WritePrimitive_(cbsb, member);
          break;
        }
        case IStringType: {
          SchemaWriterGenerator.WriteString_(cbsb, member);
          break;
        }
        case IStructureMemberType structureMemberType: {
          SchemaWriterGenerator.WriteStructure_(cbsb, member);
          break;
        }
        case ISequenceMemberType: {
          SchemaWriterGenerator.WriteArray_(cbsb, sourceSymbol, member);
          break;
        }
        default:
          // Anything that makes it down here probably isn't meant to be read.
          throw new NotImplementedException();
      }

      if (ifBoolean != null) {
        cbsb.ExitBlock();
      }
    }

    private static void Align_(
        ICurlyBracketStringBuilder cbsb,
        ISchemaMember member) {
      var align = member.Align;
      if (align != 0) {
        cbsb.WriteLine($"ew.Align({align});");
      }
    }

    private static void HandleMemberEndianness_(
        ICurlyBracketStringBuilder cbsb,
        ISchemaMember member,
        Action handler) {
      var hasEndianness = member.Endianness != null;
      if (hasEndianness) {
        cbsb.WriteLine(
            $"ew.PushMemberEndianness({SchemaGeneratorUtil.GetEndiannessName(member.Endianness.Value)});");
      }

      handler();

      if (hasEndianness) {
        cbsb.WriteLine("ew.PopEndianness();");
      }
    }

    private static void WritePrimitive_(
        ICurlyBracketStringBuilder cbsb,
        ISchemaMember member) {
      var primitiveType =
          Asserts.CastNonnull(member.MemberType as IPrimitiveMemberType);

      if (primitiveType.PrimitiveType == SchemaPrimitiveType.BOOLEAN) {
        SchemaWriterGenerator.WriteBoolean_(cbsb, member);
        return;
      }

      HandleMemberEndianness_(cbsb, member, () => {
        var readType = SchemaGeneratorUtil.GetPrimitiveLabel(
            primitiveType.UseAltFormat
                ? SchemaPrimitiveTypesUtil.ConvertNumberToPrimitive(
                    primitiveType.AltFormat)
                : primitiveType.PrimitiveType);

        var needToCast =
            primitiveType.UseAltFormat &&
            primitiveType.PrimitiveType !=
            SchemaPrimitiveTypesUtil.GetUnderlyingPrimitiveType(
                SchemaPrimitiveTypesUtil.ConvertNumberToPrimitive(
                    primitiveType.AltFormat));

        var castText = "";
        if (needToCast) {
          var castType =
              SchemaGeneratorUtil.GetTypeName(primitiveType.AltFormat);
          castText = $"({castType}) ";
        }

        var accessText = $"this.{member.Name}";
        if (member.MemberType.TypeInfo.IsNullable) {
          accessText = $"{accessText}.Value";
        }

        cbsb.WriteLine(
            $"ew.Write{readType}({castText}{accessText});");
      });
    }

    private static void WriteBoolean_(
        ICurlyBracketStringBuilder cbsb,
        ISchemaMember member) {
      HandleMemberEndianness_(cbsb, member, () => {
        var primitiveType =
            Asserts.CastNonnull(member.MemberType as IPrimitiveMemberType);

        var writeType = SchemaGeneratorUtil.GetPrimitiveLabel(
            SchemaPrimitiveTypesUtil.ConvertNumberToPrimitive(
                primitiveType.AltFormat));
        var castType = SchemaGeneratorUtil.GetTypeName(
            primitiveType.AltFormat);

        var accessText = $"this.{member.Name}";
        if (member.MemberType.TypeInfo.IsNullable) {
          accessText = $"{accessText}.Value";
        }

        cbsb.WriteLine(
            $"ew.Write{writeType}(({castType}) ({accessText} ? 1 : 0));");
      });
    }

    private static void WriteString_(
        ICurlyBracketStringBuilder cbsb,
        ISchemaMember member) {
      HandleMemberEndianness_(cbsb, member, () => {
        var stringType = Asserts.CastNonnull(member.MemberType as IStringType);

        if (stringType.LengthSourceType ==
            StringLengthSourceType.NULL_TERMINATED) {
          cbsb.WriteLine($"ew.WriteStringNT(this.{member.Name});");
        } else if (stringType.LengthSourceType ==
                   StringLengthSourceType.CONST) {
          cbsb.WriteLine(
              $"ew.WriteStringWithExactLength(this.{member.Name}, {stringType.ConstLength});");
        } else {
          cbsb.WriteLine($"ew.WriteString(this.{member.Name});");
        }
      });
    }

    private static void WriteStructure_(
        ICurlyBracketStringBuilder cbsb,
        ISchemaMember member) {
      HandleMemberEndianness_(cbsb, member, () => {
        // TODO: Do value types need to be handled differently?
        cbsb.WriteLine($"this.{member.Name}.Write(ew);");
      });
    }

    private static void WriteArray_(
        ICurlyBracketStringBuilder cbsb,
        ITypeSymbol sourceSymbol,
        ISchemaMember member) {
      var arrayType =
          Asserts.CastNonnull(member.MemberType as ISequenceMemberType);
      if (arrayType.LengthSourceType != SequenceLengthSourceType.READONLY) {
        var isImmediate =
            arrayType.LengthSourceType ==
            SequenceLengthSourceType.IMMEDIATE_VALUE;

        if (isImmediate) {
          var writeType = SchemaGeneratorUtil.GetIntLabel(
              arrayType.ImmediateLengthType);

          var castType = SchemaGeneratorUtil.GetTypeName(
              SchemaPrimitiveTypesUtil.ConvertIntToNumber(
                  arrayType.ImmediateLengthType));

          var arrayLengthName = arrayType.SequenceType == SequenceType.ARRAY
                                    ? "Length"
                                    : "Count";
          var arrayLengthAccessor = $"this.{member.Name}.{arrayLengthName}";

          cbsb.WriteLine(
              $"ew.Write{writeType}(({castType}) {arrayLengthAccessor});");
        }
      }

      SchemaWriterGenerator.WriteIntoArray_(cbsb, sourceSymbol, member);
    }

    private static void WriteIntoArray_(
        ICurlyBracketStringBuilder cbsb,
        ITypeSymbol sourceSymbol,
        ISchemaMember member) {
      HandleMemberEndianness_(cbsb, member, () => {
        var arrayType =
            Asserts.CastNonnull(member.MemberType as ISequenceMemberType);

        var elementType = arrayType.ElementType;
        if (elementType is IGenericMemberType genericElementType) {
          elementType = genericElementType.ConstraintType;
        }

        if (elementType is IPrimitiveMemberType primitiveElementType) {
          // Primitives that don't need to be cast are the easiest to write.
          if (!primitiveElementType.UseAltFormat) {
            var label =
                SchemaGeneratorUtil.GetPrimitiveLabel(
                    primitiveElementType.PrimitiveType);
            cbsb.WriteLine($"ew.Write{label}s(this.{member.Name});");
            return;
          }

          // Primitives that *do* need to be cast have to be written individually.
          var writeType = SchemaGeneratorUtil.GetPrimitiveLabel(
              SchemaPrimitiveTypesUtil.ConvertNumberToPrimitive(
                  primitiveElementType.AltFormat));
          var arrayLengthName = arrayType.SequenceType == SequenceType.ARRAY
                                    ? "Length"
                                    : "Count";
          var needToCast = primitiveElementType.UseAltFormat &&
                           primitiveElementType.PrimitiveType !=
                           SchemaPrimitiveTypesUtil.GetUnderlyingPrimitiveType(
                               SchemaPrimitiveTypesUtil
                                   .ConvertNumberToPrimitive(
                                       primitiveElementType.AltFormat));

          var castText = "";
          if (needToCast) {
            var castType =
                SchemaGeneratorUtil.GetTypeName(primitiveElementType.AltFormat);
            castText = $"({castType}) ";
          }

          cbsb.EnterBlock(
                  $"for (var i = 0; i < this.{member.Name}.{arrayLengthName}; ++i)")
              .WriteLine(
                  $"ew.Write{writeType}({castText}this.{member.Name}[i]);")
              .ExitBlock();
          return;
        }

        if (elementType is IStructureMemberType structureElementType) {
          //if (structureElementType.IsReferenceType) {
          cbsb.EnterBlock($"foreach (var e in this.{member.Name})")
              .WriteLine("e.Write(ew);")
              .ExitBlock();
          // TODO: Do value types need to be read like below?
          /*}
          // Value types (mainly structs) have to be pulled out, read, then put
          // back in.
          else {
            var arrayLengthName = arrayType.SequenceType == SequenceType.ARRAY
                                      ? "Length"
                                      : "Count";
            cbsb.EnterBlock(
                    $"for (var i = 0; i < this.{member.Name}.{arrayLengthName}; ++i)")
                .WriteLine($"var e = this.{member.Name}[i];")
                .WriteLine("e.Read(ew);")
                .WriteLine($"this.{member.Name}[i] = e;")
                .ExitBlock();
          }*/
          return;
        }

        // Anything that makes it down here probably isn't meant to be read.
        throw new NotImplementedException();
      });
    }
  }
}