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
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.YR.Activities;
using OpenRA.Mods.YR.Orders;

namespace OpenRA.Mods.YR.Traits
{
    [Desc("This actor can enter Cargo actors.")]
	public class BunkerPassengerInfo : ConditionalTraitInfo
    {
		public readonly string CargoType = null;
		public readonly PipType PipType = PipType.Green;
		public readonly int Weight = 1;
        [Desc("Will the actor disappear when enter bunker")]
        public readonly bool WillDisappear = true;
        [Desc("Grant a condition when actor is bunkered")]
        public readonly string GrantBunkerCondition = null;

        [Desc("Use to set when to use alternate transports (Never, Force, Default, Always).",
			"Force - use force move modifier (Alt) to enable.",
			"Default - use force move modifier (Alt) to disable.")]
		public readonly AlternateTransportsMode AlternateTransportsMode = AlternateTransportsMode.Force;

		[Desc("Number of retries using alternate transports.")]
		public readonly int MaxAlternateTransportAttempts = 1;

		[Desc("Range from self for looking for an alternate transport (default: 5.5 cells).")]
		public readonly WDist AlternateTransportScanRange = WDist.FromCells(11) / 2;

		[VoiceReference] public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new BunkerPassenger(init, this); }
	}

	public class BunkerPassenger : ConditionalTrait<BunkerPassengerInfo>, IIssueOrder, IResolveOrder, IOrderVoice, INotifyRemovedFromWorld
	{
		public readonly BunkerPassengerInfo info;
        private ConditionManager conditionManager;
        private Actor self;
		public BunkerPassenger(ActorInitializer init, BunkerPassengerInfo info) : base(info)
		{
			this.info = info;
            self = init.Self;
			Func<Actor, bool> canTarget = IsCorrectCargoType;
			Func<Actor, bool> useEnterCursor = CanEnter;
			Orders = new EnterAlliedActorTargeter<BunkerCargoInfo>[]
			{
				new EnterBunkerTargeter("EnterBunker", 5, canTarget, useEnterCursor, Info.AlternateTransportsMode),
				new EnterBunkersTargeter("EnterBunkers", 5, canTarget, useEnterCursor, Info.AlternateTransportsMode)
			};
		}

        protected override void Created(Actor self)
        {
            conditionManager = self.Trait<ConditionManager>();

            base.Created(self);
        }

        public void GrantCondition()
        {
            conditionManager.GrantCondition(self, info.GrantBunkerCondition);
        }

        public Actor Transport;
		public BunkerCargo ReservedCargo { get; private set; }

		public IEnumerable<IOrderTargeter> Orders { get; private set; }

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "EnterBunker" || order.OrderID == "EnterBunkers")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		bool IsCorrectCargoType(Actor target)
		{
			var ci = target.Info.TraitInfo<BunkerCargoInfo>();
			return ci.Types.Contains(Info.CargoType);
		}

		bool CanEnter(BunkerCargo cargo)
		{
			return cargo != null && cargo.HasSpace(Info.Weight);
		}

		bool CanEnter(Actor target)
		{
			return CanEnter(target.TraitOrDefault<BunkerCargo>());
		}

		public string VoicePhraseForOrder(Actor self, Order order)
        {
            if (order.OrderString != "EnterBunker" && order.OrderString != "EnterBunkers")
                return null;

            if (order.Target.Type != TargetType.Actor || !CanEnter(order.Target.Actor))
                return null;

            return Info.Voice;
        }

		public void ResolveOrder(Actor self, Order order)
        {
            if (order.OrderString != "EnterBunker" && order.OrderString != "EnterBunkers")
                return;

            // Enter orders are only valid for own/allied actors,
            // which are guaranteed to never be frozen.
            if (order.Target.Type != TargetType.Actor)
                return;

            var targetActor = order.Target.Actor;
            if (!CanEnter(targetActor))
                return;

            if (!IsCorrectCargoType(targetActor))
                return;

            if (!order.Queued)
                self.CancelActivity();

            var transports = order.OrderString == "EnterBunkers";
            self.SetTargetLine(order.Target, Color.Green);
            self.QueueActivity(new EnterBunker(self, targetActor, targetActor.CenterPosition, Info.WillDisappear, transports ? Info.MaxAlternateTransportAttempts : 0, !transports));
		}

		public bool Reserve(Actor self, BunkerCargo cargo)
		{
			Unreserve(self);
			if (!cargo.ReserveSpace(self))
				return false;
			ReservedCargo = cargo;
			return true;
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self) { Unreserve(self); }

		public void Unreserve(Actor self)
		{
			if (ReservedCargo == null)
				return;
			ReservedCargo.UnreserveSpace(self);
			ReservedCargo = null;
		}
	}
}
