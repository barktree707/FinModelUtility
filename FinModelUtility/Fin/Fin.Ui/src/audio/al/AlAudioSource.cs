﻿namespace fin.ui.audio.al {
  public partial class AlAudioManager {
    public IAudioSource<short> CreateAudioSource() => new AlAudioSource(this);

    private partial class AlAudioSource : IAudioSource<short> {
      private readonly AlAudioManager manager_;

      public AlAudioSource(AlAudioManager manager) {
        this.manager_ = manager;
      }
    }
  }
}