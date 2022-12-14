﻿// Decompiled with JetBrains decompiler
// Type: MKDS_Course_Modifier.GCN.BCK
// Assembly: MKDS Course Modifier, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: DAEF8B62-698B-42D0-BEDD-3770EB8C9FE8
// Assembly location: R:\Documents\CSharpWorkspace\Pikmin2Utility\MKDS Course Modifier\MKDS Course Modifier.exe

using bmd.G3D_Binary_File_Format;
using System;
using System.IO;
using System.Text;
using fin.util.asserts;
using schema;
using schema.attributes.endianness;
using schema.attributes.size;


namespace bmd.schema.bcx {
  /// <summary>
  ///   BCK files define joint animations with sparse keyframes.
  ///
  ///   https://wiki.cloudmodding.com/tww/BCK
  /// </summary>
  [Endianness(Endianness.BigEndian)]
  public partial class Bck : IBcx {
    public BckHeader Header;
    public ANK1Section ANK1;

    public Bck(byte[] file) {
      using EndianBinaryReader er =
          new EndianBinaryReader((Stream)new MemoryStream(file),
                                 Endianness.BigEndian);
      this.Header = er.ReadNew<BckHeader>();
      this.ANK1 = new Bck.ANK1Section(er, out _);
    }

    public IAnx1 Anx1 => this.ANK1;

    [BinarySchema]
    public partial class BckHeader : IBiSerializable {
      private readonly string magic_ = "J3D1bck1";

      [SizeOfStreamInBytes]
      private uint fileSize_;

      private readonly uint sectionCount_ = 1;

      [ArrayLengthSource(16)]
      private byte[] padding_;
    }

    public partial class ANK1Section : IAnx1 {
      public const string Signature = "ANK1";
      public DataBlockHeader Header;
      public byte LoopFlags;
      public byte AngleMultiplier;
      public ushort AnimLength;
      public ushort NrJoints;
      public ushort NrScale;
      public ushort NrRot;
      public ushort NrTrans;
      public uint JointOffset;
      public uint ScaleOffset;
      public uint RotOffset;
      public uint TransOffset;
      public float[] Scale;
      public short[] Rotation;
      public float[] Translation;

      public ANK1Section(EndianBinaryReader er, out bool OK) {
        bool OK1;
        this.Header = new DataBlockHeader(er, "ANK1", out OK1);
        if (!OK1) {
          OK = false;
        } else {
          this.LoopFlags = er.ReadByte();
          this.AngleMultiplier = er.ReadByte();
          this.AnimLength = er.ReadUInt16();
          this.NrJoints = er.ReadUInt16();
          this.NrScale = er.ReadUInt16();
          this.NrRot = er.ReadUInt16();
          this.NrTrans = er.ReadUInt16();
          this.JointOffset = er.ReadUInt32();
          this.ScaleOffset = er.ReadUInt32();
          this.RotOffset = er.ReadUInt32();
          this.TransOffset = er.ReadUInt32();
          er.Position = (long)(32U + this.ScaleOffset);
          this.Scale = er.ReadSingles((int)this.NrScale);
          er.Position = (long)(32U + this.RotOffset);
          this.Rotation = er.ReadInt16s((int)this.NrRot);
          er.Position = (long)(32U + this.TransOffset);
          this.Translation = er.ReadSingles((int)this.NrTrans);
          float RotScale =
              (float)(Math.Pow(2.0, (double)this.AngleMultiplier) *
                      Math.PI /
                      32768.0);
          er.Position = (long)(32U + this.JointOffset);
          this.Joints = new AnimatedJoint[(int)this.NrJoints];
          for (int index = 0; index < (int)this.NrJoints; ++index) {
            var animatedJoint = new AnimatedJoint(er);
            animatedJoint.SetValues(this.Scale,
                                    this.Rotation,
                                    this.Translation,
                                    RotScale);
            this.Joints[index] = animatedJoint;
          }
          OK = true;
        }
      }

      public int FrameCount => this.AnimLength;
      public IAnimatedJoint[] Joints { get; }


      public partial class AnimatedJoint : IAnimatedJoint {
        public AnimComponent X;
        public AnimComponent Y;
        public AnimComponent Z;

        public AnimatedJoint(EndianBinaryReader er) {
          this.X = er.ReadNew<AnimComponent>();
          this.Y = er.ReadNew<AnimComponent>();
          this.Z = er.ReadNew<AnimComponent>();
        }

        public IJointAnim Values { get; private set; }

        public void SetValues(
            float[] Scales,
            short[] Rotations,
            float[] Translations,
            float RotScale) {
          this.Values =
              new JointAnim(
                  this,
                  Scales,
                  Rotations,
                  Translations,
                  RotScale);
        }

        private float Interpolate(
            float v1,
            float d1,
            float v2,
            float d2,
            float t) {
          float num1 = (float)(2.0 * ((double)v1 - (double)v2)) + d1 + d2;
          float num2 =
              (float)(-3.0 * (double)v1 +
                      3.0 * (double)v2 -
                      2.0 * (double)d1) -
              d2;
          float num3 = d1;
          float num4 = v1;
          return ((num1 * t + num2) * t + num3) * t + num4;
        }

        public float GetAnimValue(IJointAnimKey[] keys, float t) {
          if (keys.Length == 0)
            return 0.0f;
          if (keys.Length == 1)
            return keys[0].Value;
          int index = 1;

          while ((double)keys[index].Time < (double)t
                 // Don't shoot past the end of the keys list!
                 &&
                 index + 1 < keys.Length)
            ++index;

          if (index + 1 == keys.Length && keys[index].Time < t) {
            return keys[0].Value;
          }

          float t1 = (float)(((double)t - (double)keys[index - 1].Time) /
                             ((double)keys[index].Time -
                              (double)keys[index - 1].Time));


          return this.Interpolate(keys[index - 1].Value,
                                  keys[index - 1].OutgoingTangent,
                                  keys[index].Value,
                                  keys[index].IncomingTangent,
                                  t1);
        }

        [BinarySchema]
        public partial class AnimComponent : IBiSerializable {
          public AnimIndex S { get; } = new();
          public AnimIndex R { get; } = new();
          public AnimIndex T { get; } = new();
        }

        [BinarySchema]
        public partial class AnimIndex : IBiSerializable {
          public ushort Count;
          public ushort Index;
          public ushort TangentMode;
        }

        public class JointAnim : IJointAnim {
          private IJointAnimKey[] scalesX_;
          private IJointAnimKey[] scalesY_;
          private IJointAnimKey[] scalesZ_;
          private IJointAnimKey[] rotationsX_;
          private IJointAnimKey[] rotationsY_;
          private IJointAnimKey[] rotationsZ_;
          private IJointAnimKey[] translationsX_;
          private IJointAnimKey[] translationsY_;
          private IJointAnimKey[] translationsZ_;

          public JointAnim(
              AnimatedJoint Joint,
              float[] Scales,
              short[] Rotations,
              float[] Translations,
              float RotScale) {
            this.SetKeysST(out this.scalesX_, Scales, Joint.X.S);
            this.SetKeysST(out this.scalesY_, Scales, Joint.Y.S);
            this.SetKeysST(out this.scalesZ_, Scales, Joint.Z.S);
            this.SetKeysR(out this.rotationsX_, Rotations, RotScale, Joint.X.R);
            this.SetKeysR(out this.rotationsY_, Rotations, RotScale, Joint.Y.R);
            this.SetKeysR(out this.rotationsZ_, Rotations, RotScale, Joint.Z.R);
            this.SetKeysST(out this.translationsX_, Translations, Joint.X.T);
            this.SetKeysST(out this.translationsY_, Translations, Joint.Y.T);
            this.SetKeysST(out this.translationsZ_, Translations, Joint.Z.T);
          }

          public IJointAnimKey[] scalesX => this.scalesX_;
          public IJointAnimKey[] scalesY => this.scalesY_;
          public IJointAnimKey[] scalesZ => this.scalesZ_;

          public IJointAnimKey[] rotationsX => this.rotationsX_;
          public IJointAnimKey[] rotationsY => this.rotationsY_;
          public IJointAnimKey[] rotationsZ => this.rotationsZ_;

          public IJointAnimKey[] translationsX => this.translationsX_;
          public IJointAnimKey[] translationsY => this.translationsY_;
          public IJointAnimKey[] translationsZ => this.translationsZ_;

          private void SetKeysST(
              out IJointAnimKey[] Destination,
              float[] Source,
              AnimIndex Component) {
            Destination = new IJointAnimKey[(int)Component.Count];
            if (Component.Count <= (ushort)0)
              throw new Exception("Count <= 0");
            if (Component.Count == (ushort)1) {
              Destination[0] =
                  new Key(
                      0.0f,
                      Source[(int)Component.Index],
                      0,
                      0);
            } else {
              var tangentMode = Component.TangentMode;
              var hasTwoTangents = tangentMode == 1;
              Asserts.True(tangentMode == 0 || tangentMode == 1);

              var stride = hasTwoTangents ? 4 : 3;
              for (int index = 0; index < (int)Component.Count; ++index) {
                var i = (int)Component.Index + stride * index;

                var time = Source[i + 0];
                var value = Source[i + 1];

                float incomingTangent, outgoingTangent;
                if (hasTwoTangents) {
                  incomingTangent = Source[i + 2];
                  outgoingTangent = Source[i + 3];
                } else {
                  incomingTangent = outgoingTangent = Source[i + 2];
                }

                Destination[index] =
                    new Key(
                        time,
                        value,
                        incomingTangent,
                        outgoingTangent);
              }
            }
          }

          private void SetKeysR(
              out IJointAnimKey[] Destination,
              short[] Source,
              float RotScale,
              AnimIndex Component) {
            Destination =
                new IJointAnimKey[(int)Component
                    .Count];
            if (Component.Count <= (ushort)0)
              throw new Exception("Count <= 0");
            if (Component.Count == (ushort)1) {
              Destination[0] = new JointAnim.Key(
                  0.0f,
                  (float)Source[(int)Component.Index] * RotScale,
                  0,
                  0);
            } else {
              var tangentMode = Component.TangentMode;
              var hasTwoTangents = tangentMode == 1;
              Asserts.True(tangentMode == 0 || tangentMode == 1);

              var stride = hasTwoTangents ? 4 : 3;
              for (int index = 0; index < (int)Component.Count; ++index) {
                var i = (int)Component.Index + stride * index;

                var time = (float)Source[i + 0];
                var value =
                    (float)Source[i + 1] *
                    RotScale;

                float incomingTangent, outgoingTangent;
                if (hasTwoTangents) {
                  incomingTangent = Source[i + 2] * RotScale;
                  outgoingTangent = Source[i + 3] * RotScale;
                } else {
                  incomingTangent = outgoingTangent = Source[i + 2] * RotScale;
                }

                Destination[index] =
                    new Key(
                        time,
                        value,
                        incomingTangent,
                        outgoingTangent);
              }
            }
          }

          public class Key : IJointAnimKey {
            public Key(
                float Time,
                float Value,
                float incomingTangent,
                float outgoingTangent) {
              this.Time = Time;
              this.Value = Value;
              this.IncomingTangent = incomingTangent;
              this.OutgoingTangent = outgoingTangent;
            }

            public float Time { get; }
            public float Value { get; }
            public float IncomingTangent { get; }
            public float OutgoingTangent { get; }
          }
        }
      }
    }
  }
}