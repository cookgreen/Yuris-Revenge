using OpenRA.Mods.Common.Warheads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.YR.Warheads
{
    public class InfraredMarkerWarhead : Warhead
    {
        [Desc("Number of the migs which will attack")]
        public readonly int MigsNumber = 2;

        public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
        {
            World world = firedBy.World;
            //Render the Infrared Marker Effect, a solid red line
            //Play the sound "migs on the way" or "target required"
            //Spawn the migs defined by 'MigsNumber', fly over the the target position, drop the missile and destroy it
            //destroy the render effect and the migs if the target has been destroyed


            Actor[] migs = new Actor[MigsNumber];
            for (int i = 0; i < MigsNumber; i++)
            {
                migs[i] = world.CreateActor("mig", new Primitives.TypeDictionary());
                migs[i].QueueActivity(new Fly(migs[i], target, WDist.Zero, new WDist(100)));
                migs[i].PlayVoice("MigAttack");

                world.AddFrameEndTask(w =>
                {
                    AttackBase[] attackBases = migs[i].TraitsImplementing<AttackBase>().ToArray();
                    foreach (var ab in attackBases)
                    {
                        if (ab.IsTraitDisabled)
                            continue;

                        if (target.Actor == null)
                            ab.AttackTarget(target, false, true, true); // force fire on the ground.
                        else if (target.Actor.Owner.Stances[firedBy.Owner] == Stance.Ally)
                            ab.AttackTarget(target, false, true, true); // force fire on ally.
                        else if (target.Actor.Owner.Stances[firedBy.Owner] == Stance.Neutral)
                            ab.AttackTarget(target, false, true, true); // force fire on neutral.
                        else
                            /* Target deprives me of force fire information.
                             * This is a glitch if force fire weapon and normal fire are different, as in
                             * RA mod spies but won't matter too much for carriers. */
                            ab.AttackTarget(target, false, true, target.RequiresForceFire);
                    }
                });
            }
        }
    }
}
