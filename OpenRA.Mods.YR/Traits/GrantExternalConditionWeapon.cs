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
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits.Conditions
{
    public enum WeaponType
    {
        Single,
        Range,
    }

    public enum DamageRangeType
    {
        Self,
        Target,
    }
    /// <summary>
    /// This kind of weapon can grant a external condition to the victim
    /// </summary>
    public class GrantExternalConditionWeaponInfo : ITraitInfo
    {
        [FieldLoader.Require]
        [Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
        public readonly string Condition = null;

        [Desc("Duration of the condition (in ticks). Set to 0 for a permanent condition.")]
        public readonly int EffectDuration = 0;

        [Desc("The armament which weapon the grant will attack the target. (== \"Name:\" tag of Armament, not @tag!)")]
        [WeaponReference]
        public readonly string ArmamentName = "primary";

        [Desc("Which type the armament is? Single - will effect one target, Range - will effect many targets")]
        public readonly WeaponType ArmamentType = WeaponType.Range;

        [Desc("What kind of damage type is? Self - Damage is circle with center point self, Target - Damage is circle with center point target")]
        public readonly DamageRangeType DamageRangeType = DamageRangeType.Self;

        [Desc("Range of the armament")]
        public readonly int ArmamentRange = 6;

        [Desc("Range of the damage")]
        public readonly int DamageRange = 1;

        public object Create(ActorInitializer init)
        {
            return new GrantExternalConditionWeapon(init, this);
        }
    }

    public class GrantExternalConditionWeapon : ITick, INotifyAttack
    {
        private GrantExternalConditionWeaponInfo info;
        private Actor self;
        public GrantExternalConditionWeapon(ActorInitializer init, GrantExternalConditionWeaponInfo info)
        {
            this.info = info;
            self = init.Self;
        }

        public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
        {
            if (a.Info.Name != info.ArmamentName)
                return;

            switch(info.ArmamentType)
            {
                case WeaponType.Range:
                    var actors = self.World.FindActorsInCircle(self.CenterPosition, WDist.FromCells(info.ArmamentRange));
                    foreach (var actor in actors)
                    {
                        var external = actor.TraitsImplementing<ExternalCondition>()
                            .FirstOrDefault(t => t.Info.Condition == info.Condition && t.CanGrantCondition(actor, self));

                        if (external != null)
                            external.GrantCondition(actor, self, info.EffectDuration);
                    }
                    break;
                case WeaponType.Single:
                    var thisVictimExternal = target.Actor.TraitsImplementing<ExternalCondition>()
                            .FirstOrDefault(t => t.Info.Condition == info.Condition && t.CanGrantCondition(target.Actor, self));

                    if (thisVictimExternal != null)
                        thisVictimExternal.GrantCondition(target.Actor, self, info.EffectDuration);

                    switch (info.DamageRangeType)
                    {
                        case DamageRangeType.Target:
                            var victimActors = self.World.FindActorsInCircle(target.Actor.CenterPosition, WDist.FromCells(info.DamageRange));
                            foreach (var actor in victimActors)
                            {
                                var victimExternal = actor.TraitsImplementing<ExternalCondition>()
                                    .FirstOrDefault(t => t.Info.Condition == info.Condition && t.CanGrantCondition(actor, self));

                                if (victimExternal != null)
                                    victimExternal.GrantCondition(actor, self, info.EffectDuration);
                            }
                            break;
                    }
                    break;
            }
        }

        public void PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

        public void Tick(Actor self)
        {
        }
    }
}
