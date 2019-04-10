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

namespace OpenRA.Mods.YR.Traits
{
    public class RemoteControlMasterInfo : ConditionalTraitInfo
    {
        [FieldLoader.Require]
        [GrantedConditionReference]
        [Desc("Grant a condition when this actor create")]
        public readonly string GrantRemoteControlCondition;
        public override object Create(ActorInitializer init)
        {
            return new RemoteControlMaster(init, this);
        }
    }
    public class RemoteControlMaster : ConditionalTrait<RemoteControlMasterInfo>, ITick, INotifyOwnerChanged, INotifyKilled, INotifySold
    {
        private RemoteControlMasterInfo info;
        private int conditionToken;
        private ConditionManager conditionManager;
        private Player newOwner;
        private Player oldOwner;
        private List<Actor> slaves;
        private Actor self;
        public RemoteControlMaster(ActorInitializer init, RemoteControlMasterInfo info) : base(info)
        {
            this.info = info;
            slaves = new List<Actor>();
            self = init.Self;
        }

        protected override void Created(Actor self)
        {
            conditionManager = self.Trait<ConditionManager>();

            if (conditionToken == ConditionManager.InvalidConditionToken)
            {
                //Grant condition to all actors belong to this faction
                World w = self.World;
                var actors = w.Actors.Where(o => o.Owner == self.Owner);
                foreach (var actor in actors)
                {
                    RemoteControlSlave slave = actor.TraitOrDefault<RemoteControlSlave>();
                    if (slave == null)
                    {
                        continue;
                    }
                    if (slave.HasMaster)
                    {
                        continue;
                    }
                    slave.LinkMaster(this);
                    slave.GrandCondition(info.GrantRemoteControlCondition);
                    slaves.Add(actor);
                }
            }

            base.Created(self);
        }

        public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
        {
            World w = self.World;
            if (this.newOwner != newOwner)
            {
                this.newOwner = newOwner;
                this.oldOwner = oldOwner;

                TraitDisabled(self);
                TraitEnabled(self);
            }
        }

        protected override void TraitEnabled(Actor self)
        {
            if (conditionToken == ConditionManager.InvalidConditionToken)
            {
                //Grant condition to all actors belong to this faction
                World w = self.World;
                var actors = w.Actors.Where(o => o.Owner == newOwner);
                foreach (var actor in actors)
                {
                    conditionToken = conditionManager.GrantCondition(actor, info.GrantRemoteControlCondition);
                }
            }
        }

        protected override void TraitDisabled(Actor self)
        {
            if (conditionToken == ConditionManager.InvalidConditionToken)
                return;

            List<Actor> actors;
            bool ret = IsDisableAllSlaves(self, out actors);
            if (ret)
            {
                if (actors == null)
                {
                    return;
                }
                foreach (var actor in actors)
                {
                    conditionToken = conditionManager.RevokeCondition(actor, conditionToken);
                }
            }
        }

        public void Tick(Actor self)
        {
            World w = self.World;
            var actors = w.Actors.Where(o => o.Owner == self.Owner);
            foreach (var actor in actors)
            {
                RemoteControlSlave slave = actor.TraitOrDefault<RemoteControlSlave>();
                if (slave == null)
                {
                    continue;
                }
                if (slave.HasMaster)
                {
                    continue;
                }

                if (slaves.Where(o => o.ActorID == actor.ActorID).Count() == 0)
                {
                    slave.LinkMaster(this);
                    slave.GrandCondition(info.GrantRemoteControlCondition);
                    slaves.Add(actor);
                }
            }

            for (int i = slaves.Count - 1; i >= 0; i--)
            {
                Actor s = slaves[i];
                if (!s.IsInWorld || s.IsDead)
                {
                    slaves.RemoveAt(i);
                }
            }

            CheckDisableSlaves();
        }

        private bool IsDisableAllSlaves(Actor self, out List<Actor> actors)
        {
            bool isDisableAllSlaves = false;
            World w = self.World;
            actors = w.Actors.Where(o => o.Owner == self.Owner).ToList();
            if (self.IsDead)
            {
                //Is there any other remote master?
                var remoteControlMasters = actors.Where(o => o.TraitsImplementing<RemoteControlMaster>().Count() > 0);
                //Are they the same remote master as me?
                if (remoteControlMasters.Where(
                    o => o.Trait<RemoteControlMaster>().info.GrantRemoteControlCondition
                    == info.GrantRemoteControlCondition).Count() > 0)
                {
                    isDisableAllSlaves = false;
                }
                else
                {
                    isDisableAllSlaves = true;
                }
            }
            else
            {
                isDisableAllSlaves = false;
            }
            return isDisableAllSlaves;
        }

        public void Killed(Actor self, AttackInfo e)
        {
            CheckDisableSlaves();
        }

        public void Selling(Actor self)
        {
        }

        public void Sold(Actor self)
        {
            CheckDisableSlaves();
        }

        private void CheckDisableSlaves()
        {
            List<Actor> remoteMasterActorsInThisMap = new List<Actor>();
            bool ret = IsDisableAllSlaves(self, out remoteMasterActorsInThisMap);
            if (remoteMasterActorsInThisMap == null)
            {
                ret = true;
            }
            if (ret)
            {
                foreach (Actor slave in slaves)
                {
                    RemoteControlSlave s = slave.TraitOrDefault<RemoteControlSlave>();
                    if (s != null)
                    {
                        s.RevokeCondition();
                    }
                }
            }
            else
            {
                Random rand = new Random();
                rand.Next(0, remoteMasterActorsInThisMap.Count);
                foreach (Actor slave in slaves)
                {
                    RemoteControlSlave s = slave.TraitOrDefault<RemoteControlSlave>();
                    if (s != null && !s.HasMaster)
                    {
                        s.LinkMaster(remoteMasterActorsInThisMap[rand.Next()].TraitOrDefault<RemoteControlMaster>());
                    }
                }
            }
        }
    }
}
