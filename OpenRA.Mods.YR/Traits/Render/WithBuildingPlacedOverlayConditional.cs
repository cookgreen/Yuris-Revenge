using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR
{
    public class WithBuildingPlacedOverlayConditionalInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
    {
        [Desc("Sequence name to use")]
        [SequenceReference]
        public readonly string Sequence = "crane-overlay";

        [Desc("Position relative to body")]
        public readonly WVec Offset = WVec.Zero;

        [Desc("Custom palette name")]
        [PaletteReference("IsPlayerPalette")]
        public readonly string Palette = null;

        [Desc("Custom palette is a player palette BaseName")]
        public readonly bool IsPlayerPalette = false;

        [GrantedConditionReference]
        [Desc("Condition to grant when place a building")]
        public readonly string PlacingCondition;

        public override object Create(ActorInitializer init)
        {
            return new WithBuildingPlacedOverlayConditional(init.Self, this);
        }
    }
    public class WithBuildingPlacedOverlayConditional : ConditionalTrait<WithBuildingPlacedOverlayConditionalInfo>, INotifyBuildComplete, INotifySold, INotifyDamageStateChanged, INotifyBuildingPlaced, INotifyTransform
    {
        readonly Animation overlay;
        bool buildComplete;
        bool visible;
        ConditionManager conditionManager;
        int wbpoToken = ConditionManager.InvalidConditionToken;

        public WithBuildingPlacedOverlayConditional(Actor self, WithBuildingPlacedOverlayConditionalInfo info) : base(info)
        {
            var rs = self.Trait<RenderSprites>();
            var body = self.Trait<BodyOrientation>();

            buildComplete = !self.Info.HasTraitInfo<BuildingInfo>(); // always render instantly for units

            overlay = new Animation(self.World, rs.GetImage(self));

            var anim = new AnimationWithOffset(overlay,
                () => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
                () => !visible || !buildComplete);

            overlay.PlayThen(info.Sequence, () => visible = false);
            rs.Add(anim, info.Palette, info.IsPlayerPalette);
        }

        void INotifyBuildComplete.BuildingComplete(Actor self)
        {
            buildComplete = true;
            visible = false;
        }

        protected override void Created(Actor self)
        {
            conditionManager = self.Trait<ConditionManager>();

            base.Created(self);
        }

        void INotifySold.Sold(Actor self) { }
        void INotifySold.Selling(Actor self)
        {
            buildComplete = false;
        }

        void INotifyTransform.BeforeTransform(Actor self)
        {
            buildComplete = false;
        }

        void INotifyTransform.OnTransform(Actor self) { }
        void INotifyTransform.AfterTransform(Actor self) { }

        void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
        {
            overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
        }

        void INotifyBuildingPlaced.BuildingPlaced(Actor self)
        {
            visible = true;

            if (wbpoToken == ConditionManager.InvalidConditionToken)
                wbpoToken = conditionManager.GrantCondition(self, Info.PlacingCondition);

            overlay.PlayThen(overlay.CurrentSequence.Name, () => 
            {
                visible = false;

                if (wbpoToken != ConditionManager.InvalidConditionToken)
                    wbpoToken = conditionManager.RevokeCondition(self, wbpoToken);
            });
        }
    }
}
