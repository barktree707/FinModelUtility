﻿using ast.schema;

using fin.audio;
using fin.audio.io.importers;
using fin.io;

using schema.binary;

namespace ast.api {
  public class AstAudioReader : IAudioImporter<AstAudioFileBundle> {
    public IAudioBuffer<short> ImportAudio(
        IAudioManager<short> audioManager,
        AstAudioFileBundle audioFileBundle) {
      var astFile = audioFileBundle.AstFile;
      var ast = astFile.ReadNew<Ast>(Endianness.BigEndian);

      var mutableBuffer = audioManager.CreateAudioBuffer();

      mutableBuffer.Frequency = (int) ast.StrmHeader.SampleRate;

      var channelData =
          ast.ChannelData.Select(data => data.ToArray()).ToArray();
      mutableBuffer.SetPcm(channelData);

      return mutableBuffer;
    }
  }
}