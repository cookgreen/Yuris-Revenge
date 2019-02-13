using OpenRA.Mods.Common.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    public class BunkerableInfo : ConditionalTraitInfo
    {
        public override object Create(ActorInitializer init)
        {
            return new Bunkerable(init, this);
        }
    }

    public class Bunkerable : ConditionalTrait<BunkerableInfo>
    {
        public Bunkerable(ActorInitializer init, BunkerableInfo info) : base(info)
        {
        }
    }
}
