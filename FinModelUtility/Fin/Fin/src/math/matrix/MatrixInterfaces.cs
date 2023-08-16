﻿using System;

namespace fin.math.matrix {
  // The type parameters on these matrices are kind of janky, but they allow us
  // to have consistent interfaces between 3x3 and 4x4 matrices.

  public interface IFinMatrix<TMutable, TReadOnly, TImpl,
                              TPosition, TRotation, TScale>
      : IReadOnlyFinMatrix<TMutable, TReadOnly, TImpl,
          TPosition, TRotation, TScale>
      where TMutable : IFinMatrix<TMutable, TReadOnly, TImpl,
          TPosition, TRotation, TScale>, TReadOnly
      where TReadOnly : IReadOnlyFinMatrix<TMutable, TReadOnly, TImpl,
          TPosition, TRotation, TScale> {
    void CopyFrom(TReadOnly other);
    void CopyFrom(TImpl other);

    TMutable SetIdentity();
    TMutable SetZero();

    new float this[int row, int column] { get; set; }

    TMutable AddInPlace(TReadOnly other);
    TMutable MultiplyInPlace(TReadOnly other);
    TMutable MultiplyInPlace(TImpl other);
    TMutable MultiplyInPlace(float other);

    TMutable InvertInPlace();
  }

  public interface IReadOnlyFinMatrix<TMutable, TReadOnly, TImpl,
                                      TPosition, TRotation, TScale>
      : IEquatable<TReadOnly>
      where TMutable : IFinMatrix<TMutable, TReadOnly, TImpl,
          TPosition, TRotation, TScale>, TReadOnly
      where TReadOnly : IReadOnlyFinMatrix<TMutable, TReadOnly, TImpl,
          TPosition, TRotation, TScale> {
    TImpl Impl { get; }

    TMutable Clone();

    float this[int row, int column] { get; }

    TMutable CloneAndAdd(TReadOnly other);
    void AddIntoBuffer(TReadOnly other, TMutable buffer);

    TMutable CloneAndMultiply(TReadOnly other);

    void MultiplyIntoBuffer(TReadOnly other, TMutable buffer);

    TMutable CloneAndMultiply(TImpl other);
    void MultiplyIntoBuffer(TImpl other, TMutable buffer);

    TMutable CloneAndMultiply(float other);
    void MultiplyIntoBuffer(float other, TMutable buffer);

    TMutable CloneAndInvert();
    void InvertIntoBuffer(TMutable buffer);

    
    void CopyTranslationInto(out TPosition dst);
    void CopyRotationInto(out TRotation dst);
    void CopyScaleInto(out TScale dst);

    void Decompose(out TPosition translation,
                   out TRotation rotation,
                   out TScale scale);
  }
}