using OpenRA.Mods.Common.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits.SupportPowers
{
    public interface INotifySupportPowerCharged
    {
        void Charged(Actor self, string key);
    }

    public interface INotifySupportPowerActived
    {
        void Active(Actor self, Order order, SupportPowerManager manager);
    }

    public class SupportPowerWithNotifyInfo : SupportPowerInfo
    {
        public override object Create(ActorInitializer init)
        {
            return new SupportPowerWithNotify(init.Self, this);
        }
    }

    public class SupportPowerWithNotify : SupportPower
    {
        public SupportPowerWithNotify(Actor self, SupportPowerInfo info) : base(self, info)
        {
        }

        public override void Charged(Actor self, string key)
        {
            base.Charged(self, key);

            var notifies = self.TraitsImplementing<INotifySupportPowerCharged>();
            foreach (var notify in notifies)
            {
                notify.Charged(self, key);
            }
        }

        public override void Activate(Actor self, Order order, SupportPowerManager manager)
        {
            base.Activate(self, order, manager);

            var notifies = self.TraitsImplementing<INotifySupportPowerActived>();
            foreach (var notify in notifies)
            {
                notify.Active(self, order, manager);
            }
        }
    }
}
