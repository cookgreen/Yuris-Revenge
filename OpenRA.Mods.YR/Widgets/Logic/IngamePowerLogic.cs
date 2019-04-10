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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Widgets.Logic
{
    public class IngamePowerLogic : ChromeLogic
    {
        [ObjectCreator.UseCtor]
        public IngamePowerLogic(Widget widget, World world)
        {
            var powerManager = world.LocalPlayer.PlayerActor.Trait<PowerManager>();


            var powerBar = widget.Get<ResourceBarSpriteWidget>("POWERBAR");

            powerBar.GetProvided = () => powerManager.PowerProvided;
            powerBar.GetUsed = () => powerManager.PowerDrained;
            powerBar.TooltipFormat = "Power Usage: {0}/{1}";
            powerBar.GetPowerState = () =>
            {
                if (powerManager.PowerState == PowerState.Critical)
                    return "low";
                if (powerManager.PowerState == PowerState.Low)
                    return "middle";
                return "heigh";
            };
        }
    }
}
