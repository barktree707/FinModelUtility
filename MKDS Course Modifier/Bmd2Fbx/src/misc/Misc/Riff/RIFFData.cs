﻿// Decompiled with JetBrains decompiler
// Type: MKDS_Course_Modifier.Misc.Riff.RIFFData
// Assembly: MKDS Course Modifier, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: DAEF8B62-698B-42D0-BEDD-3770EB8C9FE8
// Assembly location: R:\Documents\CSharpWorkspace\Pikmin2Utility\MKDS Course Modifier\MKDS Course Modifier.exe

using System.IO;

namespace MKDS_Course_Modifier.Misc.Riff
{
  public class RIFFData
  {
    public virtual string GetSignature()
    {
      return (string) null;
    }

    public virtual void Read(EndianBinaryReader er)
    {
    }

    public virtual void Write(EndianBinaryWriter er)
    {
    }
  }
}