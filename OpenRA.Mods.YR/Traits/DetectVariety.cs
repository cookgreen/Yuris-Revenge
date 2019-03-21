using OpenRA.Mods.Common.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Traits
{
    
    public class DetectVarietyInfo : ConditionalTraitInfo
    {
        [Desc("Specific variety classifications I can reveal.")]
        public readonly HashSet<string> CloakTypes = new HashSet<string> { "Cloak" };

        public readonly WDist Range = WDist.FromCells(5);

        public override object Create(ActorInitializer init) { return new DetectVariety(this); }
    }

    public class DetectVariety : ConditionalTrait<DetectVarietyInfo>
    {
        public DetectVariety(DetectVarietyInfo info) : base(info) { }
    }
}
