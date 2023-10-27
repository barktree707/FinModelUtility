﻿namespace fin.shaders.glsl {
  public static class GlslConstants {
    public const string UNIFORM_MODEL_VIEW_MATRIX_NAME = "modelViewMatrix";
    public const string UNIFORM_PROJECTION_MATRIX_NAME = "projectionMatrix";
    public const string UNIFORM_BONE_MATRICES_NAME = "boneMatrices";
    public const string UNIFORM_USE_LIGHTING_NAME = "useLighting";

    public const float MIN_ALPHA_BEFORE_DISCARD = .95f;
    public const string MIN_ALPHA_BEFORE_DISCARD_TEXT = ".95";
  }
}
