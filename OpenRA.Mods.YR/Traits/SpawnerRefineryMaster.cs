using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.YR.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Drawing;
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

        void Launch(Actor self, BaseSpawnerSlaveEntry se, CPos targetLocation)
        {
            var slave = se.Actor;

            SpawnIntoWorld(self, slave, self.CenterPosition);

            self.World.AddFrameEndTask(w =>
            {
                slave.QueueActivity(new FindResources(slave));
            });
        }

        void HandleSpawnerHarvest(Actor self, Order order)
        {
            //Maybe player have a better idea, let's move
            ForceMove(order.TargetLocation);
        }

        public void ForceMove(CPos pos)
        {
            force = true;
            forceMovePos = pos;
            transforms.DeployTransform(true);
        }

        CPos ResolveHarvestLocation(Actor self, Order order)
        {
            Mobile mobile = self.Trait<Mobile>();

            if (order.TargetLocation == CPos.Zero)
                return self.Location;

            var loc = order.TargetLocation;

            var territory = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();
            if (territory != null)
            {
                // Find the nearest claimable cell to the order location (useful for group-select harvest):
                return mobile.NearestCell(loc, p => mobile.CanEnterCell(p), 1, 6);
            }

            // Find the nearest cell to the order location (useful for group-select harvest):
            return mobile.NearestCell(loc, p => mobile.CanEnterCell(p), 1, 6);
        }

        void AssignTargetForSpawned(Actor slave, CPos targetLocation)
        {
            var sh = slave.Trait<Harvester>();

            // set target spot to mine
            sh.LastOrderLocation = targetLocation;

            // This prevents harvesters returning to an empty patch when the player orders them to a new patch:
            sh.LastHarvestedCell = sh.LastOrderLocation;
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
            foreach (var se in SlaveEntries)
                if (!se.IsValid)
                    hasInvalidEntry = true;
                else if (!se.Actor.IsInWorld)
                    Launch(self, se, destination);

            if (hasInvalidEntry)
                respawnTicks = Info.RespawnTicks;
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

        public void ResolveOrder(Actor self, Order order)
        {
            if (order.OrderString == "SpawnerRefineryHarvest")
                HandleSpawnerHarvest(self, order);
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
