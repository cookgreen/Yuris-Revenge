using OpenRA.Mods.Common.Warheads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA;
using OpenRA.Traits;

namespace yr.Warheads
{
    class VirusWarhead : Warhead
    {
        public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
        {
            World world = firedBy.World;
            
        }
    }
}
