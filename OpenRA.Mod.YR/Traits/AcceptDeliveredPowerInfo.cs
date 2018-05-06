using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA;

namespace OpenRA.Mod.YR.Traits
{
    [Desc("Tag trait for actors with `DeliversCash`.")]
    class AcceptDeliveredPowerInfo : ITraitInfo
    {
        [Desc("Accepted `DeliversPower` types. Leave empty to accept all types.")]
        public readonly HashSet<string> ValidTypes = new HashSet<string>();

        [Desc("Stance the delivering actor needs to enter.")]
        public readonly Stance ValidStances = Stance.Ally;
        public object Create(ActorInitializer init)
        {
            throw new NotImplementedException();
        }
    }

    class AcceptDeliveredPower
    {
        readonly AcceptDeliveredPowerInfo info;
        public AcceptDeliveredPower(AcceptDeliveredPowerInfo info)
        {
            this.info = info;
        }
    }
}
