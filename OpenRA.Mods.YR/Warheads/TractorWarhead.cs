#region Copyright & License Information
/*
 * Written by Boolbada of OP Mod.
 * Follows GPLv3 License as the OpenRA engine:
 *
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;
using OpenRA.Mods.YR.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Warheads;

namespace OpenRA.Mods.YR.Warheads
{
	[Desc("Can this warhead lift the actor that has Tractable trait and move it next to self by force?")]
	public class TractorWarhead : DamageWarhead
	{
		[Desc("Let his be -1, 0, 1, or anything else to modify the traction speed.")]
		public readonly int CruiseSpeedMultiplier = 1;

		public override void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers)
        {
            var victims = firedBy.World.FindActorsOnCircle(pos, WDist.FromCells(1));
            foreach (var victim in victims)
            {
                var targetTractable = victim.TraitOrDefault<Tractable>();
                if (targetTractable == null)
                    return;

                targetTractable.Tract(victim, firedBy, CruiseSpeedMultiplier);
            }
        }
	}
}
