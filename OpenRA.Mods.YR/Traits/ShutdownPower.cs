using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    public class ShutdownPowerInfo : ConditionalTraitInfo
    {
        public readonly int Duration = 0;
        public override object Create(ActorInitializer init)
        {
            return new ShutdownPower(init, this);
        }
    }

    public class ShutdownPower : ConditionalTrait<ShutdownPowerInfo>, ITick, INotifyOwnerChanged
    {
        private Actor self;
        private ShutdownPowerInfo info;
        private PowerManager powerMgr;
        public ShutdownPower(ActorInitializer init, ShutdownPowerInfo info) : base(info)
        {
            self = init.Self;
            this.info = info;
            powerMgr = self.Owner.PlayerActor.Trait<PowerManager>();
        }

        public void Tick(Actor self)
        {
            if (!IsTraitDisabled && powerMgr.PowerState != PowerState.Low)
            {
                powerMgr.TriggerPowerOutage(info.Duration);
            }
            else if (IsTraitDisabled)
            {
                powerMgr.TriggerPowerOutage(0);
            }
        }

        public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
        {
            powerMgr = newOwner.PlayerActor.Trait<PowerManager>();
        }
    }
}
