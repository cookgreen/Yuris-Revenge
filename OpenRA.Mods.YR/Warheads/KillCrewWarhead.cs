#region Copyright & License Information
/*
 * Written by Cook Green of YR Mod
 * Follows GPLv3 License as the OpenRA engine:
 * 
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion
using OpenRA.Mods.Common.Warheads;
using OpenRA.Mods.YR.Traits;
using OpenRA.Primitives;
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
    }
}
