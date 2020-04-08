#region Copyright & License Information
/*
 * CnP of FindResources.cs of OpenRA... erm... Not quite, anymore!
 * Modded by Boolbada of OP Mod
 * 
 * Modded by Cook Green of YR Mod
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

/*
This one itself doesn't need engine mod.
The slave harvester's docking however, needs engine mod.
*/

using System;
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.YR.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.YR.Activities
{
    /// <summary>
    /// This activity will allow refinery building actor to send its slave to find the resource
    /// </summary>
	public class SlaveMinerMasterHarvest : Activity
	{	
		readonly SlaveMinerMaster harv;
		readonly SlaveMinerInfo harvInfo;
		readonly ResourceClaimLayer claimLayer;
		readonly IPathFinder pathFinder;
		readonly DomainIndex domainIndex;
        int lastScanRange = 1;

		CPos? avoidCell;

		public SlaveMinerMasterHarvest(Actor self)
		{
			harv = self.Trait<SlaveMinerMaster>();
			harvInfo = self.Info.TraitInfo<SlaveMinerInfo>();
			claimLayer = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();
			pathFinder = self.World.WorldActor.Trait<IPathFinder>();
			domainIndex = self.World.WorldActor.Trait<DomainIndex>();
            lastScanRange = harvInfo.LongScanRadius;
			ChildHasPriority = false;
		}

		public SlaveMinerMasterHarvest(Actor self, CPos avoidCell)
			: this(self)
		{
			this.avoidCell = avoidCell;
		}

		Activity Mining(Actor self, out MiningState state)
		{
			// Let the harvester become idle so it can shoot enemies.
			// Tick in SpawnerHarvester trait will kick activity back to KickTick.
			state = MiningState.Mining;
			return ChildActivity;
		}

		Activity Kick(Actor self, out MiningState state)
		{
			var closestHarvestablePosition = ClosestHarvestablePos(self, harvInfo.KickScanRadius);
			if (closestHarvestablePosition.HasValue)
			{
				// I may stay mining.
				state = MiningState.Mining;
				return ChildActivity;
			}

			// get going
			harv.LastOrderLocation = null;
            closestHarvestablePosition = ClosestHarvestablePos(self, lastScanRange);
            if (closestHarvestablePosition != null)
            {
                state = MiningState.Undeploy;
                harv.ForceMove(closestHarvestablePosition.Value);
            }
            else
            {
                state = MiningState.Kick;
                lastScanRange *= 2; // larger search range
            }

            return this;
		}

		public override bool Tick(Actor self)
		{
            /*
             We just need to confirm one thing: when the nearest resource is finished, 
             just find the next resource point and transform and move to that location
             */

			if (IsCanceling)
				return false;

			// Erm... looking at this, I could split these into separte activites...
			// I prefer finite state machine style though...
			// I can see what is going on at high level in this single place -_-
			// I think this is less horrible than OpenRA FindResources... stuff.
			// We are losing one tick, but so what?
			// If this loss isn't acceptable, call ATick() from BTick() or something.
			switch (harv.MiningState)
			{
				case MiningState.Mining:
                    QueueChild(Mining(self, out harv.MiningState));
                    return false;
				case MiningState.Kick:
                    QueueChild(Kick(self, out harv.MiningState));
                    return false;
			}

			return true;
		}

		/// <summary>
		/// Using LastOrderLocation and self.Location as starting points,
		/// perform A* search to find the nearest accessible and harvestable cell.
		/// </summary>
		CPos? ClosestHarvestablePos(Actor self, int searchRadius)
		{
			if (harv.CanHarvestCell(self, self.Location) && claimLayer.CanClaimCell(self, self.Location))
				return self.Location;

			// Determine where to search from and how far to search:
			var searchFromLoc = harv.LastOrderLocation ?? self.Location;
			var searchRadiusSquared = searchRadius * searchRadius;

            BaseSpawnerSlaveEntry choosenSlave = null;
            var slaves = harv.GetSlaves();
            if (slaves.Length > 0)
            {
                choosenSlave = slaves[0];

                var mobile = choosenSlave.Actor.Trait<Mobile>();
                var mobileInfo = choosenSlave.Actor.Info.TraitInfo<MobileInfo>();
                // Find any harvestable resources:
                // var passable = (uint)mobileInfo.GetMovementClass(self.World.Map.Rules.TileSet);
                List<CPos> path;
                using (var search = PathSearch.Search(self.World, mobile.Locomotor, self, true,
                    loc => domainIndex.IsPassable(self.Location, loc, mobileInfo.LocomotorInfo)
                        && harv.CanHarvestCell(self, loc) && claimLayer.CanClaimCell(self, loc))
                    .WithCustomCost(loc =>
                    {
                        if ((avoidCell.HasValue && loc == avoidCell.Value) ||
                            (loc - self.Location).LengthSquared > searchRadiusSquared)
                            return int.MaxValue;

                        return 0;
                    })
                    .FromPoint(self.Location)
                    .FromPoint(searchFromLoc))
                    path = pathFinder.FindPath(search);

                if (path.Count > 0)
                    return path[0];

            }

			return null;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromCell(self.World, self.Location);
		}
	}
}
