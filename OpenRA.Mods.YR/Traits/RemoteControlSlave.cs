using OpenRA.Mods.Common.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    public class RemoteControlSlaveInfo : ConditionalTraitInfo
    {
        public override object Create(ActorInitializer init)
        {
            return new RemoteControlSlave(init, this);
        }
    }
    public class RemoteControlSlave : ConditionalTrait<RemoteControlSlaveInfo>
    {
        private int remoteConditionToken = ConditionManager.InvalidConditionToken;
        private RemoteControlMaster master;
        private ConditionManager conditionManager;
        private Actor self;
        public bool HasMaster
        {
            get
            {
                return master != null;
            }
        }
        public RemoteControlSlave(ActorInitializer init, RemoteControlSlaveInfo info) : base(info)
        {
            self = init.Self;
        }

        protected override void Created(Actor self)
        {
            conditionManager = self.Trait<ConditionManager>();

            base.Created(self);
        }

        public void GrandCondition(string condition)
        {
            remoteConditionToken = conditionManager.GrantCondition(self, condition);
        }

        public void RevokeCondition()
        {
            remoteConditionToken = conditionManager.RevokeCondition(self, remoteConditionToken);
        }

        public void LinkMaster(RemoteControlMaster master)
        {
            this.master = master;
        }
    }
}
