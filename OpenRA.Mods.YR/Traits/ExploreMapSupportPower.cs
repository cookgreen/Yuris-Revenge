using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    public class ExploreMapSupportPowerInfo : SupportPowerInfo, IRulesetLoaded
    {
        [Desc("Radius of the explore map support power")]
        public readonly int Radius = 6;

        [Desc("Image used by playing the sequence")]
        public readonly string Image = null;

        [Desc("Sequence played when explore specific destination")]
        public readonly string Sequence = null;

        [Desc("Platte which applied to the sequence")]
        [PaletteReference]
        public readonly string Platte = null;

        public override object Create(ActorInitializer init)
        {
            return new ExploreMapSupportPower(init.Self, this);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ExploreMapSupportPower : SupportPower
    {
        private ExploreMapSupportPowerInfo info;
        public ExploreMapSupportPower(Actor self, ExploreMapSupportPowerInfo info) : base(self, info)
        {
            this.info = info;
        }

        public override void Activate(Actor self, Order order, SupportPowerManager manager)
        {
            base.Activate(self, order, manager);
            self.World.AddFrameEndTask(w =>
            {
                WPos destPosition = order.Target.CenterPosition;
                Shroud shround = self.Owner.Shroud;
                var cells = Shroud.ProjectedCellsInRange(self.World.Map, destPosition, WDist.FromCells(info.Radius));

                if (!string.IsNullOrEmpty(info.Sequence))
                {
                    string palette = null;
                    if (info.Platte == "player")
                    {
                        palette = "player" + self.Owner.InternalName;
                    }
                    else
                    {
                        palette = info.Platte;
                    }
                    self.World.Add(new SpriteEffect(destPosition, self.World, info.Image, info.Sequence, palette));
                }

                shround.ExploreProjectedCells(self.World, cells);
            });
        }
    }
}
