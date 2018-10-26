using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    /// <summary>
    /// Change victim owner to Netural
    /// </summary>
    public class CrewKillableInfo : PausableConditionalTraitInfo
    {
        public override object Create(ActorInitializer init)
        {
            return new CrewKillable(this);
        }
    }

    public class CrewKillable : PausableConditionalTrait<CrewKillableInfo>
    {
        public CrewKillable(CrewKillableInfo info) : base(info)
        {
        }
    }
}
