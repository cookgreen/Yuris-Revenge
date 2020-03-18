#region Copyright & License Information
/*
 * Modded by Cook Green of YR Mod
 * 
 * Modded by Boolbada of OP Mod.
 * Modded from cargo.cs but a lot changed.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Mods.YR.Activities;
using System;
using OpenRA.Primitives;

/*
Works without base engine modification.
However, Mods.Common\Activities\Air\Land.cs is modified to support the air units to land "mid air!"
See landHeight private variable to track the changes.
*/

namespace OpenRA.Mods.YR.Traits
{
	[Desc("Can be slaved to a spawner.")]
	public class CarrierSlaveInfo : BaseSpawnerSlaveInfo
	{
		[Desc("Move this close to the spawner, before entering it.")]
		public readonly WDist LandingDistance = new WDist(5 * 1024);

		[Desc("We consider this is close enought to the spawner and enter it, instead of trying to reach 0 distance." +
			"This allows the spawned unit to enter the spawner while the spawner is moving.")]
		public readonly WDist CloseEnoughDistance = new WDist(128);	

		public override object Create(ActorInitializer init) { return new CarrierSlave(init, this); }
	}

	public class CarrierSlave : BaseSpawnerSlave, INotifyBecomingIdle
    {
        private readonly AmmoPool[] ammoPools;
        private CarrierMaster spawnerMaster;
        private Actor self;
        public CarrierSlaveInfo Info { get; private set; }

		public CarrierSlave(ActorInitializer init, CarrierSlaveInfo info) : base(init, info)
		{
            self = init.Self ;
			Info = info;
			ammoPools = init.Self.TraitsImplementing<AmmoPool>().ToArray();
		}

		public void EnterSpawner(Actor self)
		{
			// Hopefully, self will be disposed shortly afterwards by SpawnerSlaveDisposal policy.
			if (Master == null || Master.IsDead)
				return;

			// Proceed with enter, if already at it.
			if (self.CurrentActivity is EnterCarrierMaster)
				return;

			// Cancel whatever else self was doing and return.
			self.CancelActivity();

			var tgt = Target.FromActor(Master);

			if (self.TraitOrDefault<AttackAircraft>() != null) // Let attack planes approach me first, before landing.
				self.QueueActivity(new Fly(self, tgt, WDist.Zero, Info.LandingDistance));

			self.QueueActivity(new EnterCarrierMaster(self, Master, spawnerMaster, EnterBehaviour.Exit, Info.CloseEnoughDistance));
		}

		public override void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
		{
			base.LinkMaster(self, master, spawnerMaster);
			this.spawnerMaster = spawnerMaster as CarrierMaster;
		}

		public bool NeedToReload()
		{
			// The unit may not have ammo but will have unlimited ammunitions.
			if (ammoPools.Length == 0)
				return false;

			return ammoPools.All(x => /*!x.AutoReloads &&*/ !x.HasAmmo());
		}

		public virtual void OnBecomingIdle(Actor self)
		{
			EnterSpawner(self);
		}

        public void Reload()
        {
            foreach (var ammoPool in ammoPools)
            {
                ammoPool.GiveAmmo(self, ammoPool.Info.Ammo);
            }
        }
    }
}
