﻿using System.Runtime.CompilerServices;

namespace fin.model {
  public partial class ConsistentVertexAccessor {
    private sealed class NullNormalAccessor : BAccessor, IVertexNormalAccessor {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public void Target(IReadOnlyVertex vertex) { }

      public Normal? LocalNormal => null;
    }
  }
}