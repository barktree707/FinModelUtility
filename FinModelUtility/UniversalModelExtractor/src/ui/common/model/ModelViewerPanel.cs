﻿using fin.animation.playback;
using fin.gl.model;
using fin.io.bundles;
using fin.model;
using fin.scene;


namespace uni.ui.common.model {
  public partial class ModelViewerPanel : UserControl, IModelViewerPanel {
    public ModelViewerPanel() {
      this.InitializeComponent();
    }

    public (IFileBundle, IScene)? FileBundleAndScene {
      get => this.impl_.FileBundleAndScene;
      set {
        var fileBundle = value?.Item1;
        if (fileBundle != null) {
          this.groupBox_.Text = fileBundle.FullPath;
        } else {
          this.groupBox_.Text = "(Select a model)";
        }

        this.impl_.FileBundleAndScene = value;
      }
    }

    public IAnimationPlaybackManager? AnimationPlaybackManager 
      => this.impl_.AnimationPlaybackManager;

    public ISkeletonRenderer? SkeletonRenderer => this.impl_.SkeletonRenderer;

    public IAnimation? Animation {
      get => this.impl_.Animation;
      set => this.impl_.Animation = value;
    }
  }
}