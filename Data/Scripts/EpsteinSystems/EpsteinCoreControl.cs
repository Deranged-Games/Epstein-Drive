using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game;
using VRage.Common.Utils;
using VRageMath;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Library.Utils;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;

using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.EntityComponents;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.ObjectBuilders;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Game.ObjectBuilders.Definitions;
using Sandbox.Game.Screens.Helpers;

namespace EpsteinSystems{  
  [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OxygenTank), false, "EpsteinCore")]
  public class EpsteinCoreController : MyGameLogicComponent{
    private MyObjectBuilder_EntityBase objectBuilder;
    private IMyGasTank TerminalBlock;
    private List<IMyThrust> thrusters;
    private IMyThrust mainthruster;
    private int blocknum = 0;
    private bool startedInit = false;
    public MyResourceSinkComponent ResourceSink { get; protected set; }
    public static MyDefinitionId electricDefinition = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
    public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false){return copy ? (MyObjectBuilder_EntityBase)objectBuilder.Clone() : objectBuilder;}
    
    public override void Init(MyObjectBuilder_EntityBase objectBuilder){
      this.objectBuilder = objectBuilder;
      TerminalBlock = Entity as IMyGasTank;
      if (TerminalBlock != null)
        TerminalBlock.AppendingCustomInfo += Block_AppendingCustomInfo;
      NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
    }
    
    public override void UpdateOnceBeforeFrame(){
      if (startedInit) return;
      startedInit = true;
      try {
        TerminalBlock = Entity as IMyGasTank;
        if(TerminalBlock == null) return;
        ResourceSink = TerminalBlock.ResourceSink as MyResourceSinkComponent;
        ResourceSink.SetRequiredInputFuncByType(electricDefinition, GetRequiredPower);
      } catch(Exception e){}
    }

    public override void UpdateBeforeSimulation10(){
      base.UpdateBeforeSimulation10();
      if(mainthruster != null && mainthruster.IsFunctional){
        if (TerminalBlock != null && TerminalBlock.IsFunctional && TerminalBlock.Enabled)
          mainthruster.Enabled = true;
        else
          mainthruster.Enabled = false;
      }
      try{
        TerminalBlock.RefreshCustomInfo();
        ResourceSink.Update();
      } catch (Exception ex){MyAPIGateway.Utilities.ShowNotification("Error: "+ex, 166, MyFontEnum.Red);}

      MyCubeGrid grid = TerminalBlock.CubeGrid as MyCubeGrid;
      if(blocknum != grid.BlocksCount){
        blocknum = grid.BlocksCount;
        try {
          thrusters = GetBlocksOfType<IMyThrust>();
          mainthruster = GetClosestThruster(thrusters, TerminalBlock as IMyTerminalBlock);
        } catch(Exception ex){MyAPIGateway.Utilities.ShowNotification("Error: "+ex, 1660, MyFontEnum.Red);}
      }
    }

    private List<T> GetBlocksOfType<T>() where T: class, IMyTerminalBlock{
      List<T> blocks = new List<T>();

      IMyCubeGrid grid = TerminalBlock.CubeGrid as IMyCubeGrid;
      if (grid != null && grid.Hierarchy != null){
        HashSet<IMyEntity> componentSet = new HashSet<IMyEntity>();
        grid.Hierarchy.GetChildrenRecursive(componentSet);
        
        foreach (IMyEntity entity in componentSet){
          T block = entity as T;
          if (block != null)
            blocks.Add(block);
        }
      }
      return blocks;
    }
    
    private IMyThrust GetClosestThruster(List<IMyThrust> checkBlocks, IMyTerminalBlock referenceBlock){
      IMyThrust checkBlock = null;
      double prevCheckDistance = 13;

      for (int i = 0; i < checkBlocks.Count; i++){
        double currCheckDistance = (checkBlocks[i].GetPosition() - referenceBlock.GetPosition()).Length();

        var subType = checkBlocks[i].BlockDefinition.SubtypeName;
        if (currCheckDistance < prevCheckDistance && subType == "EpsteinCone"){
          prevCheckDistance = currCheckDistance;
          checkBlock = checkBlocks[i];
        }
      }
      return checkBlock;
    }

    private float GetRequiredPower(){
      if(mainthruster == null)
        return 0f;

      var ThrustBlock = (MyThrust)mainthruster;
      if (TerminalBlock != null && TerminalBlock.IsFunctional && TerminalBlock.Enabled && ThrustBlock != null)
        if (mainthruster.IsFunctional)
          return 100f + (ThrustBlock.CurrentStrength * 200f);
        else
          return 100f;
      else
        return 0f;
    }

    private void Block_AppendingCustomInfo(IMyTerminalBlock block, StringBuilder sb){
      try{
        sb.Clear();
        sb.Append("Power Usage: ").Append(FormatPowerString(ResourceSink.RequiredInputByType(electricDefinition))).Append("\n");
      } catch (Exception ex){MyAPIGateway.Utilities.ShowNotification("Error: "+ex, 166, MyFontEnum.Red);}
    }

    public static string FormatPowerString(float usage){
      if (usage < 0.001) return Math.Round(usage * 1000000f, 0) + "W";
      else if (usage < 1) return Math.Round(usage * 1000f, 2) + "kW";
      else if (usage < 1000) return Math.Round(usage, 2) + "MW";
      else return Math.Round(usage * 0.001f, 2) + "GW";
    }

    public override void Close(){objectBuilder = null;}
  }
}