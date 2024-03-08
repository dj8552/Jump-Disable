using System.IO;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Torch.Views;
using VRage.ModAPI;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using Sandbox.Game;
using System.Windows.Documents;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRageMath;
using HarmonyLib;
using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems;
using VRage.Utils;

namespace JumpDisable
{
    public class Plugin : TorchPluginBase, IWpfPlugin
    {
        private Persistent<Config> _config = null!;

        static List<IMyFunctionalBlock> _turrets = new List<IMyFunctionalBlock>();
        static MyCubeGrid grid;
        static bool OnOffTimer = false;
        

        public int counter = 0;
        
        public override void Init(ITorchBase torch)
        {
            new Harmony("JumpDisable").PatchAll();
            base.Init(torch);
            _config = Persistent<Config>.Load(Path.Combine(StoragePath, "JumpDisable.cfg"));
            Config._subtypes.Add("LargeRailgun");
            Config._subtypes.Add("SmallRailgun");
            Config._subtypes.Add("SmallGatlingGun");
            Config._subtypes.Add("SmallGatlingGunWarfare2");
            Config._subtypes.Add("LargeGatlingTurret");
            Config._subtypes.Add("SmallGatlingTurret");
            Config._subtypes.Add("LargeInteriorTurret");
            Config._subtypes.Add("SmallBlockAutocannon");
            Config._subtypes.Add("AutoCannonTurret");
            Config._subtypes.Add("SmallMissileLauncher");
            Config._subtypes.Add("LargeMissileLauncher");
            Config._subtypes.Add("SmallMissileLauncherWarfare2");
            Config._subtypes.Add("SmallRocketLauncherReload");
            Config._subtypes.Add("SmallMissileTurret");
            Config._subtypes.Add("SmallBlockMediumCalibreGun");
            Config._subtypes.Add("SmallBlockMediumCalibreTurret");
            Config._subtypes.Add("LargeBlockMediumCalibreTurret");
            Config._subtypes.Add("LargeBlockLargeCalibreGun");
            Config._subtypes.Add("LargeCalibreTurret");
        }
        public override void Update()
        {

            //runs every 10 seconds
            if (counter % 600 == 0)
            {
                
            }

            counter++;
        }
        
        public static void Turret(MyCubeGrid grid)
        {
            foreach (var block in grid.GetFatBlocks())
            {
                if (block != null && Config._subtypes.Contains(block.DefinitionId.Value.SubtypeId.ToString()))
                {
                    _turrets.Add((IMyFunctionalBlock)block);
                }
            }
            foreach (var turret in _turrets)
            {
                turret.Enabled = false;        
                
                if (OnOffTimer)
                {
                    turret.Enabled = true;
                    OnOffTimer = false;
                }
            }
        }

        public UserControl GetControl() => new PropertyGrid
        {
            Margin = new(3),
            DataContext = _config.Data
        };

        [HarmonyPatch]
        public class Patch
        {           

            [HarmonyPrefix]
            [HarmonyPatch(typeof(MyGridJumpDriveSystem), "PerformJump")]
            static void PerformJump(Vector3D __jumpTarget, ref MyGridJumpDriveSystem __instance)
            {
                var m_jumpdir = __jumpTarget - ((MyCubeGrid)AccessTools.Field(typeof(MyUpdateableGridSystem), "Grid").GetValue(__instance)).WorldMatrix.Translation;
                AccessTools.Field(typeof(MyGridJumpDriveSystem), "m_jumpDirection").SetValue(__instance, m_jumpdir);

                Vector3D m_jumpDirNorm;
                AccessTools.Field(typeof(MyGridJumpDriveSystem), "m_jumpDirectionnorm").GetValue(__instance);
                Vector3D.Normalize(ref m_jumpdir, out m_jumpDirNorm);
                AccessTools.Field(typeof(MyGridJumpDriveSystem), "m_jumpDirectionnorm").SetValue(__instance, m_jumpDirNorm);

                var m_userId = AccessTools.Field(typeof(MyGridJumpDriveSystem), "m_userId").GetValue(__instance);
                AccessTools.Method(typeof(MyGridJumpDriveSystem), "DepleteJumpDrives").Invoke(__instance, new object[] { m_jumpdir, m_userId });

                bool flag = false;
                if ((bool)AccessTools.Method(typeof(MyGridJumpDriveSystem), "IsLocalCharacterAffectedByJump").Invoke(__instance, new Object[] { false }))
                {
                    flag = true;
                }
                if (flag)
                {
                    MyThirdPersonSpectator.Static.ResetViewerAngle(null);
                    MyThirdPersonSpectator.Static.ResetViewerDistance(null);
                    MyThirdPersonSpectator.Static.RecalibrateCameraPosition(false);
                }
                AccessTools.Field(typeof(MyGridJumpDriveSystem), "m_jumped").SetValue(__instance, true);
                MatrixD worldMatrix = ((MyCubeGrid)AccessTools.Field(typeof(MyUpdateableGridSystem), "Grid").GetValue(__instance)).WorldMatrix;

                worldMatrix.Translation = ((MyCubeGrid)AccessTools.Field(typeof(MyUpdateableGridSystem), "Grid").GetValue(__instance)).WorldMatrix.Translation + m_jumpdir;
                ((MyCubeGrid)AccessTools.Field(typeof(MyUpdateableGridSystem), "Grid").GetValue(__instance)).Teleport(worldMatrix, null, false);
                if (flag)
                {
                    MyThirdPersonSpectator.Static.ResetViewerAngle(null);
                    MyThirdPersonSpectator.Static.ResetViewerDistance(null);
                    MyThirdPersonSpectator.Static.RecalibrateCameraPosition(false);
                }
                Turret(grid);
            }
        }
    }
}