﻿using System.Diagnostics;

using fin.animation.playback;
using fin.gl;
using fin.math;

using Tao.OpenGl;
using Tao.Platform.Windows;

using fin.model;
using fin.model.impl;
using fin.model.util;

using uni.ui.gl;


namespace uni.ui.common {
  public partial class ModelViewerGlPanel : BGlPanel {
    private readonly Camera camera_ = new();
    private float fovY_ = 30;

    private readonly Stopwatch stopwatch_ = Stopwatch.StartNew();
    private readonly Color backgroundColor_ = Color.FromArgb(51, 128, 179);

    private GlShaderProgram texturedShaderProgram_;
    private int texture0Location_;

    private GlShaderProgram texturelessShaderProgram_;

    private ModelRenderer? modelRenderer_;
    private SkeletonRenderer? skeletonRenderer_;
    private readonly BoneTransformManager boneTransformManager_ = new();

    private GridRenderer gridRenderer_ = new();

    private float scale_ = 1;

    public IModel? Model {
      get => this.modelRenderer_?.Model;
      set {
        this.modelRenderer_?.Dispose();
        this.boneTransformManager_.Clear();

        if (value != null) {
          this.modelRenderer_ =
              new ModelRenderer(value, this.boneTransformManager_);
          this.skeletonRenderer_ =
              new SkeletonRenderer(value.Skeleton, this.boneTransformManager_);
          this.boneTransformManager_.CalculateMatrices(
              value.Skeleton.Root,
              value.Skin.BoneWeights,
              null);
          this.scale_ = 1000 / ModelScaleCalculator.CalculateScale(
                            value, this.boneTransformManager_);
        } else {
          this.modelRenderer_ = null;
          this.skeletonRenderer_ = null;
          this.scale_ = 1;
        }

        this.Animation = value?.AnimationManager.Animations.FirstOrDefault();
      }
    }

    public IAnimationPlaybackManager AnimationPlaybackManager { get; set; }

    private IAnimation? animation_;

    public IAnimation? Animation {
      get => this.animation_;
      set {
        if (this.animation_ == value) {
          return;
        }

        this.animation_ = value;
        if (this.AnimationPlaybackManager != null) {
          this.AnimationPlaybackManager.Frame = 0;
          this.AnimationPlaybackManager.FrameRate =
              (int) (value?.FrameRate ?? 20);
          this.AnimationPlaybackManager.TotalFrames =
              value?.FrameCount ?? 0;
        }
      }
    }

    private bool isMouseDown_ = false;
    private (int, int)? prevMousePosition_ = null;

    private bool isForwardDown_ = false;
    private bool isBackwardDown_ = false;
    private bool isLeftwardDown_ = false;
    private bool isRightwardDown_ = false;

    public ModelViewerGlPanel() {
      this.impl_.MouseDown += (sender, args) => {
        if (args.Button == MouseButtons.Left) {
          isMouseDown_ = true;
          this.prevMousePosition_ = null;
        }
      };
      this.impl_.MouseUp += (sender, args) => {
        if (args.Button == MouseButtons.Left) {
          isMouseDown_ = false;
        }
      };
      this.impl_.MouseMove += (sender, args) => {
        if (this.isMouseDown_) {
          var mouseLocation = (args.X, args.Y);

          if (this.prevMousePosition_ != null) {
            var (prevMouseX, prevMouseY) = this.prevMousePosition_.Value;
            var (mouseX, mouseY) = mouseLocation;

            var deltaMouseX = mouseX - prevMouseX;
            var deltaMouseY = mouseY - prevMouseY;

            var fovY = this.fovY_;
            var fovX = fovY / this.Height * this.Width;

            var deltaXFrac = 1f * deltaMouseX / this.Width;
            var deltaYFrac = 1f * deltaMouseY / this.Height;

            var mouseSpeed = 3;

            this.camera_.Pitch -= deltaYFrac * fovY * mouseSpeed;
            this.camera_.Yaw -= deltaXFrac * fovX * mouseSpeed;
          }

          this.prevMousePosition_ = mouseLocation;
        }
      };

      this.impl_.KeyDown += (sender, args) => {
        switch (args.KeyCode) {
          case Keys.W: {
            this.isForwardDown_ = true;
            break;
          }
          case Keys.S: {
            this.isBackwardDown_ = true;
            break;
          }
          case Keys.A: {
            this.isLeftwardDown_ = true;
            break;
          }
          case Keys.D: {
            this.isRightwardDown_ = true;
            break;
          }
        }
      };

      this.impl_.KeyUp += (sender, args) => {
        switch (args.KeyCode) {
          case Keys.W: {
            this.isForwardDown_ = false;
            break;
          }
          case Keys.S: {
            this.isBackwardDown_ = false;
            break;
          }
          case Keys.A: {
            this.isLeftwardDown_ = false;
            break;
          }
          case Keys.D: {
            this.isRightwardDown_ = false;
            break;
          }
        }
      };
    }

    protected override void InitGl() {
      GlUtil.Init();

      var vertexShaderSrc = @"
# version 120

in vec2 in_uv0;

varying vec4 vertexColor;
varying vec3 vertexNormal;
varying vec2 uv0;

void main() {
    gl_Position = gl_ProjectionMatrix * gl_ModelViewMatrix * gl_Vertex; 
    vertexNormal = normalize(gl_ModelViewMatrix * vec4(gl_Normal, 0)).xyz;
    vertexColor = gl_Color;
    uv0 = gl_MultiTexCoord0.st;
}";

      var fragmentShaderSrc = @$"
# version 130 

uniform sampler2D texture0;

out vec4 fragColor;

in vec4 vertexColor;
in vec3 vertexNormal;
in vec2 uv0;

void main() {{
    vec4 texColor = texture(texture0, uv0);

    fragColor = texColor * vertexColor;

    vec3 diffuseLightNormal = normalize(vec3(.5, .5, -1));
    float diffuseLightAmount = {(DebugFlags.ENABLE_LIGHTING ? "max(-dot(vertexNormal, diffuseLightNormal), 0)" : "1")};

    float ambientLightAmount = .3;

    float lightAmount = min(ambientLightAmount + diffuseLightAmount, 1);

    fragColor.rgb *= lightAmount;

    if (fragColor.a < .95) {{
      discard;
    }}
}}";

      this.texturedShaderProgram_ =
          GlShaderProgram.FromShaders(vertexShaderSrc, fragmentShaderSrc);

      this.texture0Location_ =
          Gl.glGetUniformLocation(this.texturedShaderProgram_.ProgramId,
                                  "texture0");

      this.texturelessShaderProgram_ =
          GlShaderProgram.FromShaders(@"
# version 120

varying vec4 vertexColor;

void main() {
    gl_Position = gl_ProjectionMatrix * gl_ModelViewMatrix * gl_Vertex; 
    vertexColor = gl_Color;
}", @"
# version 130 

out vec4 fragColor;

in vec4 vertexColor;

void main() {
    fragColor = vertexColor;
}");

      ResetGl_();
      Wgl.wglSwapIntervalEXT(1);
    }

    private void ResetGl_() {
      Gl.glShadeModel(Gl.GL_SMOOTH);
      Gl.glEnable(Gl.GL_POINT_SMOOTH);
      Gl.glHint(Gl.GL_POINT_SMOOTH_HINT, Gl.GL_NICEST);

      Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);

      Gl.glClearDepth(5.0F);

      Gl.glDepthFunc(Gl.GL_LEQUAL);
      Gl.glEnable(Gl.GL_DEPTH_TEST);
      Gl.glDepthMask(Gl.GL_TRUE);

      Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);

      Gl.glEnable(Gl.GL_LIGHT0);
      Gl.glEnable(Gl.GL_TEXTURE_2D);

      Gl.glEnable(Gl.GL_LIGHTING);
      Gl.glEnable(Gl.GL_NORMALIZE);

      Gl.glEnable(Gl.GL_CULL_FACE);
      Gl.glCullFace(Gl.GL_BACK);

      Gl.glEnable(Gl.GL_BLEND);
      Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

      Gl.glClearColor(backgroundColor_.R / 255f, backgroundColor_.G / 255f,
                      backgroundColor_.B / 255f, 1);
    }

    protected override void RenderGl() {
      var forwardVector =
          (this.isForwardDown_ ? 1 : 0) - (this.isBackwardDown_ ? 1 : 0);
      var rightwardVector =
          (this.isRightwardDown_ ? 1 : 0) - (this.isLeftwardDown_ ? 1 : 0);
      this.camera_.Move(forwardVector, rightwardVector, 15);

      var width = this.Width;
      var height = this.Height;
      Gl.glViewport(0, 0, width, height);

      Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

      Gl.glUniform1i(this.texture0Location_, 0);

      this.RenderPerspective_();
      //this.RenderOrtho_();

      Gl.glFlush();
    }

    private void RenderPerspective_() {
      var width = this.Width;
      var height = this.Height;

      {
        Gl.glMatrixMode(Gl.GL_PROJECTION);
        Gl.glLoadIdentity();
        GlUtil.Perspective(this.fovY_, 1.0 * width / height, .1, 10000);
        GlUtil.LookAt(this.camera_.X, this.camera_.Y, this.camera_.Z,
                      this.camera_.X + this.camera_.XNormal,
                      this.camera_.Y + this.camera_.YNormal,
                      this.camera_.Z + this.camera_.ZNormal, 
                      0, 0, 1);

        Gl.glMatrixMode(Gl.GL_MODELVIEW);
        Gl.glLoadIdentity();
      }

      if (DebugFlags.ENABLE_GRID) {
        this.texturelessShaderProgram_.Use();
        this.gridRenderer_.Render();
      }

      {
        Gl.glRotated(90, 1, 0, 0);
        Gl.glScalef(this.scale_, this.scale_, this.scale_);
      }

      if (this.Animation != null) {
        this.AnimationPlaybackManager.Tick();

        this.boneTransformManager_.CalculateMatrices(
            this.Model.Skeleton.Root,
            this.Model.Skin.BoneWeights,
            (this.Animation, (float) this.AnimationPlaybackManager.Frame),
            this.AnimationPlaybackManager.ShouldLoop);
      }

      this.texturedShaderProgram_.Use();
      this.modelRenderer_?.Render();

      if (DebugFlags.ENABLE_SKELETON) {
        this.texturelessShaderProgram_.Use();
        this.skeletonRenderer_?.Render();
      }
    }

    private void RenderOrtho_() {
      var width = this.Width;
      var height = this.Height;

      {
        Gl.glMatrixMode(Gl.GL_PROJECTION);
        Gl.glLoadIdentity();
        GlUtil.Ortho2d(0, width, height, 0);

        Gl.glMatrixMode(Gl.GL_MODELVIEW);
        Gl.glLoadIdentity();

        Gl.glTranslated(width / 2, height / 2, 0);
      }

      var size = MathF.Max(width, height) * MathF.Sqrt(2);

      Gl.glBegin(Gl.GL_QUADS);

      var t = this.stopwatch_.Elapsed.TotalSeconds;
      var angle = t * 45;

      var color = ColorImpl.FromHsv(angle, 1, 1);
      Gl.glColor3f(color.Rf, color.Gf, color.Bf);

      Gl.glVertex2f(-size / 2, -size / 2);

      Gl.glVertex2f(-size / 2, size / 2);

      Gl.glVertex2f(size / 2, size / 2);

      Gl.glVertex2f(size / 2, -size / 2);

      Gl.glEnd();
    }
  }
}