#region Copyright & License Information
/*
 * Written by Cook Green of YR Mod
 * Follows GPLv3 License as the OpenRA engine:
 * 
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion
using OpenRA;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.YR.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    public class SlaveMinerMasterInfo : SpawnerHarvestResourceInfo
    {
        [Desc("When deployed, use this scan radius.")]
        public readonly int ShortScanRadius = 8;

        [Desc("Look this far when Searching for Ore (in Cells)")]
        public readonly int LongScanRadius = 24;

        [Desc("Look this far when trying to find a deployable position from the target resource patch")]
        public readonly int DeployScanRadius = 8; // 8 * 8 * 3 should be enough candidates, seriously.

        [Desc("If no resource within range at each kick, move.")]
        public readonly int KickScanRadius = 5;

        [Desc("If the SlaveMiner is idle for this long, he'll try to look for ore again at SlaveMinerShortScan range to find ore and wake up (in ticks)")]
        public readonly int KickDelay = 20;

        [Desc("Play this sound when the slave is freed")]
        public readonly string FreeSound = null;
        public override object Create(ActorInitializer init)
        {
            return new SlaveMinerMaster(init, this);
        }
    }

    public class SlaveMinerMaster : BaseSpawnerMaster, INotifyTransform, 
        INotifyBuildingPlaced, ITick, IIssueOrder, IResolveOrder
    {
        /*When transformed complete, it must be mining state*/
        public MiningState MiningState = MiningState.Mining;
        public CPos? LastOrderLocation = null;
        private SlaveMinerMasterInfo info;
        private readonly ResourceLayer resLayer;
        private int respawnTicks = 0;
        private int kickTicks;
        private bool allowKicks = true; // allow kicks?
        private Transforms transforms;
        private bool force = false;
        private CPos? forceMovePos = null;
        private const string orderID = "SlaveMinerMasterHarvest";

        public IEnumerable<IOrderTargeter> Orders
        {
            get { yield return new SlaveMinerHarvestOrderTargeter<SlaveMinerMasterInfo>(orderID); }
        }

        public SlaveMinerMaster(ActorInitializer init, SlaveMinerMasterInfo info) : base(init, info)
        {
            this.info = info;
            resLayer = init.Self.World.WorldActor.Trait<ResourceLayer>();
            transforms = init.Self.Trait<Transforms>();
        }

		#region Transform
		public void AfterTransform(Actor toActor)
        {
            //When transform complete, assign the slaves to this transform actor
            SlaveMinerHarvester harvesterMaster = toActor.Trait<SlaveMinerHarvester>();
            foreach (var se in SlaveEntries)
            {
                se.SpawnerSlave.LinkMaster(se.Actor, toActor, harvesterMaster);
                se.SpawnerSlave.Stop(se.Actor);
                if (!se.Actor.IsDead)
                    se.Actor.QueueActivity(new Follow(se.Actor, Target.FromActor(toActor), WDist.FromCells(1), WDist.FromCells(3), null));
            }
            harvesterMaster.AssignSlavesToMaster(SlaveEntries);
            if (force)
            {
                harvesterMaster.LastOrderLocation = forceMovePos;
                toActor.QueueActivity(new SlaveMinerHarvesterHarvest(toActor));
            }
            else
            {
                toActor.QueueActivity(new SlaveMinerHarvesterHarvest(toActor));
            }
        }

        public void BeforeTransform(Actor self)
        {

        }

        public void OnTransform(Actor self)
        {
        }

		#endregion

		public bool CanHarvestCell(Actor self, CPos cell)
        {
            // Resources only exist in the ground layer
            if (cell.Layer != 0)
                return false;

            var resType = resLayer.GetResource(cell);
            if (resType == null)
                return false;

            // Can the harvester collect this kind of resource?
            return info.Resources.Contains(resType.Info.Type);
        }

        private void Launch(Actor master, BaseSpawnerSlaveEntry slaveEntry, CPos targetLocation)
        {
            var slave = slaveEntry.Actor;

            SpawnIntoWorld(master, slave, master.CenterPosition);
        }

        public override void SpawnIntoWorld(Actor self, Actor slave, WPos centerPosition)
        {
            var exit = ChooseExit(self);
            SetSpawnedFacing(slave, self, exit);

            self.World.AddFrameEndTask(w =>
            {
                if (self.IsDead)
                    return;

                var spawnOffset = exit == null ? WVec.Zero : exit.SpawnOffset;
                slave.Trait<IPositionable>().SetVisualPosition(slave, centerPosition + spawnOffset);

                var location = centerPosition + spawnOffset;

                w.Add(slave);

                slave.CancelActivity();

                slave.CurrentActivity.QueueChild(new FindAndDeliverResources(slave, self));
            });
        }

        private void HandleSpawnerHarvest(Actor self, Order order)
        {
            //Maybe player have a better idea, let's move
            ForceMove(self.World.Map.CellContaining(order.Target.CenterPosition));
        }

        public void ForceMove(CPos pos)
        {
            force = true;
            forceMovePos = pos;
            transforms.DeployTransform(false);
        }
        public override void OnSlaveKilled(Actor self, Actor slave)
        {
            // Set clock so that regen happens.
            if (respawnTicks <= 0) // Don't interrupt an already running timer!
                respawnTicks = Info.RespawnTicks;
        }

        public override void Killed(Actor self, AttackInfo e)
        {
            base.Killed(self, e);

            if (!string.IsNullOrEmpty(info.FreeSound))
            {
                Game.Sound.Play(SoundType.World, info.FreeSound, self.CenterPosition);
            }
        }

        public void BuildingPlaced(Actor self)
        {
        }

        public void ResolveOrder(Actor self, Order order)
        {
            if (order.OrderString == orderID)
            {
                HandleSpawnerHarvest(self, order);
            }
            else if (order.OrderString == "Stop" || order.OrderString == "Move")
            {
                // Disable "smart idle"
                MiningState = MiningState.Scan;
            }
        }

        public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
        {
            if (order.OrderID == orderID)
                return new Order(order.OrderID, self, target, queued);
            return null;
        }

        public void TickIdle(Actor self)
        {
            // wake up on idle for long (to find new resource patch. i.e., kick)
            if (allowKicks && self.IsIdle)
                kickTicks--;
            else
                kickTicks = info.KickDelay;

            if (kickTicks <= 0)
            {
                kickTicks = info.KickDelay;
                MiningState = MiningState.Packaging;
                self.QueueActivity(new SlaveMinerMasterHarvest(self));
            }
        }

        public BaseSpawnerSlaveEntry[] GetSlaves()
        {
            return SlaveEntries;
        }

        public void Tick(Actor self)
        {
            respawnTicks--;
            if (respawnTicks > 0)
                return;

            if (MiningState != MiningState.Mining)
                return;

            Replenish(self, SlaveEntries);

            CPos destination = LastOrderLocation.HasValue ? LastOrderLocation.Value : self.Location;

            // Launch whatever we can.
            bool hasInvalidEntry = false;
            foreach (var slaveEntry in SlaveEntries)
            {
                if (!slaveEntry.IsValid)
                {
                    hasInvalidEntry = true;
                }
                else if (!slaveEntry.Actor.IsInWorld)
                {
                    Launch(self, slaveEntry, destination);
                }
            }

            if (hasInvalidEntry)
            {
                respawnTicks = Info.RespawnTicks;
            }
        }
    }
}
