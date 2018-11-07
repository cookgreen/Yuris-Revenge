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
            return new RemoteControlSlave(this);
        }
    }
    public class RemoteControlSlave : ConditionalTrait<RemoteControlSlaveInfo>
    {
        public RemoteControlSlave(RemoteControlSlaveInfo info) : base(info)
        {
        }
    }
}
