using OpenRA.Mods.Common.Warheads;
using OpenRA.Mods.YR.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Warheads
{
    public class KillCrewWarhead : SpreadDamageWarhead
    {
        public override void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers)
        {
            base.DoImpact(pos, firedBy, damageModifiers);

            World w = firedBy.World;

            Player neutralPlayer = null;

            Player[] players = w.Players;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].InternalName == "Neutral")
                {
                    neutralPlayer = players[i];
                    break;
                }
            }

            var victimActors = w.FindActorsInCircle(pos, new WDist(1));
            foreach (Actor victim in victimActors)
            {
                if (victim.TraitsImplementing<CrewKillable>().Count() > 0)//This actor can be crew killed
                {
                    if (neutralPlayer != null)
                    {
                        victim.ChangeOwner(neutralPlayer);
                    }
                }
            }
        }
        public override void DoImpact(Actor victim, Actor firedBy, IEnumerable<int> damageModifiers)
        {
        }
    }
}
