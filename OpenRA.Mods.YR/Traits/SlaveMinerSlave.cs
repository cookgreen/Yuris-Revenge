#region Copyright & License Information
/*
 * Written by Boolbada of OP Mod.
 * 
 * Modded by Cook Green of YR Mod
 * 
 * Follows OpenRA's license, GPLv3 as follows:
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/*
Works without base engine modification...
But the docking procedure may need to change to fit your needs.
In OP Mod, docking changed for Harvester.cs and related files to that
these slaves can "dock" to any adjacent cells near the master.
*/

namespace OpenRA.Mods.YR.Traits
{
    public enum SlaveState
    {
        Free, //We are free, we are free!!! Change owner to the killer
        Idle, //Nothing happen
    }
	public class SlaveMinerSlaveInfo : BaseSpawnerSlaveInfo, Requires<HarvesterInfo>
	{
        [Desc("What will happen when master was killed?")]
        public readonly SlaveState OnMasterKilled = SlaveState.Idle;

        [Desc("What will happen when master is changed owner?")]
        public readonly SlaveState OnMasterOwnerChanged = SlaveState.Idle;
		public override object Create(ActorInitializer init)
		{
			return new SlaveMinerSlave(init, this);
		}
	}

	class SlaveMinerSlave : BaseSpawnerSlave, ITick
	{
		SlaveMinerHarvester spawnerHarvesterMaster;
        SlaveMinerSlaveInfo info;

		public SlaveMinerSlave(ActorInitializer init, SlaveMinerSlaveInfo info) : base(init, info) {
            this.info = info;
        }

		public override void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
		{
            base.LinkMaster(self, master, spawnerMaster);
            // Link master for the harvester trait.
            try
            {
                spawnerHarvesterMaster = Master.Trait<SlaveMinerHarvester>();
            }
            catch
            {
            }
		}

        public override void OnMasterKilled(Actor self, Actor attacker, SpawnerSlaveDisposal disposal)
        {
            switch(info.OnMasterKilled)
            {
                case SlaveState.Free:
                    self.ChangeOwner(attacker.Owner);
                    break;
            }
        }

        public override void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
        {
            switch(info.OnMasterOwnerChanged)
            {
                case SlaveState.Free:
                    self.ChangeOwner(newOwner);
                    break;
            }
        }

        public void Tick(Actor self)
		{
            // Compensate for bug #13879 (upstream).
            // https://github.com/OpenRA/OpenRA/issues/13879
            // Follow activity sometimes fails to cancel and the slaves get busy locked by WaitFor activity.
            if (spawnerHarvesterMaster == null)
            {
                return;
            }
			if (spawnerHarvesterMaster.MiningState == MiningState.Mining && self.CurrentActivity is WaitFor)
			{
				self.CancelActivity();

				/// No need to run this here, since it already happened.
				/// This slave is just bugged out by Follow activity not canceling properly.
				/// AssignTargetForSpawned(s, self.Location);

				self.QueueActivity(new FindAndDeliverResources(self));
			}
		}
	}
}
