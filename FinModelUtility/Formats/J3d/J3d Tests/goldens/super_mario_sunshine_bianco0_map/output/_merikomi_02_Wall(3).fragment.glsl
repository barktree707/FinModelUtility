# version 400


struct Texture {
  sampler2D sampler;
  vec2 clampMin;
  vec2 clampMax;
  mat2x3 transform;
};
uniform Texture texture0;
uniform vec3 color_GxColor2;
uniform float scalar_GxAlpha2;

in vec4 vertexColor0;
in vec4 vertexColor1;
in vec2 uv0;

out vec4 fragColor;

void main() {
  vec3 colorComponent = clamp(color_GxColor2, 0, 1);

  float alphaComponent = scalar_GxAlpha2*texture(texture0.sampler, clamp((texture0.transform * uv0).xy, texture0.clampMin, texture0.clampMax)).a;

  fragColor = vec4(colorComponent, alphaComponent);
}
