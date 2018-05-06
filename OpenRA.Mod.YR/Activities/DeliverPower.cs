using OpenRA.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mod.YR.Activities
{
    class DeliverPower : Enter
    {
        readonly Actor target;
        public DeliverPower(Actor self, Actor target, EnterBehaviour enterBehaviour, int maxTries = 1, bool repathWhileMoving = true) : 
            base(self, target, enterBehaviour, maxTries, repathWhileMoving)
        {
            this.target = target;
        }

        protected override void OnInside(Actor self)
        {
            if (target.IsDead)
                return;

            
        }
    }
}
