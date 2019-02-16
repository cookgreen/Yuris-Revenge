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
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    public class ParticleSupportPowerInfo : SupportPowerInfo, IRulesetLoaded
    {
        [Desc("How will the environment be when deploy this support power?")]
        public readonly string EnvironmentLight = null;

        [Desc("Which weapon will attack the target in the range?")]
        public readonly string Weapons = null;

        [Desc("Effect Range")]
        public readonly int RangeTotal = 10;

        [Desc("Actor Effect Animation when was striked by this support power")]
        public readonly string EffectSequence = null;

        [Desc("Will change owner of the undead actor to player?")]
        public readonly bool ChangeOwner = false;
        public List<WeaponInfo> WeaponInfos { get; private set; }

        public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
        {
            if (string.IsNullOrEmpty(Weapons))
            {
                throw new YamlException("Weapons Ruleset can't be empty!");
            }
            string[] weaponArray = Weapons.Split(',');
            WeaponInfos = new List<WeaponInfo>();
            for (int i = 0; i < weaponArray.Length; i++)
            {
                string Weapon = weaponArray[i];
                WeaponInfo weapon;
                var weaponToLower = (Weapon ?? string.Empty).ToLowerInvariant();
                if (!rules.Weapons.TryGetValue(weaponToLower, out weapon))
                    throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

                WeaponInfos.Add(weapon);
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

                for (int i = 0; i < info.WeaponInfos.Count; i++)
                {
                    WeaponInfo weaponInfo = info.WeaponInfos[i];
                    if (weaponInfo.Report != null && weaponInfo.Report.Any())
                        Game.Sound.Play(SoundType.World, weaponInfo.Report.Random(self.World.SharedRandom), order.Target.CenterPosition);

                    //Boooooom......
                    weaponInfo.Impact(Target.FromPos(targetPos), self, Enumerable.Empty<int>());

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
                        if (info.ChangeOwner)
                        {
                            actor.ChangeOwner(self.Owner);
                        }
                    }
                }
            });
        }
    }
}
