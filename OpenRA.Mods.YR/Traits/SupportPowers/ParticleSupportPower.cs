#region Copyright & License Information
/*
 * Modded by Cook Green of YR Mod.
 * Modded from NukeLaunch.cs but change a lot
 * 
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion
using OpenRA.GameRules;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    public class WeaponBrust
    {
        private WeaponInfo weapon;
        private int offsetX;
        private int offsetY;
        private int offsetZ;

        public WeaponInfo Weapon
        {
            get
            {
                return weapon;
            }
        }
        public int OffsetX
        {
            get
            {
                return offsetX;
            }
        }
        public int OffsetY
        {
            get
            {
                return offsetY;
            }
        }
        public int OffsetZ
        {
            get
            {
                return offsetZ;
            }
        }

        public WeaponBrust(WeaponInfo weapon, int[] positionOffset)
        {
            this.weapon = weapon;
            offsetX = positionOffset[0];
            offsetY = positionOffset[1];
            offsetZ = positionOffset[2];
        }
    }
    public class ParticleSupportPowerInfo : SupportPowerInfo, IRulesetLoaded
    {
        [Desc("What weapons will attack the target in the range?")]
        //Weapon-Offset dictionary
        public readonly Dictionary<string, int[]> Weapons = null;

        [Desc("Effect Range")]
        public readonly int RangeTotal = 10;

        [Desc("Actor Effect Animation when was striked by this support power")]
        public readonly string EffectSequence = null;

        [Desc("Will change owner of the undead actor to player?")]
        public readonly bool ChangeOwner = false;

        [Desc("Actor type will be effected by `ChangeOwner`")]
        public readonly BitSet<TargetableType> ChangeTargets;

        [Desc("The `ChangeOwner` Effect Range")]
        public readonly int ChangeRange = -1;

        [Desc("Corresponds to `Type` from `EnvironmentEffectPaletteEffect` on the world actor.")]
        public readonly string EnvironmentEffectType = null;

        public List<WeaponBrust> WeaponInfos { get; private set; }

        public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
        {
            if (Weapons == null)
            {
                throw new YamlException("Weapons Ruleset can't be empty!");
            }
            WeaponInfos = new List<WeaponBrust>();
            for (int i = 0; i < Weapons.Count; i++)
            {
                string Weapon = Weapons.ElementAt(i).Key;
                WeaponInfo weapon;
                var weaponToLower = (Weapon ?? string.Empty).ToLowerInvariant();
                if (!rules.Weapons.TryGetValue(weaponToLower, out weapon))
                    throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

                WeaponInfos.Add(new WeaponBrust(weapon, Weapons.ElementAt(i).Value));
            }
            base.RulesetLoaded(rules, ai);
        }
        public override object Create(ActorInitializer init)
        {
            return new ParticleSupportPower(init.Self, this);
        }
    }
    public class ParticleSupportPower : SupportPower
    {
        private ParticleSupportPowerInfo info;

        public ParticleSupportPower(Actor self, ParticleSupportPowerInfo info) : base(self, info)
        {
            this.info = info;
        }

        public override void Activate(Actor self, Order order, SupportPowerManager manager)
        {
            base.Activate(self, order, manager);

            self.World.AddFrameEndTask(w => {
                WPos location = self.CenterPosition;
                WPos targetPos = order.Target.CenterPosition;

                PlayLaunchSounds();

                if (!string.IsNullOrEmpty(info.EnvironmentEffectType))
                {
                    EnvironmentPaletteEffect environmentEffect = null;
                    var effetcs = w.WorldActor.TraitsImplementing<EnvironmentPaletteEffect>();
                    foreach (var effect in effetcs)
                    {
                        if (effect.Info.Type == info.EnvironmentEffectType)
                        {
                            environmentEffect = effect;
                            break;
                        }
                    }
                    if (environmentEffect != null)
                    {
                        environmentEffect.Enable(-1);
                    }
                }

                for (int i = 0; i < info.WeaponInfos.Count; i++)
                {
                    WeaponInfo weaponInfo = info.WeaponInfos[i].Weapon;
                    if (weaponInfo.Report != null && weaponInfo.Report.Any())
                        Game.Sound.Play(SoundType.World, weaponInfo.Report.Random(self.World.SharedRandom), order.Target.CenterPosition);

                    //Boooooom......
                    WVec offset = new WVec(
                        info.WeaponInfos[i].OffsetX,
                        info.WeaponInfos[i].OffsetY,
                        info.WeaponInfos[i].OffsetZ
                    );
                    WPos newPos = targetPos + offset;
                    weaponInfo.Impact(Target.FromPos(newPos), self, Enumerable.Empty<int>());

                    var victimActors = w.FindActorsInCircle(targetPos, weaponInfo.Range);
                    foreach(Actor actor in victimActors)
                    {
                        foreach(IWarhead warhead in weaponInfo.Warheads)
                        {
                            if(warhead is SpreadDamageWarhead)
                            {
                                actor.InflictDamage(self, new Damage(((SpreadDamageWarhead)warhead).Damage));
                            }
                        }
                    }
                }

                if (info.ChangeOwner)
                {
                    var aliveActors = w.FindActorsInCircle(targetPos, WDist.FromCells(info.ChangeRange));
                    foreach (Actor aliveActor in aliveActors)
                    {
                        try
                        {
                            TargetableInfo ti = aliveActor.Info.TraitInfo<TargetableInfo>();
                            if (ti == null)
                            {
                                continue;
                            }
                            foreach (var tti in ti.GetTargetTypes())
                            {
                                if (info.ChangeTargets.Contains(tti) && aliveActor.Owner.Stances[self.Owner] == Stance.Enemy)
                                {
                                    aliveActor.ChangeOwner(self.Owner);
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            });
        }
    }
}
