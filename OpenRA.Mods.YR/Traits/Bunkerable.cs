#region Copyright & License Information
/*
 * Written by Cook Green of YR Mod
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
