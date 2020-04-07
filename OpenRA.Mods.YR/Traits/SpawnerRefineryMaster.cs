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
    public class SpawnerRefineryMasterInfo : SpawnerHarvestResourceInfo
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
            return new SpawnerRefineryMaster(init, this);
        }
    }

    public class SpawnerRefineryMaster : BaseSpawnerMaster, INotifyTransform, INotifyBuildingPlaced, ITick, IIssueOrder, IResolveOrder, INotifyIdle
    {
        /*When transformed complete, it must be mining state*/
        public MiningState MiningState = MiningState.Mining;
        public CPos? LastOrderLocation = null;
        private SpawnerRefineryMasterInfo info;
        readonly ResourceLayer resLayer;
        int respawnTicks = 0;
        int kickTicks;
        bool allowKicks = true; // allow kicks?
        Transforms transforms;
        bool force = false;
        CPos? forceMovePos = null;

        public IEnumerable<IOrderTargeter> Orders
        {
            get { yield return new SpawnerResourceHarvestOrderTargeter<SpawnerRefineryMasterInfo>("SpawnerRefineryHarvest"); }
        }

        public SpawnerRefineryMaster(ActorInitializer init, SpawnerRefineryMasterInfo info) : base(init, info)
        {
            this.info = info;
            resLayer = init.Self.World.WorldActor.Trait<ResourceLayer>();
            transforms = init.Self.Trait<Transforms>();
        }

        void INotifyTransform.AfterTransform(Actor toActor)
        {
            //When transform complete, assign the slaves to this transform actor
            SpawnerHarvesterMaster harvesterMaster = toActor.Trait<SpawnerHarvesterMaster>();
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
                toActor.QueueActivity(new SpawnerHarvesterHarvest(toActor));
            }
            else
            {
                toActor.QueueActivity(new SpawnerHarvesterHarvest(toActor));
            }
        }

        public void BeforeTransform(Actor self)
        {

        }

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

        void INotifyBuildingPlaced.BuildingPlaced(Actor self)
        {
        }

        void INotifyTransform.OnTransform(Actor self)
        {
        }

        void Launch(Actor master, BaseSpawnerSlaveEntry slaveEntry, CPos targetLocation)
        {
            var slave = slaveEntry.Actor;

            SpawnIntoWorld(master, slave, master.CenterPosition);

            master.World.AddFrameEndTask(w =>
            {
                slave.QueueActivity(new FindAndDeliverResources(slave, master));
            });
        }

        void HandleSpawnerHarvest(Actor self, Order order)
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

        void IResolveOrder.ResolveOrder(Actor self, Order order)
        {
            if (order.OrderString == "SpawnerRefineryHarvest")
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
            if (order.OrderID == "SpawnerRefineryHarvest")
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
                MiningState = MiningState.Kick;
                self.QueueActivity(new SpawnerRefineryHarvest(self));
            }
        }

        public BaseSpawnerSlaveEntry[] GetSlaves()
        {
            return SlaveEntries;
        }
    }
}
