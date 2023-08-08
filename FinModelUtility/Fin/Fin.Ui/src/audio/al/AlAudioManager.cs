﻿using OpenTK.Audio;


namespace fin.ui.audio.al {
  public partial class AlAudioManager : IAudioManager<short> {
    private readonly AudioContext context_ = new();

    public void Dispose() {
      this.ReleaseUnmanagedResources_();
      GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources_() => this.context_.Dispose();
  }
}