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
