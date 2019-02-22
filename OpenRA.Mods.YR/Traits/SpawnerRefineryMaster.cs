using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
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
    public class SpawnerRefineryMasterInfo : BaseSpawnerMasterInfo
    {
        [Desc("Which resources it can harvest. Make sure slaves can mine these too!")]
        public readonly HashSet<string> Resources = new HashSet<string>();

        [Desc("Play this sound when the slave is freed")]
        public readonly string FreeSound = null;
        public override object Create(ActorInitializer init)
        {
            return new SpawnerRefineryMaster(init, this);
        }
    }

    public class SpawnerRefineryMaster : BaseSpawnerMaster, INotifyTransform, INotifyBuildingPlaced, ITick, IIssueOrder, IResolveOrder
    {
        public MiningState MiningState = MiningState.Mining;
        public CPos? LastOrderLocation = null;
        private SpawnerRefineryMasterInfo info;
        readonly ResourceLayer resLayer;
        int respawnTicks = 0;

        public IEnumerable<IOrderTargeter> Orders
        {
            get { yield return new SpawnerHarvestOrderTargeter(); }
        }

        public SpawnerRefineryMaster(ActorInitializer init, SpawnerRefineryMasterInfo info) : base(init, info)
        {
            this.info = info;
            resLayer = init.Self.World.WorldActor.Trait<ResourceLayer>();
        }

        void INotifyTransform.AfterTransform(Actor toActor)
        {
            //When transform complete, assign the slaves to this transform actor
            SpawnerHarvesterMaster harvesterMaster = toActor.Trait<SpawnerHarvesterMaster>();
            foreach (var se in SlaveEntries)
            {
                se.SpawnerSlave.LinkMaster(se.Actor, toActor, harvesterMaster);
                se.SpawnerSlave.Stop(se.Actor);
                se.Actor.QueueActivity(new Follow(se.Actor, Target.FromActor(toActor), WDist.FromCells(1), WDist.FromCells(3)));
            }
            harvesterMaster.AssignSlavesToMaster(SlaveEntries);
        }

        public void BeforeTransform(Actor self)
        {

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
            //allowKicks = true;

            // state == Deploying implies order string of SpawnerHarvestDeploying
            // and must not cancel deploy activity!
            if (MiningState != MiningState.Deploying)
                self.CancelActivity();

            MiningState = MiningState.Scan;

            //self.QueueActivity(new SpawnerHarvesterHarvest(self));
            self.SetTargetLine(Target.FromCell(self.World, LastOrderLocation.Value), Color.Red);

            // Assign new targets for slaves too.
            foreach (var se in SlaveEntries)
            {
                if (se.IsValid && se.Actor.IsInWorld)
                {
                    LastOrderLocation = ResolveHarvestLocation(se.Actor, order);
                    AssignTargetForSpawned(se.Actor, LastOrderLocation.Value);
                }
            }
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
            if (order.OrderString == "SpawnerHarvest")
                HandleSpawnerHarvest(self, order);
            else if (order.OrderString == "Stop" || order.OrderString == "Move")
            {
                // Disable "smart idle"
                MiningState = MiningState.Scan;
            }
        }

        public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
        {
            if (order.OrderID == "SpawnerHarvest")
                return new Order(order.OrderID, self, target, queued);
            return null;
        }
    }
}
