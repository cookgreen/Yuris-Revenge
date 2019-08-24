#region Copyright & License Information
/*
 * Written by Cook Green of YR Mod
 * Follows GPLv3 License as the OpenRA engine:
 * 
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    public class GrantConditionOnCaptureInfo : ConditionalTraitInfo
    {
        [FieldLoader.Require]
        [GrantedConditionReference]
        [Desc("Grant a condition when a faction capture this actor")]
        public readonly string GrantCaptureCondition;
        public override object Create(ActorInitializer init)
        {
            return new GrantConditionOnCapture(init, this);
        }
    }
    public class GrantConditionOnCapture : ConditionalTrait<GrantConditionOnCaptureInfo>, INotifyOwnerChanged, ITick
    {
        private int conditionToken;
        ConditionManager conditionManager;
        private GrantConditionOnCaptureInfo info;
        private Player thisPlayer;
        private Player oldPlayer;
        private string thisFactionName;

        public GrantConditionOnCapture(ActorInitializer init, GrantConditionOnCaptureInfo info) : base(info)
        {
            this.info = info;
            thisFactionName = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
        }


        protected override void Created(Actor self)
        {
            conditionManager = self.Trait<ConditionManager>();

            base.Created(self);
        }

        public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
        {
            if(newOwner.InternalName == "Netural")
            {
                return;
            }
            if (thisPlayer != newOwner)
            {
                thisPlayer = newOwner;
                oldPlayer = oldOwner;

                TraitDisabled(self);
                TraitEnabled(self);
                
            }
        }
        protected override void TraitEnabled(Actor self)
        {
            if (conditionToken == ConditionManager.InvalidConditionToken)
            {
                //Grant condition to all actors belong to this faction
                World w = self.World;
                var actorsBelongToThisFaction = w.Actors.Where(o => o.Owner == thisPlayer);
                foreach (var actor in actorsBelongToThisFaction)
                {
                    conditionToken = conditionManager.GrantCondition(actor, info.GrantCaptureCondition);
                }
            }
        }

        protected override void TraitDisabled(Actor self)
        {
            if (conditionToken == ConditionManager.InvalidConditionToken)
                return;

            //Disable condition to all actors belong to old faction
            World w = self.World;
            var actorsBelongToOldFaction = w.Actors.Where(o => o.Owner == oldPlayer);
            foreach (var actor in actorsBelongToOldFaction)
            {
                conditionToken = conditionManager.RevokeCondition(actor, conditionToken);
            }
        }

        public void Tick(Actor self)
        {
            if(self.IsDead)
            {
                if (conditionToken == ConditionManager.InvalidConditionToken)
                    return;

                //Disable condition to all actors belong to this faction when the actor is dead
                World w = self.World;
                var actors = w.Actors.Where(o => o.Owner == thisPlayer);
                foreach (var actor in actors)
                {
                    conditionToken = conditionManager.RevokeCondition(actor, conditionToken);
                }
            }
        }
    }
}
