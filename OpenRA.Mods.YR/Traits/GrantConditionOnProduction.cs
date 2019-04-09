using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    public class GrantConditionOnProductionInfo : ConditionalTraitInfo
    {
        [Desc("The condition will be granted when these units were produced")]
        public string[] UnitNames;

        [Desc("Condition will be granted")]
        public string Condition = null;

        [Desc("Condition will be revoked when passed the delay")]
        public int ConditionDelay = 100;
        public override object Create(ActorInitializer init)
        {
            return new GrantConditionOnProduction(init, this);
        }
    }

    public class GrantConditionOnProduction : ConditionalTrait<GrantConditionOnProductionInfo>, INotifyProduction, ITick
    {
        private int delay = -1;
        private ConditionManager conditionManager;
        private GrantConditionOnProductionInfo info;
        private int conditionToken = ConditionManager.InvalidConditionToken;
        public GrantConditionOnProduction(ActorInitializer init, GrantConditionOnProductionInfo info) : base(info)
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
