﻿using NUnit.Framework;


namespace schema.text {
  internal class NamespaceGeneratorTests {
    [Test]
    public void TestFromSameNamespace() {
      SchemaTestUtil.AssertGenerated(@"
using schema;

namespace foo.bar {
  public enum A : byte {
  }

  [Schema]
  public partial class Wrapper : IBiSerializable {
    public A Field { get; set; }
  }
}",
                                     @"using System;
using System.IO;
namespace foo.bar {
  public partial class Wrapper {
    public void Read(EndianBinaryReader er) {
      this.Field = (A) er.ReadByte();
    }
  }
}
",
                                     @"using System;
using System.IO;
namespace foo.bar {
  public partial class Wrapper {
    public void Write(EndianBinaryWriter ew) {
      ew.WriteByte((byte) this.Field);
    }
  }
}
");
    }

    [Test]
    public void TestFromHigherNamespace() {
      SchemaTestUtil.AssertGenerated(@"
using schema;

namespace foo {
  public enum A : byte {
  }

  namespace bar {
    [Schema]
    public partial class Wrapper : IBiSerializable {
      public A Field { get; set; }
    }
  }
}",
                                     @"using System;
using System.IO;
namespace foo.bar {
  public partial class Wrapper {
    public void Read(EndianBinaryReader er) {
      this.Field = (A) er.ReadByte();
    }
  }
}
",
                                     @"using System;
using System.IO;
namespace foo.bar {
  public partial class Wrapper {
    public void Write(EndianBinaryWriter ew) {
      ew.WriteByte((byte) this.Field);
    }
  }
}
");
    }

    [Test]
    public void TestFromLowerNamespace() {
      SchemaTestUtil.AssertGenerated(@"
using schema;

namespace foo.bar {
  namespace goo {
    public enum A : byte {
    }
  }

  [Schema]
  public partial class Wrapper : IBiSerializable {
    public goo.A Field { get; set; }
  }
}",
                                     @"using System;
using System.IO;
namespace foo.bar {
  public partial class Wrapper {
    public void Read(EndianBinaryReader er) {
      this.Field = (goo.A) er.ReadByte();
    }
  }
}
",
                                     @"using System;
using System.IO;
namespace foo.bar {
  public partial class Wrapper {
    public void Write(EndianBinaryWriter ew) {
      ew.WriteByte((byte) this.Field);
    }
  }
}
");
    }
  }
}