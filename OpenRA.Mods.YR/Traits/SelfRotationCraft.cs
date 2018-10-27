using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common;

namespace OpenRA.Mods.YR.Traits
{
    public class SelfRotationCraftInfo : ConditionalTraitInfo
    {
        [Desc("Time that next rotation")]
        public readonly int TickTime = 5;
        [Desc("Angle that each rotation")]
        public readonly int RotateAngle = 20;
        public override object Create(ActorInitializer init)
        {
            return new SelfRotationCraft(init, this);
        }
    }
    /// <summary>
    /// This kind of craft can make rotation itself
    /// </summary>
    public class SelfRotationCraft : ConditionalTrait<SelfRotationCraftInfo>, ITick
    {
        private SelfRotationCraftInfo info;
        private Actor actor;
        private int cycleFacing;
        private int rotFacing;
        private int originalFacing;
        private Aircraft plane;
        public SelfRotationCraft(ActorInitializer init, SelfRotationCraftInfo info) : base(info)
        {
            this.info = info;
            this.actor = init.Self;

            GetCycleFacing(actor);
        }

        private void GetCycleFacing(Actor actor)
        {
            int finalAngle = info.RotateAngle * 360 / 1024;
            WAngle angle = WAngle.FromDegrees(finalAngle);
            int cycle = 360 / finalAngle;
            plane = actor.Trait<Aircraft>();
            originalFacing = plane.Facing;
            rotFacing = angle.Facing;
            cycleFacing = plane.Facing + cycle * rotFacing;
        }

        public void Tick(Actor self)
        {
            plane.Facing += rotFacing;
            if (plane.Facing >= cycleFacing)
            {
                plane.Facing = originalFacing;
            }
        }
    }
}
