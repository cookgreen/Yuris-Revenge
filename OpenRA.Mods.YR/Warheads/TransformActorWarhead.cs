using OpenRA.Mods.Common.Warheads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Traits;
using OpenRA.Primitives;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;

namespace OpenRA.Mods.YR.Warheads
{
    public class TransformActorWarhead : Warhead, ITick
    {
        [Desc("Which actor did you wish to transfrom to?")]
        public readonly string Actor = null;
        [Desc("Which sequence did you wish to play when tranforming an actor?")]
        public readonly string Sequence = null;
        public readonly int Facing = 128;
        private int delay = -1;
        private Actor actor;
        private TypeDictionary typeDic;
        public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
        {
            World w = firedBy.World;
            WPos targetPos = target.CenterPosition;
            var victimActors = w.FindActorsInCircle(targetPos, new WDist(1));
            foreach (Actor victimActor in victimActors)
            {
                victimActor.Kill(firedBy);

                actor = firedBy;
                CPos pos = victimActor.World.Map.CellContaining(victimActor.CenterPosition);

                SpriteEffect effect = new SpriteEffect(targetPos, w, victimActor.Info.Name, Sequence, "colorpicker");
                w.Add(effect);

                typeDic = new TypeDictionary()
                {
                    new CenterPositionInit(targetPos),
                    new LocationInit(pos),
                    new OwnerInit(firedBy.Owner),
                    new FacingInit(Facing)
                };
                delay = 105;
            }
        }

        public void Tick(Actor self)
        {
            if (delay >= 0)
            {
                if (delay == 0)
                {
                    World w = actor.World;
                    w.CreateActor(Actor, typeDic);
                    delay = -1;
                }
                else
                {
                    delay--;
                }
            }
        }
    }
}
