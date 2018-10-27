#region Copyright & License Information
/*
 * Modded by Cook Green of YR Mod
 * Modded from CarrierSlave.cs but a lot changed.
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
using OpenRA.Mods.YR.Activities;
using OpenRA.Mods.YR.Traits;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    [Desc("Can be slaved to a spawner.")]
    public class MarkerSlaveInfo : BaseSpawnerSlaveInfo
    {
        [Desc("Move this close to the spawner, before entering it.")]
        public readonly WDist LandingDistance = new WDist(5 * 1024);

        [Desc("We consider this is close enought to the spawner and enter it, instead of trying to reach 0 distance." +
            "This allows the spawned unit to enter the spawner while the spawner is moving.")]
        public readonly WDist CloseEnoughDistance = new WDist(128);

        public override object Create(ActorInitializer init) { return new MarkerSlave(init, this); }
    }

    public class MarkerSlave : BaseSpawnerSlave, INotifyBecomingIdle
    {
        public MarkerSlaveInfo Info { get; private set; }
        private WPos finishEdge;
        private WVec spawnOffset;
        private WPos targetPos;
        readonly AmmoPool[] ammoPools;

        MarkerMaster spawnerMaster;

        public MarkerSlave(ActorInitializer init, MarkerSlaveInfo info) : base(init, info)
        {
            Info = info;
            ammoPools = init.Self.TraitsImplementing<AmmoPool>().ToArray();
        }

        public void SetSpawnInfo(WPos finishEdge, WVec spawnOffset, WPos targetPos)
        {
            this.finishEdge = finishEdge;
            this.spawnOffset = spawnOffset;
            this.targetPos = targetPos;
        }

        public override void Attack(Actor self, Target target)
        {
            base.Attack(self, target);
        }

        public void EnterSpawner(Actor self)
        {
            // Hopefully, self will be disposed shortly afterwards by SpawnerSlaveDisposal policy.
            if (Master == null || Master.IsDead)
                return;

            // Proceed with enter, if already at it.
            if (self.CurrentActivity is EnterCarrierMaster)
                return;

            // Cancel whatever else self was doing and return

            //var tgt = Target.FromActor(Master);
            //
            //if (self.TraitOrDefault<AttackPlane>() != null) // Let attack planes approach me first, before landing.
            //    self.QueueActivity(new Fly(self, tgt, WDist.Zero, Info.LandingDistance));
            //
            //self.QueueActivity(new EnterCarrierMaster(self, Master, spawnerMaster, EnterBehaviour.Exit, Info.CloseEnoughDistance));
            

            self.CancelActivity();

            self.QueueActivity(new Fly(self, Target.FromPos(targetPos + spawnOffset)));
            self.QueueActivity(new Fly(self, Target.FromPos(finishEdge + spawnOffset)));
            self.QueueActivity(new RemoveSelf());
        }

        public override void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
        {
            base.LinkMaster(self, master, spawnerMaster);
            this.spawnerMaster = spawnerMaster as MarkerMaster;
        }

        bool NeedToReload(Actor self)
        {
            // The unit may not have ammo but will have unlimited ammunitions.
            if (ammoPools.Length == 0)
                return false;

            return ammoPools.All(x => !x.AutoReloads && !x.HasAmmo());
        }

        public virtual void OnBecomingIdle(Actor self)
        {
            EnterSpawner(self);
        }
    }
}
