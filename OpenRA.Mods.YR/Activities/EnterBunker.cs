#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.YR.Traits;
using OpenRA.Mods.Common.Effects;

namespace OpenRA.Mods.YR.Activities
{
	class EnterBunker : Enter
	{
		readonly BunkerPassenger passenger;
		readonly int maxTries;
		Actor bunker;
		BunkerCargo cargo;
        bool willDisappear;
        WPos targetPos;

        public EnterBunker(Actor self, Actor bunker, WPos pos, bool willDisappear=true, int maxTries = 0, bool repathWhileMoving = true)
			: base(self, bunker, EnterBehaviour.Exit, maxTries, repathWhileMoving)
		{
			this.bunker = bunker;
			this.maxTries = maxTries;
            this.willDisappear = willDisappear;
            targetPos = pos;
			cargo = bunker.Trait<BunkerCargo>();
			passenger = self.Trait<BunkerPassenger>();
		}

		protected override void Unreserve(Actor self, bool abort) { passenger.Unreserve(self); }
		protected override bool CanReserve(Actor self) { return cargo.Unloading || cargo.CanLoad(bunker, self); }
		protected override ReserveStatus Reserve(Actor self)
		{
			var status = base.Reserve(self);
			if (status != ReserveStatus.Ready)
				return status;
			if (passenger.Reserve(self, cargo))
				return ReserveStatus.Ready;
			return ReserveStatus.Pending;
		}

		protected override void OnInside(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
                Mobile mobile = self.TraitOrDefault<Mobile>();
				if (self.IsDead || bunker.IsDead || !cargo.CanLoad(bunker, self))
					return;

                if (!string.IsNullOrEmpty(cargo.Info.SequenceOnCargo))
                {
                    w.Add(new SpriteEffect(bunker.CenterPosition, w, bunker.Info.Name, cargo.Info.SequenceOnCargo, "player" + self.Owner.InternalName));
                }

                if (cargo.GetBunkeredNumber() == 0)
                {
                    cargo.GrantCondition(passenger.info.GrantBunkerCondition);
                }
                cargo.Load(bunker, self);
                if(willDisappear)
                {
                    w.Remove(self);
                }
                else
                {
                    //If didn't disappear, then move the passenger actor to the bunker center
                    self.QueueActivity(mobile.MoveToTarget(self, Target.FromPos(targetPos)));
                    passenger.GrantCondition();
                }
			});

			Done(self);
		}

		protected override bool TryGetAlternateTarget(Actor self, int tries, ref Target target)
		{
			if (tries > maxTries)
				return false;
			var type = target.Actor.Info.Name;
			return TryGetAlternateTargetInCircle(
				self, passenger.Info.AlternateTransportScanRange,
				t => { bunker = t.Actor; cargo = t.Actor.Trait<BunkerCargo>(); }, // update transport and cargo
				a => { var c = a.TraitOrDefault<BunkerCargo>(); return c != null && c.Info.Types.Contains(passenger.Info.CargoType) && (c.Unloading || c.CanLoad(a, self)); },
				new Func<Actor, bool>[] { a => a.Info.Name == type }); // Prefer transports of the same type
		}
	}
}
