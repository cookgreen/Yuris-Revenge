using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.YR.Traits.SupportPowers;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits.Render
{
    public class WithSupportPowerAnimationInfo : ConditionalTraitInfo
    {
        public readonly string OrderName = null;
        public readonly string IdleSequence = null;
        public readonly string ChargedSequence = null;
        public readonly string ActiveSequence = null;
        public readonly string DeactiveSequence = null;
        public readonly string Condition = null;

        [Desc("Which sprite body to play the animation on.")]
        public readonly string Body = "body";

        public override object Create(ActorInitializer init)
        {
            return new WithSupportPowerAnimation(init, this);
        }
    }
    public class WithSupportPowerAnimation : ConditionalTrait<WithSupportPowerAnimationInfo>, INotifySupportPowerCharged, INotifySupportPowerActived
    {
        private WithSupportPowerAnimationInfo info;
        private Actor self;
        private WithSpriteBody wsb;
        private SupportPowerManager supportPowerManager;
        private IEnumerable<SupportPowerInstance> powers;
        private string key;
        private ConditionManager conditionManager;
        private int conditionToken = ConditionManager.InvalidConditionToken;
        public WithSupportPowerAnimation(ActorInitializer init, WithSupportPowerAnimationInfo info) : base(info)
        {
            this.info = info;
            self = init.Self;
            supportPowerManager = self.Owner.PlayerActor.Trait<SupportPowerManager>();
            powers = supportPowerManager.GetPowersForActor(self);
        }

        protected override void Created(Actor self)
        {
            base.Created(self);
            conditionManager = self.Trait<ConditionManager>();
            wsb = self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == info.Body);
        }

        public void Charged(Actor self, string key)
        {
            conditionToken = conditionManager.GrantCondition(self, info.Condition);
            this.key = key;
            if (key == info.OrderName)
            {
                wsb.PlayCustomAnimation(self, info.ChargedSequence, () => 
                {
                    wsb.PlayCustomAnimationRepeating(self, info.ActiveSequence);
                });
            }
        }

        public void Active(Actor self, Order order, SupportPowerManager manager)
        {
            wsb.PlayCustomAnimation(self, info.DeactiveSequence, () =>
            {
                self.World.AddFrameEndTask(w =>
                {
                    conditionToken = conditionManager.RevokeCondition(self, conditionToken);
                });
            });
        }
    }
}
