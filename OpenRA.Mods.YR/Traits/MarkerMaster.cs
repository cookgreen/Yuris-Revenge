using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
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
    [Desc("This actor can spawn actors.")]
    public class MarkerMasterInfo : BaseSpawnerMasterInfo
    {
        [Desc("Spawn is a missile that dies and not return.")]
        public readonly bool SpawnIsMissile = false;

        [Desc("Spawn rearm delay, in ticks")]
        public readonly int RearmTicks = 150;

        [GrantedConditionReference]
        [Desc("The condition to grant to self right after launching a spawned unit. (Used by V3 to make immobile.)")]
        public readonly string LaunchingCondition = null;

        [Desc("After this many ticks, we remove the condition.")]
        public readonly int LaunchingTicks = 15;

        [Desc("Pip color for the spawn count.")]
        public readonly PipType PipType = PipType.Yellow;

        [Desc("Insta-repair spawners when they return?")]
        public readonly bool InstaRepair = true;

        [GrantedConditionReference]
        [Desc("The condition to grant to self while spawned units are loaded.",
            "Condition can stack with multiple spawns.")]
        public readonly string LoadedCondition = null;

        [Desc("Conditions to grant when specified actors are contained inside the transport.",
            "A dictionary of [actor id]: [condition].")]
        public readonly Dictionary<string, string> SpawnContainConditions = new Dictionary<string, string>();

        [GrantedConditionReference]
        public IEnumerable<string> LinterSpawnContainConditions { get { return SpawnContainConditions.Values; } }

        public readonly int SquadSize = 1;
        public readonly WVec SquadOffset = new WVec(-1536, 1536, 0);

        public readonly int QuantizedFacings = 32;
        public readonly WDist Cordon = new WDist(5120);

        public override object Create(ActorInitializer init) { return new MarkerMaster(init, this); }
    }

    public class MarkerMaster : BaseSpawnerMaster, IPips, ITick, INotifyAttack, INotifyBecomingIdle
    {
        class CarrierSlaveEntry : BaseSpawnerSlaveEntry
        {
            public int RearmTicks = 0;
            public bool IsLaunched = false;
            public new CarrierSlave SpawnerSlave;
        }

        readonly Dictionary<string, Stack<int>> spawnContainTokens = new Dictionary<string, Stack<int>>();

        public new MarkerMasterInfo Info { get; private set; }

        CarrierSlaveEntry[] slaveEntries;
        ConditionManager conditionManager;

        Stack<int> loadedTokens = new Stack<int>();

        int respawnTicks = 0;

        public MarkerMaster(ActorInitializer init, MarkerMasterInfo info) : base(init, info)
        {
            Info = info;
        }

        protected override void Created(Actor self)
        {
            base.Created(self);
            conditionManager = self.Trait<ConditionManager>();
        }

        public override BaseSpawnerSlaveEntry[] CreateSlaveEntries(BaseSpawnerMasterInfo info)
        {
            slaveEntries = new CarrierSlaveEntry[info.Actors.Length]; // For this class to use

            for (int i = 0; i < slaveEntries.Length; i++)
                slaveEntries[i] = new CarrierSlaveEntry();

            return slaveEntries; // For the base class to use
        }

        public override void InitializeSlaveEntry(Actor slave, BaseSpawnerSlaveEntry entry)
        {
            var se = entry as CarrierSlaveEntry;
            base.InitializeSlaveEntry(slave, se);

            se.RearmTicks = 0;
            se.IsLaunched = false;
            se.SpawnerSlave = slave.Trait<CarrierSlave>();
        }

        void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

        // The rate of fire of the dummy weapon determines the launch cycle as each shot
        // invokes Attacking()
        void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
        {
            if (IsTraitDisabled)
                return;

            if (a.Info.Name != Info.SpawnerArmamentName)
                return;

            // Issue retarget order for already launched ones
            foreach (var slave in slaveEntries)
                if (slave.IsLaunched && slave.IsValid)
                    slave.SpawnerSlave.Attack(slave.Actor, target);

            var se = GetLaunchable();
            if (se == null)
                return;

            se.IsLaunched = true; // mark as launched

            // Launching condition is timed, so not saving the token.
            if (Info.LaunchingCondition != null)
                conditionManager.GrantCondition(self, Info.LaunchingCondition/*, Info.LaunchingTicks*/);

            SpawnIntoWorld(self, se.Actor, self.CenterPosition);

            // Queue attack order, too.
            self.World.AddFrameEndTask(w =>
            {
                // The actor might had been trying to do something before entering the carrier.
                // Cancel whatever it was trying to do.
                se.SpawnerSlave.Stop(se.Actor);

                se.SpawnerSlave.Attack(se.Actor, target);
            });
        }

        public void SendSlaveFromTheEdage(Actor self, WPos target)
        {
            for (int j = 0; j < Info.Actors.Length; j++)
            {
                string slaveName = Info.Actors[j];
                int attackFacing = 256 * self.World.SharedRandom.Next(Info.QuantizedFacings) / Info.QuantizedFacings;

                var altitude = self.World.Map.Rules.Actors[slaveName].TraitInfo<AircraftInfo>().CruiseAltitude.Length;
                var attackRotation = WRot.FromFacing(attackFacing);
                var delta = new WVec(0, -1024, 0).Rotate(attackRotation);
                target = target + new WVec(0, 0, altitude);
                var startEdge = target - (self.World.Map.DistanceToEdge(target, -delta) + Info.Cordon).Length * delta / 1024;
                var finishEdge = target + (self.World.Map.DistanceToEdge(target, delta) + Info.Cordon).Length * delta / 1024;

                var aircraftInRange = new Dictionary<Actor, bool>();


                self.World.AddFrameEndTask(w =>
                {
                    Actor distanceTestActor = null;
                    for (var i = -Info.SquadSize / 2; i <= Info.SquadSize / 2; i++)
                    {
                        // Even-sized squads skip the lead plane
                        if (i == 0 && (Info.SquadSize & 1) == 0)
                            continue;

                        // Includes the 90 degree rotation between body and world coordinates
                        var so = Info.SquadOffset;
                        var spawnOffset = new WVec(i * so.Y, -Math.Abs(i) * so.X, 0).Rotate(attackRotation);
                        var targetOffset = new WVec(i * so.Y, 0, 0).Rotate(attackRotation);

                        var a = w.CreateActor(slaveName, new TypeDictionary
                    {
                        new CenterPositionInit(startEdge + spawnOffset),
                        new OwnerInit(self.Owner),
                        new FacingInit(attackFacing),
                    });

                        var attack = a.Trait<AttackBomber>();
                        attack.SetTarget(w, target + targetOffset);

                        a.QueueActivity(new Fly(a, Target.FromPos(target + spawnOffset)));
                        a.QueueActivity(new Fly(a, Target.FromPos(finishEdge + spawnOffset)));
                        a.QueueActivity(new RemoveSelf());
                        aircraftInRange.Add(a, false);
                        distanceTestActor = a;
                    }
                });
            }
    }

        public virtual void OnBecomingIdle(Actor self)
        {
            Recall(self);
        }

        void Recall(Actor self)
        {
            // Tell launched slaves to come back and enter me.
            foreach (var se in slaveEntries)
                if (se.IsLaunched && se.IsValid)
                    se.SpawnerSlave.EnterSpawner(se.Actor);
        }

        public override void OnSlaveKilled(Actor self, Actor slave)
        {
            // Set clock so that regen happens.
            if (respawnTicks <= 0) // Don't interrupt an already running timer!
                respawnTicks = Info.RespawnTicks;
        }

        CarrierSlaveEntry GetLaunchable()
        {
            foreach (var se in slaveEntries)
                if (se.RearmTicks <= 0 && !se.IsLaunched && se.IsValid)
                    return se;

            return null;
        }

        public IEnumerable<PipType> GetPips(Actor self)
        {
            if (IsTraitDisabled)
                yield break;

            int inside = 0;
            foreach (var se in slaveEntries)
                if (se.IsValid && !se.IsLaunched)
                    inside++;

            for (var i = 0; i < Info.Actors.Length; i++)
            {
                if (i < inside)
                    yield return Info.PipType;
                else
                    yield return PipType.Transparent;
            }
        }

        public void PickupSlave(Actor self, Actor a)
        {
            CarrierSlaveEntry slaveEntry = null;
            foreach (var se in slaveEntries)
                if (se.Actor == a)
                {
                    slaveEntry = se;
                    break;
                }

            if (slaveEntry == null)
                throw new InvalidOperationException("An actor that isn't my slave entered me?");

            slaveEntry.IsLaunched = false;

            // setup rearm
            slaveEntry.RearmTicks = Info.RearmTicks;

            string spawnContainCondition;
            if (conditionManager != null && Info.SpawnContainConditions.TryGetValue(a.Info.Name, out spawnContainCondition))
                spawnContainTokens.GetOrAdd(a.Info.Name).Push(conditionManager.GrantCondition(self, spawnContainCondition));

            if (conditionManager != null && !string.IsNullOrEmpty(Info.LoadedCondition))
                loadedTokens.Push(conditionManager.GrantCondition(self, Info.LoadedCondition));
        }

        public void Tick(Actor self)
        {
            if (respawnTicks > 0)
            {
                respawnTicks--;

                // Time to respawn someting.
                if (respawnTicks <= 0)
                {
                    Replenish(self, slaveEntries);

                    // If there's something left to spawn, restart the timer.
                    if (SelectEntryToSpawn(slaveEntries) != null)
                        respawnTicks = Info.RespawnTicks;
                }
            }

            // Rearm
            foreach (var se in slaveEntries)
            {
                if (se.RearmTicks > 0)
                    se.RearmTicks--;
            }
        }
    }
}
