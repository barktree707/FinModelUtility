# version 400


struct Light {
  bool enabled;
  vec3 position;
  vec3 normal;
  vec4 color;
};

uniform Light lights[8];


struct Texture {
  sampler2D sampler;
  vec2 clampS;
  vec2 clampT;
  mat2x3 transform;
};
uniform Texture texture0;
uniform sampler2D texture1;
uniform vec3 color_GxMaterialColor0;
uniform vec3 color_GxAmbientColor0;
uniform vec3 color_GxColor0;
uniform float scalar_GxAlpha0;

in vec3 vertexNormal;
in vec4 vertexColor0;
in vec4 vertexColor1;
in vec2 uv0;

out vec4 fragColor;

vec4 getLightColor(Light light) {
  if (!light.enabled) {
    return vec4(0);
  }

  vec3 diffuseLightNormal = normalize(light.normal);
  float diffuseLightAmount = max(-dot(vertexNormal, diffuseLightNormal), 0);
  float lightAmount = min(diffuseLightAmount, 1);
  return lightAmount * light.color;
}

void main() {
  vec4 individualLightColors[8];
  for (int i = 0; i < 8; ++i) {
    vec4 lightColor = getLightColor(lights[i]);
    individualLightColors[i] = lightColor;
  }

  vec3 colorComponent = clamp(texture(texture0.sampler, clamp((texture0.transform * uv0).xy, vec2(texture0.clampS.x, texture0.clampT.x), vec2(texture0.clampS.y, texture0.clampT.y))).rgb*clamp((color_GxColor0 + texture(texture1, uv0).rgb*(vec3(1) + vec3(-1)*vec3(0.625)) + color_GxMaterialColor0*clamp((individualLightColors[0].rgb + color_GxAmbientColor0), 0, 1)*vec3(0.625)), 0, 1), 0, 1);

  float alphaComponent = scalar_GxAlpha0*texture(texture0.sampler, clamp((texture0.transform * uv0).xy, vec2(texture0.clampS.x, texture0.clampT.x), vec2(texture0.clampS.y, texture0.clampT.y))).a;

  fragColor = vec4(colorComponent, alphaComponent);
}
