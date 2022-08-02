﻿using System;

using schema.memory;


namespace schema.attributes.memory {
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
  public class BlockAttribute : BMemberAttribute {
    private readonly string? parentBlockName_;
    private readonly string readOffsetName_;
    private readonly string readSizeName_;

    public BlockAttribute(string readOffsetName, string readSizeName) {
      this.readOffsetName_ = readOffsetName;
      this.readSizeName_ = readSizeName;
    }

    public BlockAttribute(
        string parentBlockName,
        string readOffsetName,
        string readSizeName) {
      this.parentBlockName_ = parentBlockName;
      this.readOffsetName_ = readOffsetName;
      this.readSizeName_ = readSizeName;
    }

    protected override void InitFields() {
      if (this.parentBlockName_ != null) {
        this.ParentBlock =
            this.GetMemberRelativeToStructure<IMemoryBlock>(
                this.parentBlockName_);
      }

      this.ReadOffset =
          this.GetMemberRelativeToStructure(this.readOffsetName_)
              .AssertIsInteger();
      this.ReadSize =
          this.GetMemberRelativeToStructure(this.readSizeName_)
              .AssertIsInteger();
    }

    public IMemberReference<IMemoryBlock>? ParentBlock { get; private set; }
    public IMemberReference? ReadOffset { get; private set; }
    public IMemberReference? ReadSize { get; private set; }
  }

  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
  public class PointerAttribute : Attribute {
    public PointerAttribute(string memoryBlockName, string readOffsetName) {
      this.MemoryBlockName = memoryBlockName;
      this.ReadOffsetName = readOffsetName;
    }

    public string MemoryBlockName { get; }
    public string ReadOffsetName { get; }
  }
}