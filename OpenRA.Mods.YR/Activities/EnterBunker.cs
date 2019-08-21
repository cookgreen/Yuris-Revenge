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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.YR.Activities
{
	class EnterBunker : Enter
	{
		readonly BunkerPassenger bunkerPassenger;
		readonly int maxTries;
		Actor bunkerActor;
		BunkerCargo bunkerCargo;
        bool willDisappear;
        WPos targetPos;
        RenderSprites rs;
        Animation bunkerAnimation;

        public EnterBunker(Actor passengerActor, Actor bunkerActor, WPos pos, bool willDisappear=true, int maxTries = 0, bool repathWhileMoving = true)
			: base(passengerActor, Target.FromActor(bunkerActor))
		{
			this.bunkerActor = bunkerActor;
			bunkerCargo = bunkerActor.Trait<BunkerCargo>();
            bunkerPassenger = passengerActor.Trait<BunkerPassenger>();
            this.maxTries = maxTries;
            this.willDisappear = willDisappear;
            targetPos = pos;
            rs = bunkerActor.TraitOrDefault<RenderSprites>();
            bunkerAnimation = new Animation(bunkerActor.World, bunkerActor.Info.Name);
        }

        protected override void OnEnterComplete(Actor self, Actor targetActor)
        {
            self.World.AddFrameEndTask(w =>
            {
                Mobile mobile = self.TraitOrDefault<Mobile>();
                if (self.IsDead || bunkerActor.IsDead || !bunkerCargo.CanLoad(bunkerActor, self))
                    return;

                if (!string.IsNullOrEmpty(bunkerCargo.Info.SequenceOnCargo))
                {
                    w.Add(new SpriteEffect(bunkerActor.CenterPosition, w, bunkerActor.Info.Name, bunkerCargo.Info.SequenceOnCargo, "player" + self.Owner.InternalName));
                }

                if (bunkerCargo.GetBunkeredNumber() == 0)
                {
                    bunkerCargo.ChangeState(BunkerState.Bunkered);
                    if (bunkerCargo.Info.ChangeOwnerWhenGarrison)
                    {
                        bunkerActor.ChangeOwner(self.Owner);
                    }

                    if (!string.IsNullOrEmpty(bunkerCargo.Info.StructureGarrisonSound))
                    {
                        Game.Sound.PlayToPlayer(SoundType.World, self.Owner, bunkerCargo.Info.StructureGarrisonSound);
                    }

                    if (!string.IsNullOrEmpty(bunkerCargo.Info.StructureGarrisonedNotification))
                    {
                        Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", bunkerCargo.Info.StructureGarrisonedNotification, self.Owner.Faction.InternalName);
                    }
                }
                bunkerCargo.Load(bunkerActor, self);
                bunkerPassenger.GrantCondition();
                if (willDisappear)
                {
                    w.Remove(self);
                }
                else
                {
                    //If didn't disappear, then move the passenger actor to the bunker center
                    self.QueueActivity(mobile.VisualMove(self, self.CenterPosition, bunkerActor.CenterPosition));
                    mobile.SetVisualPosition(self, bunkerActor.CenterPosition);
                }
            });
        }

        protected override bool TryStartEnter(Actor self, Actor targetActor)
        {
            return bunkerCargo.Unloading || bunkerCargo.CanLoad(bunkerActor, self);
        }

        protected override void OnLastRun(Actor self)
        {
            bunkerPassenger.Unreserve(self);
        }

        protected override void OnCancel(Actor self)
        {
            bunkerPassenger.Unreserve(self);
        }
    }
}
