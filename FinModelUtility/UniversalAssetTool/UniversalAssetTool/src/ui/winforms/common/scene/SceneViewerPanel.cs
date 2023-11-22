﻿using fin.animation;
using fin.io.bundles;
using fin.model;
using fin.scene;
using fin.ui.rendering.gl.model;

namespace uni.ui.winforms.common.scene {
  public partial class SceneViewerPanel : UserControl, ISceneViewerPanel {
    public SceneViewerPanel() {
      this.InitializeComponent();
    }

    public (IFileBundle, IScene, ILighting?)? FileBundleAndSceneAndLighting {
      get => this.impl_.FileBundleAndSceneAndLighting;
      set {
        var fileBundle = value?.Item1;
        if (fileBundle != null) {
          this.groupBox_.Text = fileBundle.DisplayFullPath;
        } else {
          this.groupBox_.Text = "(Select a model)";
        }

        this.impl_.FileBundleAndSceneAndLighting = value;
      }
    }

    public ISceneModel? FirstSceneModel => this.impl_.FirstSceneModel;

    public IAnimationPlaybackManager? AnimationPlaybackManager 
      => this.impl_.AnimationPlaybackManager;

    public ISkeletonRenderer? SkeletonRenderer => this.impl_.SkeletonRenderer;

    public IModelAnimation? Animation {
      get => this.impl_.Animation;
      set => this.impl_.Animation = value;
    }

    public TimeSpan FrameTime => this.impl_.FrameTime;
  }
}