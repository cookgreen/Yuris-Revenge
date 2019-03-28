using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    public class GrantConditionOnStageInfo : ConditionalTraitInfo
    {
        /// <summary>
        /// condition-delay dictionary
        /// </summary>
        public Dictionary<string, int> Conditions;
        public override object Create(ActorInitializer init)
        {
            return new GrantConditionOnStage(init, this);
        }
    }
    public class GrantConditionOnStage : ConditionalTrait<GrantConditionOnStageInfo>, ITick
    {
        private string currentCondition;
        private Dictionary<string, int> conditions;
        private int delay = -1;
        private ConditionManager conditionManager;
        private int currentConditionToken = ConditionManager.InvalidConditionToken;
        private int currentConditionIndex;
        public GrantConditionOnStage(ActorInitializer init, GrantConditionOnStageInfo info) : base(info)
        {
            this.conditions = info.Conditions;
            currentCondition = conditions.ElementAt(0).Key;
            delay = conditions.ElementAt(0).Value;
            currentConditionIndex = 0;
        }

        protected override void Created(Actor self)
        {
            conditionManager = self.Trait<ConditionManager>();
            currentConditionToken = conditionManager.GrantCondition(self, currentCondition);
        }

        public void Tick(Actor self)
        {
            if (delay >= 0)
            {
                if (delay == 0)
                {
                    if (currentConditionIndex == conditions.Count - 1)
                    {
                        currentConditionIndex = 0;
                    }
                    else
                    {
                        currentConditionIndex++;
                    }
                    currentConditionToken = conditionManager.RevokeCondition(self, currentConditionToken);
                    currentConditionToken = conditionManager.GrantCondition(self, conditions.ElementAt(currentConditionIndex).Key);

                    delay = conditions.ElementAt(currentConditionIndex).Value;
                }
                else
                {
                    delay--;
                }
            }
        }
    }
}
