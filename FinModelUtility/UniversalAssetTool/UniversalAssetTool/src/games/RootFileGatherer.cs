﻿using fin.io.bundles;

using uni.config;
using uni.games.battalion_wars_1;
using uni.games.battalion_wars_2;
using uni.games.chibi_robo;
using uni.games.dead_space_1;
using uni.games.dead_space_2;
using uni.games.dead_space_3;
using uni.games.doshin_the_giant;
using uni.games.glover;
using uni.games.great_ace_attorney;
using uni.games.halo_wars;
using uni.games.luigis_mansion_3d;
using uni.games.majoras_mask_3d;
using uni.games.mario_kart_double_dash;
using uni.games.midnight_club_2;
using uni.games.nintendogs_labrador_and_friends;
using uni.games.ocarina_of_time;
using uni.games.ocarina_of_time_3d;
using uni.games.pikmin_1;
using uni.games.pikmin_2;
using uni.games.professor_layton_vs_phoenix_wright;
using uni.games.super_mario_64;
using uni.games.super_mario_sunshine;
using uni.games.super_smash_bros_melee;
using uni.games.wind_waker;
using uni.util.io;

namespace uni.games {
  public class RootFileGatherer {
    public IFileBundleDirectory GatherAllFiles() {
      IAnnotatedFileBundleGathererAccumulator accumulator =
          Config.Instance.ExtractorSettings.UseMultithreadingToExtractRoms
              ? new ParallelAnnotatedFileBundleGathererAccumulator()
              : new AnnotatedFileBundleGathererAccumulator();

      var gatherers = new IAnnotatedFileBundleGatherer[] {
          new BattalionWars1AnnotatedFileGatherer(),
          new BattalionWars2AnnotatedFileGatherer(),
          new ChibiRoboAnnotatedFileGatherer(),
          new DeadSpace1AnnotatedFileGatherer(),
          new DeadSpace2AnnotatedFileGatherer(),
          new DeadSpace3AnnotatedFileGatherer(),
          new DoshinTheGiantAnnotatedFileGatherer(),
          new GloverModelAnnotatedFileGatherer(),
          new GreatAceAttorneyModelAnnotatedFileGatherer(),
          new HaloWarsModelAnnotatedFileGatherer(),
          new LuigisMansion3dModelAnnotatedFileGatherer(),
          new MajorasMask3dAnnotatedFileGatherer(),
          new MarioKartDoubleDashAnnotatedFileGatherer(),
          new MidnightClub2AnnotatedFileGatherer(),
          new NintendogsLabradorAndFriendsAnnotatedFileBundleGatherer(),
          new OcarinaOfTimeAnnotatedFileBundleGatherer(),
          new OcarinaOfTime3dAnnotatedFileGatherer(),
          new Pikmin1ModelAnnotatedFileGatherer(),
          new Pikmin2AnnotatedFileGatherer(),
          new ProfessorLaytonVsPhoenixWrightModelAnnotatedFileGatherer(),
          new SuperMario64AnnotatedFileGatherer(),
          new SuperMarioSunshineModelAnnotatedFileGatherer(),
          new SuperSmashBrosMeleeModelAnnotatedFileGatherer(),
          new WindWakerAnnotatedFileGatherer(),
      };
      foreach (var gatherer in gatherers) {
        accumulator.Add(gatherer);
      }

      return new FileBundleHierarchyOrganizer().Organize(
          accumulator.GatherFileBundles());
    }
  }
}