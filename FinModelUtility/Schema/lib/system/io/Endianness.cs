﻿// Decompiled with JetBrains decompiler
// Type: System.IO.Endianness
// Assembly: MKDS Course Modifier, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: DAEF8B62-698B-42D0-BEDD-3770EB8C9FE8
// Assembly location: R:\Documents\CSharpWorkspace\Pikmin2Utility\MKDS Course Modifier\MKDS Course Modifier.exe

using System.Collections.Generic;


namespace System.IO {
  public enum EndiannessSource {
    STREAM,
    STRUCTURE,
    MEMBER
  }

  public enum Endianness {
    BigEndian,
    LittleEndian,
  }

  public static class EndiannessUtil {
    public static Endianness SystemEndianness
      => BitConverter.IsLittleEndian
          ? Endianness.LittleEndian
          : Endianness.BigEndian;
  }

  public interface IEndiannessStack {
    Endianness Endianness { get; }

    bool IsOppositeEndiannessOfSystem { get; }

    void PushStructureEndianness(Endianness endianness);

    void PushMemberEndianness(Endianness endianness);

    void PopEndianness();
  }

  public class EndiannessStackImpl : IEndiannessStack {
    private readonly Stack<(EndiannessSource, Endianness)?> endiannessStack_ =
        new();

    public EndiannessStackImpl(Endianness? streamEndianness) {
      this.endiannessStack_.Push(
          streamEndianness != null
              ? (EndiannessSource.STREAM, streamEndianness.Value)
              : null);
      this.UpdateReverse_();
    }

    public Endianness Endianness
      => this.endiannessStack_.Peek()?.Item2 ?? EndiannessUtil.SystemEndianness;

    public bool IsOppositeEndiannessOfSystem { get; private set; }

    /// <summary>
    ///   Pushes a structure's endianness. This will override the reader/writer
    ///   endianness, but will be overwritten by the field endianness if those
    ///   were already pushed.
    /// </summary>
    public void PushStructureEndianness(Endianness endianness) {
      this.endiannessStack_.Push(
          PickSuperior_(
              this.endiannessStack_.Peek(),
              (EndiannessSource.STRUCTURE, endianness)));
      this.UpdateReverse_();
    }
    
    /// <summary>
    ///   Pushes a field's endianness. This will override any other
    ///   endiannesses that were previously pushed.
    /// </summary>
    /// <param name="endianness"></param>
    public void PushMemberEndianness(Endianness endianness) {
      this.endiannessStack_.Push((EndiannessSource.MEMBER, endianness));
      this.UpdateReverse_();
    }

    public void PopEndianness() {
      this.endiannessStack_.Pop();
      this.UpdateReverse_();
    }

    private void UpdateReverse_() {
      IsOppositeEndiannessOfSystem =
          this.Endianness != EndiannessUtil.SystemEndianness;
    }

    private static (EndiannessSource, Endianness)? PickSuperior_(
        (EndiannessSource, Endianness)? prev,
        (EndiannessSource, Endianness) next) {
      if (prev == null) {
        return next;
      }

      var prevSource = prev.Value.Item1;
      var nextSource = next.Item1;
      if (nextSource >= prevSource) {
        return next;
      }

      return prev;
    }
  }
}