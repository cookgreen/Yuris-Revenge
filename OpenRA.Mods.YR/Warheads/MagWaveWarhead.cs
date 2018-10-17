using OpenRA.Mods.Common.Warheads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.YR.Warheads
{
    /// <summary>
    /// Used by Yuri Magnetic Tank
    /// </summary>
    public class MagWaveWarhead : Warhead
    {
        public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
        {
            var world = firedBy.World;
            var debugOverlayRange = new[] { WDist.Zero, new WDist(128) };

            var debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();
            if (debugVis != null && debugVis.CombatGeometry)
                world.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(target.Actor.CenterPosition, debugOverlayRange, DebugOverlayColor);
        }
    }
}
