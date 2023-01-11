﻿using fin.color;
using System.Collections.Generic;


namespace fin.model {
  public interface ILighting {
    IReadOnlyList<ILight> Lights { get; }
    ILight CreateLight();
  }

  public enum AttenuationFunction {
    NONE,
    SPECULAR,
    SPOT,
  }

  public enum DiffuseFunction {
    NONE,
    SIGNED,
    CLAMP,
  }

  public interface ILight {
    string Name { get; }
    ILight SetName(string name);
    
    IPosition Position { get; }
    ILight SetPosition(IPosition position);

    INormal Normal { get; }
    ILight SetNormal(INormal normal);

    IColor Color { get; }
    ILight SetColor(IColor color);

    IVector3 CosineAttenuation { get; }
    ILight SetCosineAttenuation(IVector3 cosineAttenuation);
    IVector3 DistanceAttenuation { get; }
    ILight SetDistanceAttenuation(IVector3 distanceAttenuation);

    AttenuationFunction AttenuationFunction { get; }
    ILight SetAttenuationFunction(AttenuationFunction attenuationFunction);
    DiffuseFunction DiffuseFunction { get; }
    ILight SetDiffuseFunction(DiffuseFunction diffuseFunction);
  }
}