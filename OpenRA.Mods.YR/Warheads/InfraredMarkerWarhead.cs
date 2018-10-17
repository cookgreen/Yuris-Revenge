using OpenRA.Mods.Common.Warheads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Traits;

namespace OpenRA.Mods.YR.Warheads
{
    public class InfraredMarkerWarhead : Warhead
    {
        [Desc("Number of the migs which will attack")]
        public int MigsNumber { get; }

        public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
        {
            World world = firedBy.World;
            //Render the Infrared Marker Effect, a solid red line
            //Play the sound "migs on the way" or "target required"
            //Spawn the migs defined by 'MigsNumber', fly over the the target position, drop the missile and destroy it

            Actor mig1 = firedBy.World.CreateActor(true, "mig", new Primitives.TypeDictionary());
            Actor mig2 = firedBy.World.CreateActor(true, "mig", new Primitives.TypeDictionary());

            //destroy the render effect and the migs if the target has been destroyed
        }
    }
}
