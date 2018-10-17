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
    /// <summary>
    /// Used by Yuri Virus Unit 
    /// </summary>
    public class VirusWarhead : Warhead
    {
        public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
        {
            World world = firedBy.World;
            //when target is dead, play the victim poision animation at the target position
            
        }
    }
}
