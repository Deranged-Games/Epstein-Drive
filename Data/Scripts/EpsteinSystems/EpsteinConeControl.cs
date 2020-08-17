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

namespace ThrustUpgrade{
  [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false, "EpsteinCone")]
  public class EpsteinConeController : MyGameLogicComponent{
    private MyObjectBuilder_EntityBase objectBuilder;
    private IMyThrust TerminalBlock;
    private List<IMyGasTank> tanks;
    private bool startedInit = false;
    private int blocknum = 0;
    private bool nearbyCore = false;
    public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false){return copy ? (MyObjectBuilder_EntityBase)objectBuilder.Clone() : objectBuilder;}

    public override void Init(MyObjectBuilder_EntityBase objectBuilder){
      base.Init(objectBuilder);
      this.objectBuilder = objectBuilder;
      TerminalBlock = Entity as IMyThrust; //IMyGasTank
      NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
    }

    public override void UpdateOnceBeforeFrame(){
      if (startedInit) return;
      startedInit = true;
      try {
        TerminalBlock = Entity as IMyThrust;
        if(TerminalBlock == null) return;
      } catch(Exception e){}
    }

    public override void UpdateBeforeSimulation10(){
      base.UpdateBeforeSimulation10();
      if(TerminalBlock != null){
        if (!nearbyCore && TerminalBlock.Enabled)
        TerminalBlock.Enabled = false;
      }
      MyCubeGrid grid = TerminalBlock.CubeGrid as MyCubeGrid;
      if(blocknum != grid.BlocksCount){
        blocknum = grid.BlocksCount;
        try {
          tanks = GetBlocksOfType<IMyGasTank>();
          nearbyCore = findNearbyCore(tanks, TerminalBlock as IMyTerminalBlock);
        } catch(Exception ex){MyAPIGateway.Utilities.ShowNotification("Error: "+ex, 1660, MyFontEnum.Red);}
      }
    }

    private bool findNearbyCore(List<IMyGasTank> checkBlocks, IMyTerminalBlock referenceBlock){
      IMyGasTank checkBlock = null;
      double checkDistance = 13;

      for (int i = 0; i < checkBlocks.Count; i++){
        double distance = (checkBlocks[i].GetPosition() - referenceBlock.GetPosition()).Length();
        var subType = checkBlocks[i].BlockDefinition.SubtypeName;
        if (distance < checkDistance && subType == "EpsteinCore") return true;
      }
      return false;
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

    public override void Close(){objectBuilder = null;}
  }
}
