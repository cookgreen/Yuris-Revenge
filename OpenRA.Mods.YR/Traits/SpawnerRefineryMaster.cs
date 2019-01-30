using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    public class SpawnerRefineryMasterInfo : BaseSpawnerMasterInfo
    {
        [Desc("Play this sound when the slave is freed")]
        public readonly string FreeSound = null;
        public override object Create(ActorInitializer init)
        {
            return new SpawnerRefineryMaster(init, this);
        }
    }

    public class SpawnerRefineryMaster : BaseSpawnerMaster, INotifyTransform, INotifyBuildingPlaced, ITick
    {
        public MiningState MiningState = MiningState.Mining;
        public CPos? LastOrderLocation = null;
        private SpawnerRefineryMasterInfo info;
        int respawnTicks = 0;
        public SpawnerRefineryMaster(ActorInitializer init, SpawnerRefineryMasterInfo info) : base(init, info)
        {
            for (int i = 0; i < SlaveEntries.Length; i++)
            {
                if(SlaveEntries[i].IsValid)
                {
                    continue;
                }
                Replenish(init.Self, SlaveEntries[i]);
            }
            this.info = info;
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
                // Move into world, if not. Ground units get stuck without this.
                if (Info.SpawnIsGroundUnit)
                {
                    var mv = se.Actor.Trait<IMove>().MoveIntoWorld(slave, self.Location);
                    if (mv != null)
                        slave.QueueActivity(mv);
                }

                AssignTargetForSpawned(slave, targetLocation);
                slave.QueueActivity(new FindResources(slave));
            });
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
    }
}
