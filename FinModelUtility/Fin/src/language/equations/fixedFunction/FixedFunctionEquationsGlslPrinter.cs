﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using fin.model;
using fin.util.asserts;


namespace fin.language.equations.fixedFunction {
  public class FixedFunctionEquationsGlslPrinter {
    private readonly IReadOnlyList<ITexture?> textures_;

    public FixedFunctionEquationsGlslPrinter(IReadOnlyList<ITexture> textures) {
      this.textures_ = textures;
    }

    public string Print(IReadOnlyFixedFunctionMaterial material) {
      var sb = new StringBuilder();

      using var os = new StringWriter(sb);
      this.Print(os, material);

      return sb.ToString();
    }

    public void Print(
        StringWriter os,
        IReadOnlyFixedFunctionMaterial material) {
      var equations = material.Equations;

      os.WriteLine("# version 330");
      os.WriteLine();

      for (var t = 0; t < MaterialConstants.MAX_TEXTURES; ++t) {
        if (new[] {
                FixedFunctionSource.TEXTURE_COLOR_0 + t,
                FixedFunctionSource.TEXTURE_ALPHA_0 + t
            }.Any(equations.HasInput)) {
          os.WriteLine($"uniform sampler2D texture{t};");
        }
      }
      os.WriteLine();

      var hasAllLightsMerged = new[] {
          FixedFunctionSource.ALL_LIGHTING_MERGED_COLOR,
          FixedFunctionSource.ALL_LIGHTING_MERGED_ALPHA
      }.Any(equations.HasInput);

      var hasGlobalLightsMerged = new[] {
          FixedFunctionSource.GLOBAL_LIGHTING_MERGED_COLOR,
          FixedFunctionSource.GLOBAL_LIGHTING_MERGED_ALPHA
      }.Any(equations.HasInput);

      var hasLocalLightsMerged = new[] {
          FixedFunctionSource.LOCAL_LIGHTING_MERGED_COLOR,
          FixedFunctionSource.LOCAL_LIGHTING_MERGED_ALPHA
      }.Any(equations.HasInput);

      var hasIndividualGlobalLights = Enumerable
                           .Range(0, MaterialConstants.MAX_GLOBAL_LIGHTS)
                           .Select(
                               i => new[] {
                                   FixedFunctionSource.GLOBAL_LIGHT_0_COLOR + i,
                                   FixedFunctionSource.GLOBAL_LIGHT_0_ALPHA + i
                               }.Any(equations.HasInput))
                           .ToArray();
      var hasIndividualLocalLights = Enumerable
                           .Range(0, MaterialConstants.MAX_LOCAL_LIGHTS)
                           .Select(
                               i => new[] {
                                   FixedFunctionSource.LOCAL_LIGHT_0_COLOR + i,
                                   FixedFunctionSource.LOCAL_LIGHT_0_ALPHA + i
                               }.Any(equations.HasInput))
                           .ToArray();

      var dependsOnGlobalLights = hasAllLightsMerged || hasGlobalLightsMerged ||
                                  hasIndividualGlobalLights.Any(value => value);
      var dependsOnLocalLights = hasAllLightsMerged || hasLocalLightsMerged ||
                                 hasIndividualLocalLights.Any(value => value);
      
      if (dependsOnGlobalLights || dependsOnLocalLights) {
        os.WriteLine(@"
struct Light {
  bool enabled;
  vec3 position;
  vec3 normal;
  vec4 color;
};
");
      }

      if (dependsOnGlobalLights) {
        os.WriteLine(
            $"uniform Light globalLights[{MaterialConstants.MAX_GLOBAL_LIGHTS}];");
      }
      if (dependsOnLocalLights) {
        os.WriteLine(
            $"uniform Light localLights[{MaterialConstants.MAX_LOCAL_LIGHTS}];");
      }
      os.WriteLine();

      os.WriteLine("in vec2 normalUv;");
      os.WriteLine("in vec3 vertexNormal;");
      for (var i = 0; i < MaterialConstants.MAX_COLORS; ++i) {
        os.WriteLine($"in vec4 vertexColor{i};");
      }
      for (var i = 0; i < MaterialConstants.MAX_UVS; ++i) {
        os.WriteLine($"in vec2 uv{i};");
      }
      os.WriteLine();
      os.WriteLine("out vec4 fragColor;");
      os.WriteLine();
      os.WriteLine("void main() {");

      if (new[] {
              FixedFunctionSource.ALL_LIGHTING_MERGED_COLOR,
              FixedFunctionSource.ALL_LIGHTING_MERGED_ALPHA
          }.Any(equations.HasInput)) {
        os.WriteLine(@"  vec3 diffuseLightNormal = normalize(globalLights[0].normal);
  float diffuseLightAmount = max(-dot(vertexNormal, diffuseLightNormal), 0);

  float lightAmount = min(diffuseLightAmount, 1);
  vec3 lightColor = vec3(.5, .5, .5);
  
  vec4 allLightMergedColor = vec4(lightAmount * lightColor, 1);");
        os.WriteLine();
      }

      // TODO: Get tree of all values that this depends on, in case there needs to be other variables defined before.
      var outputColor =
          equations.ColorOutputs[FixedFunctionSource.OUTPUT_COLOR];

      os.Write("  vec3 colorComponent = ");
      this.PrintColorValue_(os, outputColor.ColorValue);
      os.WriteLine(";");
      os.WriteLine();

      var outputAlpha =
          equations.ScalarOutputs[FixedFunctionSource.OUTPUT_ALPHA];

      os.Write("  float alphaComponent = ");
      this.PrintScalarValue_(os, outputAlpha.ScalarValue);
      os.WriteLine(";");
      os.WriteLine();

      os.WriteLine("  fragColor = vec4(colorComponent, alphaComponent);");

      var alphaOpValue =
          DetermineAlphaOpValue(
              material.AlphaOp,
              DetermineAlphaCompareType(
                  material.AlphaCompareType0,
                  material.AlphaReference0),
              DetermineAlphaCompareType(
                  material.AlphaCompareType1,
                  material.AlphaReference1));

      if (alphaOpValue != AlphaOpValue.ALWAYS_TRUE) {
        os.WriteLine();

        var alphaCompareText0 =
            GetAlphaCompareText_(material.AlphaCompareType0,
                                 material.AlphaReference0);
        var alphaCompareText1 =
            GetAlphaCompareText_(material.AlphaCompareType1,
                                 material.AlphaReference1);

        switch (alphaOpValue) {
          case AlphaOpValue.ONLY_0_REQUIRED: {
            os.WriteLine($@"  if (!({alphaCompareText0})) {{
    discard;
  }}");
            break;
          }
          case AlphaOpValue.ONLY_1_REQUIRED: {
            os.WriteLine($@"  if (!({alphaCompareText1})) {{
    discard;
  }}");
            break;
          }
          case AlphaOpValue.BOTH_REQUIRED: {
            switch (material.AlphaOp) {
              case AlphaOp.And: {
                os.Write(
                    $"  if (!({alphaCompareText0} && {alphaCompareText1})");
                break;
              }
              case AlphaOp.Or: {
                os.Write(
                    $"  if (!({alphaCompareText0} || {alphaCompareText1})");
                break;
              }
              case AlphaOp.XOR: {
                os.WriteLine($"  bool a = {alphaCompareText0};");
                os.WriteLine($"  bool b = {alphaCompareText1};");
                os.Write(
                    $"  if (!(any(bvec2(all(bvec2(!a, b)), all(bvec2(a, !b)))))");
                break;
              }
              case AlphaOp.XNOR: {
                os.WriteLine($"  bool a = {alphaCompareText0};");
                os.WriteLine($"  bool b = {alphaCompareText1};");
                os.Write(
                    "  if (!(any(bvec2(all(bvec2(!a, !b)), all(bvec2(a, b)))))");
                break;
              }
              default: throw new ArgumentOutOfRangeException();
            }
            os.WriteLine(@") {
    discard;
  }");
            break;
          }
          case AlphaOpValue.ALWAYS_FALSE: {
            os.WriteLine("  discard;");
            break;
          }
          default: throw new ArgumentOutOfRangeException();
        }
      }

      os.WriteLine("}");
    }

    private string GetAlphaCompareText_(
        AlphaCompareType alphaCompareType,
        float reference)
      => alphaCompareType switch {
          AlphaCompareType.Never   => "false",
          AlphaCompareType.Less    => $"fragColor.a < {reference}",
          AlphaCompareType.Equal   => $"fragColor.a == {reference}",
          AlphaCompareType.LEqual  => $"fragColor.a <= {reference}",
          AlphaCompareType.Greater => $"fragColor.a > {reference}",
          AlphaCompareType.NEqual  => $"fragColor.a != {reference}",
          AlphaCompareType.GEqual  => $"fragColor.a >= {reference}",
          AlphaCompareType.Always  => "true",
          _ => throw new ArgumentOutOfRangeException(
                   nameof(alphaCompareType), alphaCompareType, null)
      };

    private enum AlphaOpValue {
      ONLY_0_REQUIRED,
      ONLY_1_REQUIRED,
      BOTH_REQUIRED,
      ALWAYS_TRUE,
      ALWAYS_FALSE,
    }

    private AlphaOpValue DetermineAlphaOpValue(
        AlphaOp alphaOp,
        AlphaCompareValue compareValue0,
        AlphaCompareValue compareValue1) {
      var is0False = compareValue0 == AlphaCompareValue.ALWAYS_FALSE;
      var is0True = compareValue0 == AlphaCompareValue.ALWAYS_TRUE;
      var is1False = compareValue1 == AlphaCompareValue.ALWAYS_FALSE;
      var is1True = compareValue1 == AlphaCompareValue.ALWAYS_TRUE;

      if (alphaOp == AlphaOp.And) {
        if (is0False || is1False) {
          return AlphaOpValue.ALWAYS_FALSE;
        }

        if (is0True && is1True) {
          return AlphaOpValue.ALWAYS_TRUE;
        }
        if (is0True) {
          return AlphaOpValue.ONLY_1_REQUIRED;
        }
        if (is1True) {
          return AlphaOpValue.ONLY_0_REQUIRED;
        }
        return AlphaOpValue.BOTH_REQUIRED;
      }

      if (alphaOp == AlphaOp.Or) {
        if (is0True || is1True) {
          return AlphaOpValue.ALWAYS_TRUE;
        }

        if (is0False && is1False) {
          return AlphaOpValue.ALWAYS_FALSE;
        }
        if (is0False) {
          return AlphaOpValue.ONLY_1_REQUIRED;
        }
        if (is1False) {
          return AlphaOpValue.ONLY_0_REQUIRED;
        }
        return AlphaOpValue.BOTH_REQUIRED;
      }

      return AlphaOpValue.BOTH_REQUIRED;
    }

    private enum AlphaCompareValue {
      INDETERMINATE,
      ALWAYS_TRUE,
      ALWAYS_FALSE,
    }

    private AlphaCompareValue DetermineAlphaCompareType(
        AlphaCompareType compareType,
        float reference) {
      var isReference0 = Math.Abs(reference - 0) < .001;
      var isReference1 = Math.Abs(reference - 1) < .001;

      if (compareType == AlphaCompareType.Always ||
          (compareType == AlphaCompareType.GEqual && isReference0) ||
          (compareType == AlphaCompareType.LEqual && isReference1)) {
        return AlphaCompareValue.ALWAYS_TRUE;
      }

      if (compareType == AlphaCompareType.Never ||
          (compareType == AlphaCompareType.Greater && isReference1) ||
          (compareType == AlphaCompareType.Less && isReference0)) {
        return AlphaCompareValue.ALWAYS_FALSE;
      }

      return AlphaCompareValue.INDETERMINATE;
    }

    private void PrintScalarValue_(
        StringWriter os,
        IScalarValue value,
        bool wrapExpressions = false) {
      if (value is IScalarExpression expression) {
        if (wrapExpressions) {
          os.Write("(");
        }
        this.PrintScalarExpression_(os, expression);
        if (wrapExpressions) {
          os.Write(")");
        }
      } else if (value is IScalarTerm term) {
        this.PrintScalarTerm_(os, term);
      } else if (value is IScalarFactor factor) {
        this.PrintScalarFactor_(os, factor);
      } else {
        Asserts.Fail("Unsupported value type!");
      }
    }

    private void PrintScalarExpression_(
        StringWriter os,
        IScalarExpression expression) {
      var terms = expression.Terms;

      for (var i = 0; i < terms.Count; ++i) {
        var term = terms[i];

        if (i > 0) {
          os.Write(" + ");
        }
        this.PrintScalarValue_(os, term);
      }
    }

    private void PrintScalarTerm_(
        StringWriter os,
        IScalarTerm scalarTerm) {
      var numerators = scalarTerm.NumeratorFactors;
      var denominators = scalarTerm.DenominatorFactors;

      if (numerators.Count > 0) {
        for (var i = 0; i < numerators.Count; ++i) {
          var numerator = numerators[i];

          if (i > 0) {
            os.Write("*");
          }

          this.PrintScalarValue_(os, numerator, true);
        }
      } else {
        os.Write(1);
      }

      if (denominators != null) {
        for (var i = 0; i < denominators.Count; ++i) {
          var denominator = denominators[i];

          os.Write("/");

          this.PrintScalarValue_(os, denominator, true);
        }
      }
    }

    private void PrintScalarFactor_(
        StringWriter os,
        IScalarFactor factor) {
      if (factor is IScalarNamedValue<FixedFunctionSource> namedValue) {
        this.PrintScalarNamedValue_(os, namedValue);
      } else if (factor is IScalarConstant constant) {
        this.PrintScalarConstant_(os, constant);
      } else if
          (factor is IColorNamedValueSwizzle<FixedFunctionSource>
           namedSwizzle) {
        this.PrintColorNamedValueSwizzle_(os, namedSwizzle);
      } else if (factor is IColorValueSwizzle swizzle) {
        this.PrintColorValueSwizzle_(os, swizzle);
      } else {
        Asserts.Fail("Unsupported factor type!");
      }
    }

    private void PrintScalarNamedValue_(
        StringWriter os,
        IScalarNamedValue<FixedFunctionSource> namedValue)
      => os.Write(this.GetScalarNamedValue_(namedValue));

    private string GetScalarNamedValue_(
        IScalarNamedValue<FixedFunctionSource> namedValue) {
      var id = namedValue.Identifier;
      var isTextureAlpha = id is >= FixedFunctionSource.TEXTURE_ALPHA_0
                                 and <= FixedFunctionSource.TEXTURE_ALPHA_7;

      if (isTextureAlpha) {
        var textureIndex =
            (int)id - (int)FixedFunctionSource.TEXTURE_ALPHA_0;

        var textureText = this.GetTextureValue_(textureIndex);
        var textureValueText = $"{textureText}.a";

        return textureValueText;
      }

      if (id == FixedFunctionSource.ALL_LIGHTING_MERGED_ALPHA) {
        return "allLightMergedColor.a";
      }
      if (id == FixedFunctionSource.GLOBAL_LIGHTING_MERGED_ALPHA) {
        return "globalLightMergedColor.a";
      }
      if (id == FixedFunctionSource.LOCAL_LIGHTING_MERGED_ALPHA) {
        return "localLightMergedColor.a";
      }
      if (IsInRange_(id,
                     FixedFunctionSource.GLOBAL_LIGHT_0_ALPHA,
                     FixedFunctionSource.GLOBAL_LIGHT_7_ALPHA,
                     out var globalLightAlphaIndex)) {
        return $"individualGlobalLightColors[{globalLightAlphaIndex}].a";
      }
      if (IsInRange_(id,
                     FixedFunctionSource.LOCAL_LIGHT_0_ALPHA,
                     FixedFunctionSource.LOCAL_LIGHT_7_ALPHA,
                     out var localLightAlphaIndex)) {
        return $"individualLocalLightColors[{localLightAlphaIndex}].a";
      }

      return namedValue.Identifier switch {
          FixedFunctionSource.VERTEX_ALPHA_0 => "vertexColor0.a",
          FixedFunctionSource.VERTEX_ALPHA_1 => "vertexColor1.a",

          FixedFunctionSource.UNDEFINED => "1",
          _ => throw new ArgumentOutOfRangeException()
      };
    }

    private void PrintScalarConstant_(
        StringWriter os,
        IScalarConstant constant)
      => os.Write(constant.Value);

    private enum WrapType {
      NEVER,
      EXPRESSIONS,
      ALWAYS
    }

    private void PrintColorValue_(
        StringWriter os,
        IColorValue value,
        WrapType wrapType = WrapType.NEVER) {
      var clamp = value.Clamp;

      if (clamp) {
        os.Write("clamp(");
      }

      if (value is IColorExpression expression) {
        var wrapExpressions =
            wrapType is WrapType.EXPRESSIONS or WrapType.ALWAYS;
        if (wrapExpressions) {
          os.Write("(");
        }
        this.PrintColorExpression_(os, expression);
        if (wrapExpressions) {
          os.Write(")");
        }
      } else if (value is IColorTerm term) {
        var wrapTerms = wrapType == WrapType.ALWAYS;
        if (wrapTerms) {
          os.Write("(");
        }
        this.PrintColorTerm_(os, term);
        if (wrapTerms) {
          os.Write(")");
        }
      } else if (value is IColorFactor factor) {
        var wrapFactors = wrapType == WrapType.ALWAYS;
        if (wrapFactors) {
          os.Write("(");
        }
        this.PrintColorFactor_(os, factor);
        if (wrapFactors) {
          os.Write(")");
        }
      } else if (value is IColorValueTernaryOperator ternaryOperator) {
        this.PrintColorTernaryOperator_(os, ternaryOperator);
      } else {
        Asserts.Fail("Unsupported value type!");
      }

      if (clamp) {
        os.Write(", 0, 1)");
      }
    }

    private void PrintColorExpression_(
        StringWriter os,
        IColorExpression expression) {
      var terms = expression.Terms;

      for (var i = 0; i < terms.Count; ++i) {
        var term = terms[i];

        if (i > 0) {
          os.Write(" + ");
        }
        this.PrintColorValue_(os, term);
      }
    }

    private void PrintColorTerm_(
        StringWriter os,
        IColorTerm scalarTerm) {
      var numerators = scalarTerm.NumeratorFactors;
      var denominators = scalarTerm.DenominatorFactors;

      if (numerators.Count > 0) {
        for (var i = 0; i < numerators.Count; ++i) {
          var numerator = numerators[i];

          if (i > 0) {
            os.Write("*");
          }

          this.PrintColorValue_(os, numerator, WrapType.EXPRESSIONS);
        }
      } else {
        os.Write(1);
      }

      if (denominators != null) {
        for (var i = 0; i < denominators.Count; ++i) {
          var denominator = denominators[i];

          os.Write("/");

          this.PrintColorValue_(os, denominator, WrapType.EXPRESSIONS);
        }
      }
    }

    private void PrintColorFactor_(
        StringWriter os,
        IColorFactor factor) {
      if (factor is IColorNamedValue<FixedFunctionSource> namedValue) {
        this.PrintColorNamedValue_(os, namedValue);
      } else {
        var useIntensity = factor.Intensity != null;

        if (!useIntensity) {
          var r = factor.R;
          var g = factor.G;
          var b = factor.B;

          os.Write("vec3(");
          this.PrintScalarValue_(os, r);
          os.Write(",");
          this.PrintScalarValue_(os, g);
          os.Write(",");
          this.PrintScalarValue_(os, b);
          os.Write(")");
        } else {
          os.Write("vec3(");
          this.PrintScalarValue_(os, factor.Intensity!);
          os.Write(")");
        }
      }
    }

    private void PrintColorNamedValue_(
        StringWriter os,
        IColorNamedValue<FixedFunctionSource> namedValue)
      => os.Write(this.GetColorNamedValue_(namedValue));

    private string GetColorNamedValue_(
        IColorNamedValue<FixedFunctionSource> namedValue) {
      var id = namedValue.Identifier;
      var isTextureColor = id is >= FixedFunctionSource.TEXTURE_COLOR_0
                                 and <= FixedFunctionSource.TEXTURE_COLOR_7;
      var isTextureAlpha = id is >= FixedFunctionSource.TEXTURE_ALPHA_0
                                 and <= FixedFunctionSource.TEXTURE_ALPHA_7;

      if (isTextureColor || isTextureAlpha) {
        var textureIndex =
            isTextureColor
                ? (int)id - (int)FixedFunctionSource.TEXTURE_COLOR_0
                : (int)id - (int)FixedFunctionSource.TEXTURE_ALPHA_0;

        var textureText = this.GetTextureValue_(textureIndex);
        var textureValueText = isTextureColor
                                   ? $"{textureText}.rgb"
                                   : $"vec3({textureText}.a)";

        return textureValueText;
      }

      if (id == FixedFunctionSource.ALL_LIGHTING_MERGED_COLOR) {
        return "allLightMergedColor.rgb";
      }
      if (id == FixedFunctionSource.ALL_LIGHTING_MERGED_ALPHA) {
        return "allLightMergedColor.aaa";
      }

      if (id == FixedFunctionSource.GLOBAL_LIGHTING_MERGED_COLOR) {
        return "globalLightMergedColor.rgb";
      }
      if (id == FixedFunctionSource.GLOBAL_LIGHTING_MERGED_ALPHA) {
        return "globalLightMergedColor.aaa";
      }

      if (id == FixedFunctionSource.LOCAL_LIGHTING_MERGED_COLOR) {
        return "localLightMergedColor.rgb";
      }
      if (id == FixedFunctionSource.LOCAL_LIGHTING_MERGED_ALPHA) {
        return "localLightMergedColor.aaa";
      }

      if (IsInRange_(id,
                     FixedFunctionSource.GLOBAL_LIGHT_0_COLOR,
                     FixedFunctionSource.GLOBAL_LIGHT_7_COLOR,
                     out var globalLightColorIndex)) {
        return $"individualGlobalLightColors[{globalLightColorIndex}].rgb";
      }
      if (IsInRange_(id,
                     FixedFunctionSource.GLOBAL_LIGHT_0_ALPHA,
                     FixedFunctionSource.GLOBAL_LIGHT_7_ALPHA,
                     out var globalLightAlphaIndex)) {
        return $"individualGlobalLightColors[{globalLightAlphaIndex}].aaa";
      }

      if (IsInRange_(id,
                     FixedFunctionSource.LOCAL_LIGHT_0_COLOR,
                     FixedFunctionSource.LOCAL_LIGHT_7_COLOR,
                     out var localLightColorIndex)) {
        return $"individualLocalLightColors[{localLightColorIndex}].rgb";
      }
      if (IsInRange_(id,
                     FixedFunctionSource.LOCAL_LIGHT_0_ALPHA,
                     FixedFunctionSource.LOCAL_LIGHT_7_ALPHA,
                     out var localLightAlphaIndex)) {
        return $"individualLocalLightColors[{localLightAlphaIndex}].aaa";
      }

      return namedValue.Identifier switch {
          FixedFunctionSource.VERTEX_COLOR_0 => "vertexColor0.rgb",
          FixedFunctionSource.VERTEX_COLOR_1 => "vertexColor1.rgb",

          FixedFunctionSource.VERTEX_ALPHA_0 => "vertexColor0.aaa",
          FixedFunctionSource.VERTEX_ALPHA_1 => "vertexColor1.aaa",

          FixedFunctionSource.UNDEFINED => "vec3(1)",
          _ => throw new ArgumentOutOfRangeException()
      };
    }

    private bool IsInRange_(FixedFunctionSource value,
                            FixedFunctionSource min,
                            FixedFunctionSource max,
                            out int relative) {
      relative = value - min;
      return value >= min && value <= max;
    }

    private string GetTextureValue_(int textureIndex) {
      var texture = this.textures_[textureIndex];

      var uvText = texture?.UvType switch {
          UvType.NORMAL    => $"uv{texture.UvIndex}",
          UvType.SPHERICAL => "asin(normalUv) / 3.14159 + 0.5",
          UvType.LINEAR    => "acos(normalUv) / 3.14159",
          _                => throw new ArgumentOutOfRangeException()
      };

      var textureText = $"texture(texture{textureIndex}, {uvText})";
      return textureText;
    }

    private void PrintColorTernaryOperator_(
        StringWriter os,
        IColorValueTernaryOperator ternaryOperator) {
      os.Write('(');
      switch (ternaryOperator.ComparisonType) {
        case BoolComparisonType.EQUAL_TO: {
          os.Write("abs(");
          this.PrintScalarValue_(os, ternaryOperator.Lhs);
          os.Write(" - ");
          this.PrintScalarValue_(os, ternaryOperator.Rhs);
          os.Write(")");
          os.Write(" < ");
          os.Write("(1.0 / 255)");
          break;
        }
        case BoolComparisonType.GREATER_THAN: {
          this.PrintScalarValue_(os, ternaryOperator.Lhs);
          os.Write(" > ");
          this.PrintScalarValue_(os, ternaryOperator.Rhs);
          break;
        }
        default:
          throw new ArgumentOutOfRangeException(
              nameof(ternaryOperator.ComparisonType));
      }
      os.Write(" ? ");
      this.PrintColorValue_(os, ternaryOperator.TrueValue);
      os.Write(" : ");
      this.PrintColorValue_(os, ternaryOperator.FalseValue);
      os.Write(')');
    }

    private void PrintColorNamedValueSwizzle_(
        StringWriter os,
        IColorNamedValueSwizzle<FixedFunctionSource> swizzle) {
      this.PrintColorNamedValue_(os, swizzle.Source);
      os.Write(".");
      os.Write(swizzle.SwizzleType switch {
          ColorSwizzle.R => 'r',
          ColorSwizzle.G => 'g',
          ColorSwizzle.B => 'b',
      });
    }

    private void PrintColorValueSwizzle_(
        StringWriter os,
        IColorValueSwizzle swizzle) {
      this.PrintColorValue_(os, swizzle.Source, WrapType.ALWAYS);
      os.Write(".");
      os.Write(swizzle.SwizzleType);
    }
  }
}