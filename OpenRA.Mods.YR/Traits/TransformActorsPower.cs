#region Copyright & License Information
/*
 * Modded by Cook Green of YR Mod.
 * Modded from SpawnActorPower.cs but change a lot
 * 
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    public class TransformActorsPowerInfo : SupportPowerInfo
    {
        [Desc("Which actor did you want to transfrom to?")]
        public readonly string Actor = null;
        [Desc("Effect Range")]
        public readonly int Range = 10;

        public readonly string EffectImage = null;
        [SequenceReference("EffectImage")]
        public readonly string EffectSequence = "idle";

        [PaletteReference]
        public readonly string EffectPalette = "player";

        [Desc("Some actor you didn't want to transform")]
        public readonly string ExcludeActor = "brute,jumpjet";
        public override object Create(ActorInitializer init)
        {
            return new TransformActorsPower(init.Self, this);
        }
    }
    /// <summary>
    /// Transform the victim actors to other actors
    /// </summary>
    public class TransformActorsPower : SupportPower, ITick
    {
        private int delay = -1;
        private List<TypeDictionary> dics;
        private TransformActorsPowerInfo info;
        private Actor self;
        private string[] excludeActors;
        public TransformActorsPower(Actor self, TransformActorsPowerInfo info) : base(self, info)
        {
            this.self = self;
            this.info = info;
            if(!string.IsNullOrEmpty(info.ExcludeActor))
            {
                excludeActors = info.ExcludeActor.Split(',');
            }
        }

        public override void Activate(Actor self, Order order, SupportPowerManager manager)
        {
            base.Activate(self, order, manager);

            if (info.Actor != null)
            {
                self.World.AddFrameEndTask(w =>
                {
                    dics = new List<TypeDictionary>();
                    var location = self.World.Map.CenterOfCell(order.TargetLocation);

                    PlayLaunchSounds();
                    //Game.Sound.Play(SoundType.World, info.DeploySound, location);

                    var victimActors = w.FindActorsInCircle(location, WDist.FromCells(10));
                    if (victimActors != null)
                    {
                        foreach (Actor victimActor in victimActors)
                        {
                            if (!victimActor.IsDead &&
                                 victimActor.TraitsImplementing<WithInfantryBody>().Count() > 0 &&
                                 !excludeActors.Contains(victimActor.Info.Name))
                            {
                                WPos victimPos = victimActor.CenterPosition;
                                if (!string.IsNullOrEmpty(info.EffectSequence) && !string.IsNullOrEmpty(info.EffectPalette))
                                {
                                    string palette = null;
                                    if(info.EffectPalette == "player")
                                    {
                                        palette = "player" + self.Owner.InternalName;
                                    }
                                    else
                                    {
                                        palette = info.EffectPalette;
                                    }
                                    w.Add(new SpriteEffect(victimPos, w, victimActor.Info.Name, info.EffectSequence, palette));
                                }
                                CPos pos = victimActor.World.Map.CellContaining(victimActor.CenterPosition);
                                dics.Add(new TypeDictionary
                                {
                                    new CenterPositionInit(victimPos),
                                    new LocationInit(pos),
                                    new OwnerInit(self.Owner),
                                    new FacingInit(128)
                                });
                                victimActor.Kill(self);
                            }
                        }
                        delay = 105;
                    }
                });
            }
        }

        public void Tick(Actor self)
        {
            if (delay >= 0)
            {
                if (delay == 0)
                {
                    self.World.AddFrameEndTask(w =>
                    {
                        foreach (TypeDictionary dic in dics)
                        {
                            var actor = w.CreateActor(info.Actor, dic);
                        }
                        delay = -1;
                    });
                }
                else
                {
                    delay--;
                }
            }
        }
    }
}
