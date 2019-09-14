using OpenRA.Mods.Common.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
	public class RepairsUnitsConditionalInfo : RepairsUnitsInfo
	{
		[Desc("Grant a condition when repairing")]
		public readonly string RepairingCondition;
		public override object Create(ActorInitializer init)
		{
			return new RepairsUnitsConditional(init, this);
		}
	}

	public class RepairsUnitsConditional : RepairsUnits, INotifyResupply
	{
		private RepairsUnitsConditionalInfo info;
		private int conditionToken = ConditionManager.InvalidConditionToken;
		private ConditionManager conditionManager;
		public RepairsUnitsConditional(ActorInitializer init, RepairsUnitsConditionalInfo info) : base(info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			conditionManager = self.Trait<ConditionManager>();

			base.Created(self);
		}

		public void BeforeResupply(Actor host, Actor target, ResupplyType types)
		{
		}

		public void ResupplyTick(Actor host, Actor target, ResupplyType types)
		{
			if (types.HasFlag(ResupplyType.Repair))
			{
				if (conditionToken == ConditionManager.InvalidConditionToken)
				{
					conditionToken = conditionManager.GrantCondition(host, info.RepairingCondition);
				}
			}
			else if (types.HasFlag(ResupplyType.None))
			{
				if (conditionToken != ConditionManager.InvalidConditionToken)
				{
					conditionToken = conditionManager.RevokeCondition(host, conditionToken);
				}
			}
		}
	}
}
