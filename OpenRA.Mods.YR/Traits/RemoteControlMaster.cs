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
    public class RemoteControlMaster : ConditionalTrait<RemoteControlMasterInfo>, ITick, INotifyOwnerChanged
    {
        private RemoteControlMasterInfo info;
        private int conditionToken;
        private ConditionManager conditionManager;
        private Player newOwner;
        private Player oldOwner;
        public RemoteControlMaster(ActorInitializer init, RemoteControlMasterInfo info) : base(info)
        {
            this.info = info;
        }

        protected override void Created(Actor self)
        {
            conditionManager = self.Trait<ConditionManager>();

            if (conditionToken == ConditionManager.InvalidConditionToken)
            {
                //Grant condition to all actors belong to this faction
                World w = self.World;
                var actors = w.Actors.Where(o => o.Owner == self.Owner);
                foreach (var remoteSlave in actors)
                {
                    conditionToken = conditionManager.GrantCondition(remoteSlave, info.GrantRemoteControlCondition);
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
            List<Actor> actors = new List<Actor>();
            bool ret = IsDisableAllSlaves(self, out actors);
            if (actors == null)
            {
                return;
            }
            if(ret)
            {
                foreach (Actor actor in actors)
                {
                    conditionToken = conditionManager.RevokeCondition(actor, conditionToken);
                }
            }
            else
            {
                foreach (Actor actor in actors)
                {
                    conditionToken = conditionManager.GrantCondition(actor, info.GrantRemoteControlCondition);
                }
            }
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
    }
}
