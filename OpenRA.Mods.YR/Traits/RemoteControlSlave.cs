using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.YR.Activities;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Primitives;

namespace OpenRA.Mods.YR.Traits
{
    public class RemoteControlSlaveInfo : ConditionalTraitInfo
    {
        //Equal to the Remote Master's condition
        public string Condition = null;
        public override object Create(ActorInitializer init)
        {
            return new RemoteControlSlave(init, this);
        }
    }
    public class RemoteControlSlave : ConditionalTrait<RemoteControlSlaveInfo>, INotifyOtherProduction, ITick
    {
        private int remoteConditionToken = ConditionManager.InvalidConditionToken;
        private RemoteControlMaster master;
        private ConditionManager conditionManager;
        private Actor self;
        public bool BuildComplete;
        public bool HasMaster
        {
            get
            {
                return master != null;
            }
        }
        public RemoteControlSlave(ActorInitializer init, RemoteControlSlaveInfo info) : base(info)
        {
            self = init.Self;
            BuildComplete = false;
        }

        protected override void Created(Actor self)
        {
            conditionManager = self.Trait<ConditionManager>();
            remoteConditionToken = conditionManager.GrantCondition(self, Info.Condition);
            base.Created(self);
        }

        public void GrandCondition(string condition)
        {
            if (remoteConditionToken != ConditionManager.InvalidConditionToken)
            {
                return;
            }
            if(BuildComplete)
            {
                remoteConditionToken = conditionManager.GrantCondition(self, condition);
            }
        }

        public void RevokeCondition()
        {
            if (remoteConditionToken == ConditionManager.InvalidConditionToken)
            {
                return;
            }
            if (BuildComplete)
            {
                remoteConditionToken = conditionManager.RevokeCondition(self, remoteConditionToken);
            }
        }

        public void LinkMaster(RemoteControlMaster master)
        {
            this.master = master;
        }

        //void INotifyProduction.UnitProduced(Actor producer, Actor newUnit, CPos exit)
        //{
        //}

        public void Tick(Actor self)
        {
            if (!BuildComplete)
                return;

            var actors = CheckAllMasters();
            if (actors.Count() > 0)
            {
                //We don't need to link master now, because master will find suitable slave
                GrandCondition(Info.Condition);
            }
            else
            {
                RevokeCondition();
            }
        }

        private IEnumerable<Actor> CheckAllMasters()
        {
            List<Actor> foundActors = new List<Actor>();
            var myActors = self.World.Actors.Where(o => o.Owner == self.Owner);
            foreach (var myActor in myActors)
            {
                if (!myActor.IsDead && myActor.IsInWorld &&
                    myActor.TraitsImplementing<RemoteControlMaster>().Count() > 0 &&
                    myActor.TraitOrDefault<RemoteControlMaster>().Info.GrantRemoteControlCondition == Info.Condition)
                {
                    foundActors.Add(myActor);
                }
            }
            return foundActors;
        }

        public void UnitProducedByOther(Actor self, Actor producer, Actor newUnit, string productionType, TypeDictionary init)
        {
            if (self == newUnit)
            {
                //When call this function, the remote control slave hasn't moved to the 
                //outside, it is still inside the production factory
                //so we need to queue a activity to do the addon logic 
                //to check if the production is complete and has moved to the outside of 
                //the production factory
                self.QueueActivity(new RemoteControlSlaveProductionComplete(this));
            }
        }
    }
}
