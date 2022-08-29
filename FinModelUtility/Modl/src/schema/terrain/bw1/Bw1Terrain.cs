﻿using fin.util.asserts;

using schema;


namespace modl.schema.terrain.bw1 {
  public class Bw1Terrain : IBwTerrain, IDeserializable {
    public IBwHeightmap Heightmap { get; private set; }
    public IList<BwHeightmapMaterial> Materials { get; private set; }

    public void Read(EndianBinaryReader er) {
      var sections = new Dictionary<string, BwSection>();
      while (!er.Eof) {
        var name = er.ReadStringEndian(4);
        var size = er.ReadInt32();
        var offset = er.Position;

        sections[name] = new BwSection(name, size, offset);

        er.Position += size;
      }

      // TODO: Handle COLM
      // TODO: Handle GPNF
      // TODO: Handle UWCT

      var terrSection = sections["TERR"];
      er.Position = terrSection.Offset;
      var terr = er.ReadNew<TerrData>();

      var chnkSection = sections["CHNK"];
      er.Position = chnkSection.Offset;
      var tilesBytes = er.ReadBytes(chnkSection.Size);

      var cmapSection = sections["CMAP"];
      er.Position = cmapSection.Offset;
      var tilemapBytes = er.ReadBytes(cmapSection.Size);

      var matlSection = sections["MATL"];
      var expectedMatlSectionSize = terr.MaterialCount * 48;
      Asserts.Equal(expectedMatlSectionSize, matlSection.Size);
      er.Position = matlSection.Offset;
      er.ReadNewArray<BwHeightmapMaterial>(
          out var materials, terr.MaterialCount);

      this.Heightmap = new HeightmapParser(tilemapBytes, tilesBytes);
      this.Materials = materials;
    }
  }
}