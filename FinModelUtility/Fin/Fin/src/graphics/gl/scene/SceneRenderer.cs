﻿using System;
using System.Collections.Generic;
using System.Linq;

using fin.scene;

namespace fin.graphics.gl.scene {
  public class SceneRenderer : IRenderable, IDisposable {
    public SceneRenderer(IScene scene) {
      this.AreaRenderers
          = scene.Areas
                 .Select(area => new SceneAreaRenderer(area))
                 .ToArray();
    }

    ~SceneRenderer() => this.ReleaseUnmanagedResources_();

    public void Dispose() {
      this.ReleaseUnmanagedResources_();
      GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources_() {
      foreach (var areaRenderer in this.AreaRenderers) {
        areaRenderer.Dispose();
      }
    }

    public IReadOnlyList<SceneAreaRenderer> AreaRenderers { get; }

    public void Render() {
      foreach (var areaRenderer in this.AreaRenderers) {
        areaRenderer.Render();
      }
    }
  }
}