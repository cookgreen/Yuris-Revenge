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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

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
    /// This kind of craft can rotate itself
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
            /*
             * TODO: rotate when is moving
             * 
             * OpenRA don't have moving event to catch
             * and the flying activity seems to force aircraft not to rotate
             * maybe need a new trait?
             * maybe need engine update?
             * 
             */
            plane.Facing += rotFacing;
            if (plane.Facing >= cycleFacing)
            {
                plane.Facing = originalFacing;
            }
        }
    }
}
