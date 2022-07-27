﻿using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;


namespace schema.parser {
  public enum SchemaTypeKind {
    BOOL,
    INTEGER,
    FLOAT,
    CHAR,
    STRING,
    ENUM,
    STRUCTURE,
    SEQUENCE,
  }

  public interface ITypeInfo {
    ITypeSymbol TypeSymbol { get; }
    SchemaTypeKind Kind { get; }
    bool IsReadonly { get; }
    bool IsNullable { get; }
  }

  public interface IPrimitiveTypeInfo : ITypeInfo {
    SchemaPrimitiveType PrimitiveType { get; }
  }

  public interface IBoolTypeInfo : IPrimitiveTypeInfo { }

  public interface INumberTypeInfo : IPrimitiveTypeInfo {
    SchemaNumberType NumberType { get; }
  }

  public interface IIntegerTypeInfo : INumberTypeInfo {
    SchemaIntType IntType { get; }
  }

  public interface IEnumTypeInfo : IPrimitiveTypeInfo { }

  public interface ICharTypeInfo : IPrimitiveTypeInfo { }

  public interface IStringTypeInfo : ITypeInfo { }

  public interface IStructureTypeInfo : ITypeInfo {
    INamedTypeSymbol NamedTypeSymbol { get; }
  }

  public interface ISequenceTypeInfo : ITypeInfo {
    bool IsArray { get; }
    bool IsLengthConst { get; }
    ITypeInfo ElementTypeInfo { get; }
  }

  public class TypeInfoParser {
    public enum ParseStatus {
      SUCCESS,
      NOT_A_FIELD_OR_PROPERTY,
      NOT_IMPLEMENTED,
    }

    public IEnumerable<(ParseStatus, ISymbol, ITypeInfo)> ParseMembers(
        INamedTypeSymbol structureSymbol) {
      foreach (var memberSymbol in SymbolTypeUtil.GetInstanceMembers(structureSymbol)) {
        // Tries to parse the type to get info about it
        var parseStatus = this.ParseMember(
            memberSymbol, out var memberTypeInfo);
        yield return (parseStatus, memberSymbol, memberTypeInfo);
      }
    }

    public ParseStatus
        ParseMember(ISymbol memberSymbol, out ITypeInfo typeInfo) {
      typeInfo = null;

      if (!GetTypeOfMember_(
              memberSymbol,
              out var memberTypeSymbol,
              out var isReadonly)) {
        return ParseStatus.NOT_A_FIELD_OR_PROPERTY;
      }

      return this.ParseTypeSymbol(
          memberTypeSymbol,
          isReadonly,
          out typeInfo);
    }

    public ParseStatus ParseTypeSymbol(
        ITypeSymbol typeSymbol,
        bool isReadonly,
        out ITypeInfo typeInfo) {
      this.ParseNullable_(ref typeSymbol, out var isNullable);

      var primitiveType =
          SchemaPrimitiveTypesUtil.GetPrimitiveTypeFromTypeSymbol(
              typeSymbol);
      if (primitiveType != SchemaPrimitiveType.UNDEFINED) {
        switch (primitiveType) {
          case SchemaPrimitiveType.BOOLEAN: {
            typeInfo = new BoolTypeInfo(
                typeSymbol,
                isReadonly,
                isNullable);
            return ParseStatus.SUCCESS;
          }
          case SchemaPrimitiveType.BYTE:
          case SchemaPrimitiveType.SBYTE:
          case SchemaPrimitiveType.INT16:
          case SchemaPrimitiveType.UINT16:
          case SchemaPrimitiveType.INT32:
          case SchemaPrimitiveType.UINT32:
          case SchemaPrimitiveType.INT64:
          case SchemaPrimitiveType.UINT64: {
            typeInfo = new IntegerTypeInfo(
                typeSymbol,
                SchemaTypeKind.INTEGER,
                SchemaPrimitiveTypesUtil.ConvertNumberToInt(
                    SchemaPrimitiveTypesUtil
                        .ConvertPrimitiveToNumber(primitiveType)),
                isReadonly,
                isNullable);
            return ParseStatus.SUCCESS;
          }
          case SchemaPrimitiveType.SN8:
          case SchemaPrimitiveType.UN8:
          case SchemaPrimitiveType.SN16:
          case SchemaPrimitiveType.UN16:
          case SchemaPrimitiveType.SINGLE:
          case SchemaPrimitiveType.DOUBLE: {
            typeInfo = new FloatTypeInfo(
                typeSymbol,
                SchemaTypeKind.FLOAT,
                SchemaPrimitiveTypesUtil
                    .ConvertPrimitiveToNumber(primitiveType),
                isReadonly,
                isNullable);
            return ParseStatus.SUCCESS;
          }
          case SchemaPrimitiveType.CHAR: {
            typeInfo = new CharTypeInfo(
                typeSymbol,
                isReadonly,
                isNullable);
            return ParseStatus.SUCCESS;
          }
          case SchemaPrimitiveType.ENUM: {
            typeInfo = new EnumTypeInfo(
                typeSymbol,
                isReadonly,
                isNullable);
            return ParseStatus.SUCCESS;
          }
          default: throw new ArgumentOutOfRangeException();
        }
      }

      if (typeSymbol.SpecialType == SpecialType.System_String) {
        typeInfo = new StringTypeInfo(
            typeSymbol,
            isReadonly,
            isNullable);
        return ParseStatus.SUCCESS;
      }

      if (typeSymbol.SpecialType is SpecialType
              .System_Collections_Generic_IReadOnlyList_T) {
        var listTypeSymbol = typeSymbol as INamedTypeSymbol;

        var containedTypeSymbol = listTypeSymbol.TypeArguments[0];
        var containedParseStatus = this.ParseTypeSymbol(
            containedTypeSymbol,
            true,
            out var containedTypeInfo);
        if (containedParseStatus != ParseStatus.SUCCESS) {
          typeInfo = default;
          return containedParseStatus;
        }

        typeInfo = new SequenceTypeInfo(
            typeSymbol,
            isReadonly,
            isNullable,
            false,
            isReadonly,
            containedTypeInfo);
        return ParseStatus.SUCCESS;
      }

      if (typeSymbol.TypeKind is TypeKind.Array) {
        var arrayTypeSymbol = typeSymbol as IArrayTypeSymbol;

        var containedTypeSymbol = arrayTypeSymbol.ElementType;
        var containedParseStatus = this.ParseTypeSymbol(
            containedTypeSymbol,
            false,
            out var containedTypeInfo);
        if (containedParseStatus != ParseStatus.SUCCESS) {
          typeInfo = default;
          return containedParseStatus;
        }

        typeInfo = new SequenceTypeInfo(
            typeSymbol,
            isReadonly,
            isNullable,
            true,
            isReadonly,
            containedTypeInfo);
        return ParseStatus.SUCCESS;
      }

      if (typeSymbol.SpecialType is SpecialType
              .System_Collections_Generic_IList_T) {
        var listTypeSymbol = typeSymbol as INamedTypeSymbol;

        var containedTypeSymbol = listTypeSymbol.TypeArguments[0];
        var containedParseStatus = this.ParseTypeSymbol(
            containedTypeSymbol,
            false,
            out var containedTypeInfo);
        if (containedParseStatus != ParseStatus.SUCCESS) {
          typeInfo = default;
          return containedParseStatus;
        }

        typeInfo = new SequenceTypeInfo(
            typeSymbol,
            isReadonly,
            isNullable,
            false,
            false,
            containedTypeInfo);
        return ParseStatus.SUCCESS;
      }

      if (typeSymbol is INamedTypeSymbol namedTypeSymbol) {
        typeInfo = new StructureTypeInfo(
            namedTypeSymbol,
            isReadonly,
            isNullable);
        return ParseStatus.SUCCESS;
      }

      typeInfo = default;
      return ParseStatus.NOT_IMPLEMENTED;
    }

    private bool GetTypeOfMember_(
        ISymbol memberSymbol,
        out ITypeSymbol memberTypeSymbol,
        out bool isMemberReadonly) {
      switch (memberSymbol) {
        case IPropertySymbol propertySymbol: {
          isMemberReadonly = propertySymbol.SetMethod == null;
          memberTypeSymbol = propertySymbol.Type;
          return true;
        }
        case IFieldSymbol fieldSymbol: {
          isMemberReadonly = fieldSymbol.IsReadOnly;
          memberTypeSymbol = fieldSymbol.Type;
          return true;
        }
        default: {
          isMemberReadonly = false;
          memberTypeSymbol = default;
          return false;
        }
      }
    }

    private void ParseNullable_(ref ITypeSymbol typeSymbol,
                                out bool isNullable) {
      isNullable = false;
      if (typeSymbol is INamedTypeSymbol {
              Name: "Nullable"
          } fieldNamedTypeSymbol) {
        typeSymbol = fieldNamedTypeSymbol.TypeArguments[0];
        isNullable = true;
      } else if (typeSymbol.NullableAnnotation ==
                 NullableAnnotation.Annotated) {
        isNullable = true;
      }
    }

    private record BoolTypeInfo : IBoolTypeInfo {
      public BoolTypeInfo(
          ITypeSymbol typeSymbol,
          bool isReadonly,
          bool isNullable) {
        this.TypeSymbol = typeSymbol;
        this.IsReadonly = isReadonly;
        this.IsNullable = isNullable;
      }

      public ITypeSymbol TypeSymbol { get; }

      public SchemaPrimitiveType PrimitiveType => SchemaPrimitiveType.BOOLEAN;
      public SchemaTypeKind Kind => SchemaTypeKind.BOOL;

      public bool IsReadonly { get; }
      public bool IsNullable { get; }
    }


    private class FloatTypeInfo : INumberTypeInfo {
      public FloatTypeInfo(
          ITypeSymbol typeSymbol,
          SchemaTypeKind kind,
          SchemaNumberType numberType,
          bool isReadonly,
          bool isNullable) {
        this.TypeSymbol = typeSymbol;
        this.Kind = kind;
        this.NumberType = numberType;
        this.IsReadonly = isReadonly;
        this.IsNullable = isNullable;
      }

      public ITypeSymbol TypeSymbol { get; }
      public SchemaTypeKind Kind { get; }
      public SchemaNumberType NumberType { get; }

      public SchemaPrimitiveType PrimitiveType
        => SchemaPrimitiveTypesUtil.ConvertNumberToPrimitive(this.NumberType);

      public bool IsReadonly { get; }
      public bool IsNullable { get; }
    }

    private class IntegerTypeInfo : IIntegerTypeInfo {
      public IntegerTypeInfo(
          ITypeSymbol typeSymbol,
          SchemaTypeKind kind,
          SchemaIntType intType,
          bool isReadonly,
          bool isNullable) {
        this.TypeSymbol = typeSymbol;
        this.Kind = kind;
        this.IntType = intType;
        this.IsReadonly = isReadonly;
        this.IsNullable = isNullable;
      }

      public ITypeSymbol TypeSymbol { get; }
      public SchemaTypeKind Kind { get; }
      public SchemaIntType IntType { get; }

      public SchemaNumberType NumberType
        => SchemaPrimitiveTypesUtil.ConvertIntToNumber(this.IntType);

      public SchemaPrimitiveType PrimitiveType
        => SchemaPrimitiveTypesUtil.ConvertNumberToPrimitive(this.NumberType);

      public bool IsReadonly { get; }
      public bool IsNullable { get; }
    }

    private class CharTypeInfo : ICharTypeInfo {
      public CharTypeInfo(
          ITypeSymbol typeSymbol,
          bool isReadonly,
          bool isNullable) {
        this.TypeSymbol = typeSymbol;
        this.IsReadonly = isReadonly;
        this.IsNullable = isNullable;
      }

      public SchemaPrimitiveType PrimitiveType => SchemaPrimitiveType.CHAR;
      public SchemaTypeKind Kind => SchemaTypeKind.CHAR;

      public ITypeSymbol TypeSymbol { get; }

      public bool IsReadonly { get; }
      public bool IsNullable { get; }
    }

    private class StringTypeInfo : IStringTypeInfo {
      public StringTypeInfo(
          ITypeSymbol typeSymbol,
          bool isReadonly,
          bool isNullable) {
        this.TypeSymbol = typeSymbol;
        this.IsReadonly = isReadonly;
        this.IsNullable = isNullable;
      }

      public SchemaTypeKind Kind => SchemaTypeKind.STRING;

      public ITypeSymbol TypeSymbol { get; }

      public bool IsReadonly { get; }
      public bool IsNullable { get; }
    }

    private class EnumTypeInfo : IEnumTypeInfo {
      public EnumTypeInfo(
          ITypeSymbol typeSymbol,
          bool isReadonly,
          bool isNullable) {
        this.TypeSymbol = typeSymbol;
        this.IsReadonly = isReadonly;
        this.IsNullable = isNullable;
      }

      public SchemaPrimitiveType PrimitiveType => SchemaPrimitiveType.ENUM;
      public SchemaTypeKind Kind => SchemaTypeKind.ENUM;

      public ITypeSymbol TypeSymbol { get; }

      public bool IsReadonly { get; }
      public bool IsNullable { get; }
    }

    private class StructureTypeInfo : IStructureTypeInfo {
      public StructureTypeInfo(
          INamedTypeSymbol namedTypeSymbol,
          bool isReadonly,
          bool isNullable) {
        this.NamedTypeSymbol = namedTypeSymbol;
        this.IsReadonly = isReadonly;
        this.IsNullable = isNullable;
      }

      public SchemaTypeKind Kind => SchemaTypeKind.STRUCTURE;

      public INamedTypeSymbol NamedTypeSymbol { get; }
      public ITypeSymbol TypeSymbol => this.NamedTypeSymbol;

      public bool IsReadonly { get; }
      public bool IsNullable { get; }
    }

    private class SequenceTypeInfo : ISequenceTypeInfo {
      public SequenceTypeInfo(
          ITypeSymbol typeSymbol,
          bool isReadonly,
          bool isNullable,
          bool isArray,
          bool isLengthConst,
          ITypeInfo containedType) {
        this.TypeSymbol = typeSymbol;
        this.IsReadonly = isReadonly;
        this.IsNullable = isNullable;
        this.IsArray = isArray;
        this.IsLengthConst = isLengthConst;
        this.ElementTypeInfo = containedType;
      }

      public SchemaTypeKind Kind => SchemaTypeKind.SEQUENCE;

      public ITypeSymbol TypeSymbol { get; }

      public bool IsReadonly { get; }
      public bool IsNullable { get; }

      public bool IsArray { get; }
      public bool IsLengthConst { get; }
      public ITypeInfo ElementTypeInfo { get; }
    }
  }
}