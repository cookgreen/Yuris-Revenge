#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.AS.Traits;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.AS.Activities
{
	public class ChronoResourceTeleport : Activity
	{
		readonly CPos destination;
		readonly ChronoResourceDeliveryInfo info;
		readonly CPos harvestedField;

		public ChronoResourceTeleport(CPos destination, ChronoResourceDeliveryInfo info, CPos harvestedField)
		{
			this.destination = destination;
			this.info = info;
			this.harvestedField = harvestedField;
		}

		public override Activity Tick(Actor self)
		{
			var image = info.Image ?? self.Info.Name;

			var sourcepos = self.CenterPosition;

			if (info.WarpInSequence != null)
				self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(sourcepos, w, image, info.WarpInSequence, info.Palette)));

			if (info.WarpInSound != null && (info.AudibleThroughFog || !self.World.FogObscures(sourcepos)))
				Game.Sound.Play(SoundType.World, info.WarpInSound, self.CenterPosition, info.SoundVolume);

			if (info.ExposeInfectors)
				foreach (var i in self.TraitsImplementing<IRemoveInfector>())
					i.RemoveInfector(self, false);

			self.Trait<IPositionable>().SetPosition(self, destination);
			self.Generation++;

			var destinationpos = self.CenterPosition;

			if (info.WarpOutSequence != null)
				self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(destinationpos, w, image, info.WarpOutSequence, info.Palette)));

			if (info.WarpOutSound != null && (info.AudibleThroughFog || !self.World.FogObscures(sourcepos)))
				Game.Sound.Play(SoundType.World, info.WarpOutSound, self.CenterPosition, info.SoundVolume);

			self.QueueActivity(new FindAndDeliverResources(self, harvestedField));

			return NextActivity;
		}
	}
}
