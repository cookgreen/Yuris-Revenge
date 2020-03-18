using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.YR.Traits.SupportPowers;
using OpenRA.Primitives;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits.Render
{
    public class WithSupportPowerOverlayInfo : ConditionalTraitInfo
    {
        public readonly string OrderName = null;
        public readonly string IdleSequence = null;
        public readonly string ChargedSequence = null;
        public readonly string ActiveSequence = null;
        public readonly string DeactiveSequence = null;
        public readonly string Condition = null;

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Which sprite body to play the animation on.")]
        public readonly string Body = "body";

        public override object Create(ActorInitializer init)
        {
            return new WithSupportPowerOverlay(init.Self, this);
        }
    }
    public class WithSupportPowerOverlay : ConditionalTrait<WithSupportPowerOverlayInfo>, INotifySupportPowerCharged, INotifySupportPowerActived
	{
        private WithSupportPowerOverlayInfo info;
        private Actor self;
        private SupportPowerManager supportPowerManager;
        private IEnumerable<SupportPowerInstance> powers;
        private string key;
        private ConditionManager conditionManager;
        private int conditionToken = ConditionManager.InvalidConditionToken;
		private Animation overlay;
		bool buildComplete;
		bool visible;
		public WithSupportPowerOverlay(Actor self, WithSupportPowerOverlayInfo info) : base(info)
		{
			this.info = info;
			this.self = self;

			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();
			supportPowerManager = self.Owner.PlayerActor.Trait<SupportPowerManager>();
            powers = supportPowerManager.GetPowersForActor(self);

			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>(); // always render instantly for units
			overlay = new Animation(self.World, rs.GetImage(self));

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(WVec.Zero.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => !visible || !buildComplete);

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

        protected override void Created(Actor self)
		{
			conditionManager = self.Trait<ConditionManager>();

			base.Created(self);
        }

        public void Charged(Actor self, string key)
        {
			visible = true;
            this.key = key;
            if (key == info.OrderName)
            {
				overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.ChargedSequence), () =>
				{
					overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.ActiveSequence));
				});
            }
        }

        public void Active(Actor self, Order order, SupportPowerManager manager)
        {
			visible = true;
			overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.DeactiveSequence), () =>
            {
				visible = false;
            });
		}
	}
}
