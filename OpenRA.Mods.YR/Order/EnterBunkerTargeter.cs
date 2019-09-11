#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Mods.YR.Traits;
using OpenRA.Mods.Common.Orders;

namespace OpenRA.Mods.YR.Orders
{
	public class EnterBunkerTargeter : EnterAlliedActorTargeter<BunkerCargoInfo>
	{
		public EnterBunkerTargeter(string order, int priority,
			Func<Actor, TargetModifiers, bool> canTarget, Func<Actor, bool> useEnterCursor)
			: base(order, priority, canTarget, useEnterCursor)
        {

        }

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			//switch (mode)
			//{
			//	case AlternateTransportsMode.None:
			//		break;
			//	case AlternateTransportsMode.Force:
			//		if (modifiers.HasModifier(TargetModifiers.ForceMove))
			//			return false;
			//		break;
			//	case AlternateTransportsMode.Default:
			//		if (!modifiers.HasModifier(TargetModifiers.ForceMove))
			//			return false;
			//		break;
			//	case AlternateTransportsMode.Always:
			//		return false;
			//}

			return base.CanTargetActor(self, target, modifiers, ref cursor);
		}
	}
}
