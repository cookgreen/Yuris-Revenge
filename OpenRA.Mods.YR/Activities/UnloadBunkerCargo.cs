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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Mods.YR.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class UnloadBunkerCargo : Activity
	{
		readonly Actor self;
		readonly BunkerCargo cargo;
		readonly INotifyUnload[] notifiers;
		readonly bool unloadAll;

		public UnloadBunkerCargo(Actor self, bool unloadAll)
		{
			this.self = self;
			cargo = self.Trait<BunkerCargo>();
			notifiers = self.TraitsImplementing<INotifyUnload>().ToArray();
			this.unloadAll = unloadAll;
		}

		public Pair<CPos, SubCell>? ChooseExitSubCell(Actor passenger)
		{
			var pos = passenger.Trait<IPositionable>();

			return cargo.CurrentAdjacentCells
				.Shuffle(self.World.SharedRandom)
				.Select(c => Pair.New(c, pos.GetAvailableSubCell(c)))
				.Cast<Pair<CPos, SubCell>?>()
				.FirstOrDefault(s => s.Value.Second != SubCell.Invalid);
		}

		IEnumerable<CPos> BlockedExitCells(Actor passenger)
		{
			var pos = passenger.Trait<IPositionable>();

			// Find the cells that are blocked by transient actors
			return cargo.CurrentAdjacentCells
				.Where(c => pos.CanEnterCell(c, null, true) != pos.CanEnterCell(c, null, false));
		}

		public override Activity Tick(Actor self)
		{
			cargo.Unloading = false;
			if (IsCanceled || cargo.IsEmpty(self))
				return NextActivity;

			foreach (var inu in notifiers)
				inu.Unloading(self);

			var actor = cargo.Peek(self);
			var spawn = self.CenterPosition;

			var exitSubCell = ChooseExitSubCell(actor);
			if (exitSubCell == null)
			{
				self.NotifyBlocker(BlockedExitCells(actor));

				return ActivityUtils.SequenceActivities(new Wait(10), this);
			}

			cargo.Unload(self);
			self.World.AddFrameEndTask(w =>
			{
				if (actor.Disposed)
					return;

				var move = actor.Trait<IMove>();
				var pos = actor.Trait<IPositionable>();

                var bunkerPassenger = actor.TraitOrDefault<BunkerPassenger>();
                bunkerPassenger.RevokeCondition();//Disable the condition

                actor.CancelActivity();
				pos.SetVisualPosition(actor, spawn);
				actor.QueueActivity(move.MoveIntoWorld(actor, exitSubCell.Value.First, exitSubCell.Value.Second));
				actor.SetTargetLine(Target.FromCell(w, exitSubCell.Value.First, exitSubCell.Value.Second), Color.Green, false);
                
                if(cargo.Info.WillDisappear)
                    w.Add(actor);
			});

			if (!unloadAll || cargo.IsEmpty(self))
				return NextActivity;

			cargo.Unloading = true;

            //if (cargo.GetBunkeredNumber() == 0)
            //{
            //    cargo.RevokeCondition();
            //}

			return this;
		}
	}
}
