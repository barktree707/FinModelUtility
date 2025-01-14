﻿using cmb.api;

using dat.api;

using fin.model;
using fin.model.io;
using fin.model.io.importers;

using glo.api;

using hw.api;

using jsystem.api;

using level5.api;

using mod.api;

using modl.api;

using pmdc.api;

using UoT.api;

using visceral.api;

using xmod.api;

namespace uni.api {
  public class GlobalModelImporter : IModelImporter<IModelFileBundle> {
    public IModel ImportModel(IModelFileBundle modelFileBundle)
      => modelFileBundle switch {
          IBattalionWarsModelFileBundle battalionWarsModelFileBundle
              => new BattalionWarsModelImporter().ImportModel(
                  battalionWarsModelFileBundle),
          BmdModelFileBundle bmdModelFileBundle
              => new BmdModelImporter().ImportModel(bmdModelFileBundle),
          CmbModelFileBundle cmbModelFileBundle
              => new CmbModelImporter().ImportModel(cmbModelFileBundle),
          DatModelFileBundle datModelFileBundle
              => new DatModelImporter().ImportModel(datModelFileBundle),
          GeoModelFileBundle geoModelFileBundle
              => new GeoModelImporter().ImportModel(geoModelFileBundle),
          GloModelFileBundle gloModelFileBundle
              => new GloModelImporter().ImportModel(gloModelFileBundle),
          IHaloWarsModelFileBundle haloWarsModelFileBundle
              => new HaloWarsModelImporter().ImportModel(
                  haloWarsModelFileBundle),
          ModModelFileBundle modModelFileBundle
              => new ModModelImporter().ImportModel(modModelFileBundle),
          OmdModelFileBundle omdModelFileBundle
              => new OmdModelImporter().ImportModel(omdModelFileBundle),
          OotModelFileBundle ootModelFileBundle
              => new OotModelImporter().ImportModel(ootModelFileBundle),
          PedModelFileBundle pedModelFileBundle
              => new PedModelImporter().ImportModel(pedModelFileBundle),
          XcModelFileBundle xcModelFileBundle
              => new XcModelImporter().ImportModel(xcModelFileBundle),
          XmodModelFileBundle xmodModelFileBundle
              => new XmodModelImporter().ImportModel(xmodModelFileBundle),
          _ => throw new ArgumentOutOfRangeException(nameof(modelFileBundle))
      };
  }
}