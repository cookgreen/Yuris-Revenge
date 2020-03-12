using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.YR.WDT;
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
		readonly WDTData wdtData;

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
			var wdtDataReader = new WDTDataReader("wdt_data.yaml");
			wdtData = wdtDataReader.Read(modData);

			currentSprites = null;
			currentFrame = 0;
			currentPalette = null;
			isVideoLoaded = false;
			isLoadError = false;

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

			loadDefaultWDTData();
		}

		private void loadDefaultWDTData()
		{
			if (wdtData.Scenarios.Count > 0)
			{
				currentSprites = world.Map.Rules.Sequences.SpriteCache[wdtData.Scenarios[0].BackgroundImage];
				currentFrame = 0;
			}
		}
	}
}
