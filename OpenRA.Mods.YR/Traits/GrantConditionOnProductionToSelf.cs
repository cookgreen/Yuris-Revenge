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
    //OpenRA also have a trait like this, but that trait will apply to the producted unit itself, 
    //and this trait will apply to the productor itself
    //When OpenRA support this feature, this trait will be removed
    public class GrantConditionOnProductionToSelfInfo : ConditionalTraitInfo
    {
        [Desc("The condition will be granted when these units were produced")]
        public string[] UnitNames;

        [Desc("Condition will be granted")]
        public string Condition = null;

        [Desc("Condition will be revoked when passed the delay")]
        public int ConditionDelay = 100;
        public override object Create(ActorInitializer init)
        {
            return new GrantConditionOnProductionToSelf(init, this);
        }
    }

    public class GrantConditionOnProductionToSelf : ConditionalTrait<GrantConditionOnProductionToSelfInfo>, INotifyProduction, ITick
    {
        private int delay = -1;
        private ConditionManager conditionManager;
        private GrantConditionOnProductionToSelfInfo info;
        private int conditionToken = ConditionManager.InvalidConditionToken;
        public GrantConditionOnProductionToSelf(ActorInitializer init, GrantConditionOnProductionToSelfInfo info) : base(info)
        {
            this.info = info;
        }

        protected override void Created(Actor self)
        {
            conditionManager = self.Trait<ConditionManager>();
        }

        void ITick.Tick(Actor self)
        {
            if (delay >= 0)
            {
                if (delay == 0)
                {
                    if (conditionToken != ConditionManager.InvalidConditionToken)
                    {
                        self.World.AddFrameEndTask(w =>
                        {
                            conditionToken = conditionManager.RevokeCondition(self, conditionToken);
                        });
                    }

                    delay = -1;
                }
                else
                {
                    delay--;
                }
            }
        }

        void INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)
        {
            if (info.UnitNames.Contains(other.Info.Name))
            {
                conditionToken = conditionManager.GrantCondition(self, info.Condition);
                delay = info.ConditionDelay;
            }
        }
    }
}
