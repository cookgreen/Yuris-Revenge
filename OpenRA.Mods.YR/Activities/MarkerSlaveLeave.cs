using OpenRA.Mods.Common.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Traits;
using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.YR.Traits;

namespace OpenRA.Mods.YR.Activities
{
    class MarkerSlaveLeave : Activity
    {
        private Actor self;
        private Actor master;
        public MarkerSlaveLeave(Actor self, Actor master)
        {
            this.self = self;
            this.master = master;
        }

        public override Activity Tick(Actor self)
        {
            if (self.IsDead)
                return NextActivity;

            self.World.AddFrameEndTask(w =>
            {
                master.Trait<MarkerMaster>().PickupSlave(master, self);

                self.World.Remove(self);
            });

            return NextActivity;
        }
    }
}
