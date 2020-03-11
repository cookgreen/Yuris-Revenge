using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Widgets.Logic
{
	public class WDTLogic : ChromeLogic
	{

		readonly World world;
		readonly ModData modData;

		Widget panel;

		Sprite[] currentSprites;
		int currentFrame;
		string currentPalette;
		bool isVideoLoaded, isLoadError;

		[ObjectCreator.UseCtor]
		public WDTLogic(Widget widget, Action onExit, ModData modData, World world, Dictionary<string, MiniYaml> logicArgs)
		{
			this.modData = modData;
			this.world = world;

			panel = widget;

			var spriteWidget = panel.GetOrNull<SpriteWidget>("SPRITE");
			if (spriteWidget != null)
			{
				spriteWidget.GetSprite = () => currentSprites != null ? currentSprites[currentFrame] : null;
				currentPalette = spriteWidget.Palette;
				spriteWidget.GetPalette = () => currentPalette;
				spriteWidget.IsVisible = () => !isVideoLoaded && !isLoadError;
			}

			var closeButton = panel.GetOrNull<ButtonWidget>("BACK_BUTTON");
			if (closeButton != null)
				closeButton.OnClick = () =>
				{
					Ui.CloseWindow();
					onExit();
				};
		}
	}
}
