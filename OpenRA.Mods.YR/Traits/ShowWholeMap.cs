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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    public class ShowWholeMapInfo : ConditionalTraitInfo
    {
        public override object Create(ActorInitializer init)
        {
            return new ShowWholeMap(init, this);
        }
    }
    public class ShowWholeMap : ConditionalTrait<ShowWholeMapInfo>, INotifyOwnerChanged, ITick, INotifySold, INotifyKilled
    {
        private PowerManager powerMgr;
        private ShowWholeMapInfo info;
        private Actor self;
        private bool disableShround;
        public ShowWholeMap(ActorInitializer init, ShowWholeMapInfo info) : base(info)
        {
            self = init.Self;
            this.info = info;
            powerMgr = self.Owner.PlayerActor.Trait<PowerManager>();
        }

        public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
        {
            if(newOwner.InternalName=="Netural")
            {
                return;
            }

            if(self.Owner == oldOwner)
            {
                disableShround = true;
            }
            else if(self.Owner == newOwner)
            {
                disableShround = false;
            }

            powerMgr = newOwner.PlayerActor.Trait<PowerManager>();

            oldOwner.Shroud.Disabled = false;
            newOwner.Shroud.Disabled = true;
        }

        public void Tick(Actor self)
        {
            if(powerMgr.PowerState == PowerState.Low)
            {
                //low power
                disableShround = false;
            }
            else
            {
                disableShround = true;
            }

            if (this.self.IsDead)
            {
                disableShround = false;
            }
            UpdateShroundState(disableShround);
        }

        protected override void Created(Actor self)
        {
            disableShround = true;
            UpdateShroundState(disableShround);

            base.Created(self);
        }

        public void Selling(Actor self)
        {
        }

        public void Sold(Actor self)
        {
            disableShround = false;
            UpdateShroundState(disableShround);
        }

        public void Killed(Actor self, AttackInfo e)
        {
            var allActorsCanShowWholeMap = this.self.Owner.World.Actors.Where(o => o.TraitsImplementing<ShowWholeMap>().Count() > 0);
            if (allActorsCanShowWholeMap.Count() > 0)
            {
                //We still have other actors that can show whole map, let's check power
                if (powerMgr.PowerState == PowerState.Low)
                {
                    //low power
                    disableShround = false;
                }
                else
                {
                    disableShround = true;
                }
                UpdateShroundState(disableShround);
            }
            else
            {
                disableShround = false;
                UpdateShroundState(disableShround);
            }
        }

        private void UpdateShroundState(bool disableShround)
        {
            this.self.Owner.Shroud.Disabled = disableShround;
            if (self.World.LocalPlayer == self.Owner)
                self.World.RenderPlayer = disableShround ? null : self.Owner;
        }
    }
}
