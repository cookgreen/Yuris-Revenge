using OpenRA.Activities;
using OpenRA.Mods.YR.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Activities
{
    class RemoteControlSlaveProductionComplete : Activity
    {
        private RemoteControlSlave slave;
        public RemoteControlSlaveProductionComplete(RemoteControlSlave slave)
        {
            this.slave = slave;
        }

        public override Activity Tick(Actor self)
        {
            slave.BuildComplete = true;

            return NextActivity;
        }
    }
}
