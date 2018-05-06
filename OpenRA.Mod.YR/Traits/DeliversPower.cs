using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mod.YR.Traits
{
    [Desc("Deliver power to actors with the `AcceptsDeliveredPower` trait.")]
    class DeliversPowerInfo : ITraitInfo
    {
        [Desc("Identifier checked against AcceptsDeliveredCash.ValidTypes. Only needed if the latter is not empty.")]
        public readonly string Type = null;

        [VoiceReference]
        public readonly string Voice = "Action";

        public object Create(ActorInitializer init)
        {
            return new DeliversPower(this);
        }
    }
    class DeliversPower : IIssueOrder, IResolveOrder, IOrderVoice
    {
        readonly DeliversPowerInfo info;
        public DeliversPower(DeliversPowerInfo info)
        {
            this.info = info;
        }
        public IEnumerable<IOrderTargeter> Orders
        {
            get { yield return new DeliversPowerTargeter(); }
        }

        public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
        {
            if (order.OrderID != "DeliverPower")
                return null;

            if (target.Type == TargetType.FrozenActor)
                return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

            return new Order(order.OrderID, self, queued) ;
        }

        public void ResolveOrder(Actor self, Order order)
        {
            if (order.OrderString != "DeliverPower")
                return;

            var target = self.ResolveFrozenActorOrder(order, Color.Yellow);
            if (target.Type != TargetType.Actor)
                return;

            if (!order.Queued)
                self.CancelActivity();

            self.SetTargetLine(target, Color.Yellow);
            //self.QueueActivity();
        }

        public string VoicePhraseForOrder(Actor self, Order order)
        {
            return info.Voice;
        }
    }

    class DeliversPowerTargeter : UnitOrderTargeter
    {
        public DeliversPowerTargeter() : 
            base("DeliverPower", 5, "enter", false, true)
        {
        }

        public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
        {
            var type = self.Info.TraitInfo<DeliversPowerInfo>().Type;
            var targetInfo = target.Info.TraitInfoOrDefault<AcceptDeliveredPowerInfo>();
            return targetInfo != null
                
                && (targetInfo.ValidTypes.Count == 0
                    || (!string.IsNullOrEmpty(type) && targetInfo.ValidTypes.Contains(type)));
        }

        public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
        {
            var type = self.Info.TraitInfo<DeliversPowerInfo>().Type;
            var targetInfo = target.Info.TraitInfoOrDefault<AcceptDeliveredPowerInfo>();
            return targetInfo != null
                
                && (targetInfo.ValidTypes.Count == 0
                    || (!string.IsNullOrEmpty(type) && targetInfo.ValidTypes.Contains(type)));
        }
    }
}
