#region Copyright & License Information
/*
 * Written by Cook Green of YR Mod
 * Follows GPLv3 License as the OpenRA engine:
 * 
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Mods.Cnc.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = OpenRA.Primitives.Color;

namespace OpenRA.Mods.YR.Widgets.Logic
{
    public class VxlBrowserLogic : ChromeLogic
    {
        readonly string[] allowedExtensions;
        readonly IEnumerable<IReadOnlyPackage> acceptablePackages;

        readonly World world;
        readonly ModData modData;

        Widget panel;

        TextFieldWidget unitnameInput;
        ScrollPanelWidget unitList;
        ScrollItemWidget template;

        TextFieldWidget scaleInput;
        TextFieldWidget lightPitchInput;
        TextFieldWidget lightYawInput;
        ColorBlockWidget lightAmbientColorBlock;
        ColorBlockWidget lightDiffuseColorBlock;
        LabelWidget lightAmbientColorValue;
        LabelWidget lightDiffuseColorValue;

        string currentPalette;
        string currentPlayerPalette = "player";
        string currentNormalsPalette = "normals";
        string currentShadowPalette = "shadow";
        int scale = 12;
        int lightPitch = 142;
        int lightYaw = 682;
        float[] lightAmbientColor = new float[] {0.6f, 0.6f, 0.6f };
        float[] lightDiffuseColor = new float[] { 0.4f, 0.4f, 0.4f };

        string currentUnitname;
        Voxel currentVoxel;
        VqaPlayerWidget player = null;
        bool isVideoLoaded = false;
        bool isLoadError = false;

        [ObjectCreator.UseCtor]
        public VxlBrowserLogic(Widget widget, Action onExit, ModData modData, World world, Dictionary<string, MiniYaml> logicArgs)
        {
            this.world = world;
            this.modData = modData;
            panel = widget;

            var voxelWidget = panel.GetOrNull<VoxelWidget>("VOXEL");
            if (voxelWidget != null)
            {
                voxelWidget.GetVoxel = () => currentVoxel != null ? currentVoxel : null;
                currentPalette = voxelWidget.Palette;
                voxelWidget.GetPalette = () => currentPalette;
                voxelWidget.GetPlayerPalette = () => currentPlayerPalette;
                voxelWidget.GetNormalsPalette = () => currentNormalsPalette;
                voxelWidget.GetShadowPalette = () => currentShadowPalette;
                voxelWidget.GetLightAmbientColor = () => lightAmbientColor;
                voxelWidget.GetLightDiffuseColor = () => lightDiffuseColor;
                voxelWidget.GetLightPitch = () => lightPitch;
                voxelWidget.GetLightYaw = () => lightYaw;
                voxelWidget.IsVisible = () => !isVideoLoaded && !isLoadError;
            }

            var playerWidget = panel.GetOrNull<VqaPlayerWidget>("PLAYER");
            if (playerWidget != null)
                playerWidget.IsVisible = () => isVideoLoaded && !isLoadError;

            var paletteDropDown = panel.GetOrNull<DropDownButtonWidget>("PALETTE_SELECTOR");
            if (paletteDropDown != null)
            {
                paletteDropDown.OnMouseDown = _ => ShowPaletteDropdown(paletteDropDown, world);
                paletteDropDown.GetText = () => currentPalette;
            }

            var lightAmbientColorPreview = panel.GetOrNull<ColorPreviewManagerWidget>("LIGHT_AMBIENT_COLOR_MANAGER");
            if (lightAmbientColorPreview != null)
                lightAmbientColorPreview.Color = Color.FromArgb(
                    Convert.ToInt32(lightAmbientColor[0] * 255),
                    Convert.ToInt32(lightAmbientColor[1] * 255),
                    Convert.ToInt32(lightAmbientColor[2] * 255)
                );

            var lightDiffuseColorPreview = panel.GetOrNull<ColorPreviewManagerWidget>("LIGHT_DIFFUSE_COLOR_MANAGER");
            if (lightDiffuseColorPreview != null)
                lightDiffuseColorPreview.Color = Color.FromArgb(
                    Convert.ToInt32(lightDiffuseColor[0] * 255),
                    Convert.ToInt32(lightDiffuseColor[1] * 255),
                    Convert.ToInt32(lightDiffuseColor[2] * 255)
                );

            var playerPaletteDropDown = panel.GetOrNull<DropDownButtonWidget>("PLAYER_PALETTE_SELECTOR");
            if (playerPaletteDropDown != null)
            {
                playerPaletteDropDown.OnMouseDown = _ => ShowPlayerPaletteDropdown(playerPaletteDropDown, world);
                playerPaletteDropDown.GetText = () => currentPlayerPalette;
            }

            var normalsPlaletteDropDown = panel.GetOrNull<DropDownButtonWidget>("NORMALS_PALETTE_SELECTOR");
            if (normalsPlaletteDropDown != null)
            {
                normalsPlaletteDropDown.OnMouseDown = _ => ShowNormalsPaletteDropdown(normalsPlaletteDropDown, world);
                normalsPlaletteDropDown.GetText = () => currentNormalsPalette;
            }

            var shadowPlaletteDropDown = panel.GetOrNull<DropDownButtonWidget>("SHADOW_PALETTE_SELECTOR");
            if (shadowPlaletteDropDown != null)
            {
                shadowPlaletteDropDown.OnMouseDown = _ => ShowShadowPaletteDropdown(normalsPlaletteDropDown, world);
                shadowPlaletteDropDown.GetText = () => currentShadowPalette;
            }

            scaleInput = panel.GetOrNull<TextFieldWidget>("SCALE_TEXT");
            scaleInput.OnTextEdited = () => OnScaleEdit();
            scaleInput.OnEscKey = scaleInput.YieldKeyboardFocus;

            lightPitchInput = panel.GetOrNull<TextFieldWidget>("LIGHTPITCH_TEXT");
            lightPitchInput.OnTextEdited = () => OnLightPitchEdit();
            lightPitchInput.OnEscKey = lightPitchInput.YieldKeyboardFocus;

            lightYawInput = panel.GetOrNull<TextFieldWidget>("LIGHTYAW_TEXT");
            lightYawInput.OnTextEdited = () => OnLightYawEdit();
            lightYawInput.OnEscKey = lightYawInput.YieldKeyboardFocus;


            var lightAmbientColorDropDown = panel.GetOrNull<DropDownButtonWidget>("LIGHT_AMBIENT_COLOR");
            if (lightAmbientColorDropDown != null)
            {
                lightAmbientColorDropDown.OnMouseDown = _ => ShowLightAmbientColorDropDown(lightAmbientColorDropDown, lightAmbientColorPreview, world);
                lightAmbientColorBlock = panel.Get<ColorBlockWidget>("AMBIENT_COLORBLOCK");
                lightAmbientColorBlock.GetColor = () => OpenRA.Primitives.Color.FromArgb(
                    Convert.ToInt32(lightAmbientColor[0] * 255),
                    Convert.ToInt32(lightAmbientColor[1] * 255),
                    Convert.ToInt32(lightAmbientColor[2] * 255)
                );
            }

            lightAmbientColorValue = panel.GetOrNull<LabelWidget>("LIGHTAMBIENTCOLOR_VALUE");
            lightDiffuseColorValue = panel.GetOrNull<LabelWidget>("LIGHTDIFFUSECOLOR_VALUE");

            var lightDiffuseColorDropDown = panel.GetOrNull<DropDownButtonWidget>("LIGHT_DIFFUSE_COLOR");
            if (lightDiffuseColorDropDown != null)
            {
                lightDiffuseColorDropDown.OnMouseDown = _ => ShowLightDiffuseColorDropDown(lightDiffuseColorDropDown, lightDiffuseColorPreview, world);
                lightDiffuseColorBlock = panel.Get<ColorBlockWidget>("DIFFUSE_COLORBLOCK");
                lightDiffuseColorBlock.GetColor = () => Color.FromArgb(
                    Convert.ToInt32(lightDiffuseColor[0] * 255),
                    Convert.ToInt32(lightDiffuseColor[1] * 255),
                    Convert.ToInt32(lightDiffuseColor[2] * 255)
                );
            }

            unitnameInput = panel.Get<TextFieldWidget>("FILENAME_INPUT");
            unitnameInput.OnTextEdited = () => ApplyFilter();
            unitnameInput.OnEscKey = unitnameInput.YieldKeyboardFocus;

            if (logicArgs.ContainsKey("SupportedFormats"))
                allowedExtensions = FieldLoader.GetValue<string[]>("SupportedFormats", logicArgs["SupportedFormats"].Value);
            else
                allowedExtensions = new string[0];

            acceptablePackages = modData.ModFiles.MountedPackages.Where(p =>
                p.Contents.Any(c => allowedExtensions.Contains(Path.GetExtension(c).ToLowerInvariant())));

            unitList = panel.Get<ScrollPanelWidget>("ASSET_LIST");
            template = panel.Get<ScrollItemWidget>("ASSET_TEMPLATE");
            PopulateAssetList();

            var closeButton = panel.GetOrNull<ButtonWidget>("CLOSE_BUTTON");
            if (closeButton != null)
                closeButton.OnClick = () =>
                {
                    if (isVideoLoaded)
                        player.Stop();
                    Ui.CloseWindow();
                    onExit();
                };
        }

        private void OnScaleEdit()
        {
            string strScale = scaleInput.Text;
            int.TryParse(strScale, out scale);
        }

        private void OnLightYawEdit()
        {
            string strLightYam = lightYawInput.Text;
            int.TryParse(strLightYam, out lightYaw);
        }

        private void OnLightPitchEdit()
        {
            string strLightPitch = lightPitchInput.Text;
            int.TryParse(strLightPitch, out lightPitch);
        }

        Dictionary<string, bool> unitVisByName = new Dictionary<string, bool>();

        bool FilterAsset(string filename)
        {
            var filter = unitnameInput.Text;

            if (string.IsNullOrWhiteSpace(filter))
                return true;

            if (filename.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        void ApplyFilter()
        {
            unitVisByName.Clear();
            unitList.Layout.AdjustChildren();
            unitList.ScrollToTop();

            // Select the first visible
            var firstVisible = unitVisByName.FirstOrDefault(kvp => kvp.Value);
            IReadOnlyPackage package;
            string unitname;

            if (firstVisible.Key != null && modData.DefaultFileSystem.TryGetPackageContaining(firstVisible.Key, out package, out unitname))
                LoadUnit(unitname);
        }

        void AddUnit(ScrollPanelWidget list, string unitname, ScrollItemWidget template)
        {
            var item = ScrollItemWidget.Setup(template,
                () => currentUnitname == unitname,
                () => { LoadUnit(unitname); });

            item.Get<LabelWidget>("TITLE").GetText = () => unitname;
            item.IsVisible = () =>
            {
                bool visible;
                if (unitVisByName.TryGetValue(unitname, out visible))
                    return visible;

                visible = FilterAsset(unitname);
                unitVisByName.Add(unitname, visible);
                return visible;
            };

            list.AddChild(item);
        }

        bool LoadUnit(string unitname)
        {
            if (isVideoLoaded)
            {
                player.Stop();
                player = null;
                isVideoLoaded = false;
            }

            if (string.IsNullOrEmpty(unitname))
                return false;

            isLoadError = false;

            try
            {
                currentUnitname = unitname;

                currentVoxel = (Voxel)world.ModelCache.GetModelSequence(currentUnitname, "idle");
            }
            catch (Exception ex)
            {
                isLoadError = true;
                Log.AddChannel("vxlbrowser", "vxlbrowser.log");
                Log.Write("vxlbrowser", "Error reading {0}:{3} {1}{3}{2}", unitname, ex.Message, ex.StackTrace, Environment.NewLine);

                return false;
            }

            return true;
        }

        void PopulateAssetList()
        {
            unitList.RemoveChildren();

            var units = new SortedList<string, string>();

            var modelSequences = world.Map.Rules.ModelSequences;
            foreach (var modelSequence in modelSequences)
            {
                units.Add(modelSequence.Key, modelSequence.Key);
            }

            foreach (var unit in units.OrderBy(s => s.Key))
            {
                AddUnit(unitList, unit.Key, template);
            }
        }

        bool ShowPaletteDropdown(DropDownButtonWidget dropdown, World world)
        {
            Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (name, itemTemplate) =>
            {
                var item = ScrollItemWidget.Setup(itemTemplate,
                    () => currentPalette == name,
                    () => currentPalette = name);
                item.Get<LabelWidget>("LABEL").GetText = () => name;

                return item;
            };

            var palettes = world.WorldActor.TraitsImplementing<IProvidesAssetBrowserPalettes>()
                .SelectMany(p => p.PaletteNames);
            dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 280, palettes, setupItem);
            return true;
        }

        bool ShowPlayerPaletteDropdown(DropDownButtonWidget dropdown, World world)
        {
            Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (name, itemTemplate) =>
            {
                var item = ScrollItemWidget.Setup(itemTemplate,
                    () => currentPlayerPalette == name,
                    () => currentPlayerPalette = name);
                item.Get<LabelWidget>("LABEL").GetText = () => name;

                return item;
            };

            var palettes = world.WorldActor.TraitsImplementing<IProvidesAssetBrowserPalettes>()
                .SelectMany(p => p.PaletteNames);
            dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 280, palettes, setupItem);
            return true;
        }

        bool ShowNormalsPaletteDropdown(DropDownButtonWidget dropdown, World world)
        {
            Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (name, itemTemplate) =>
            {
                var item = ScrollItemWidget.Setup(itemTemplate,
                    () => currentNormalsPalette == name,
                    () => currentNormalsPalette = name);
                item.Get<LabelWidget>("LABEL").GetText = () => name;

                return item;
            };

            var palettes = world.WorldActor.TraitsImplementing<IProvidesAssetBrowserPalettes>()
                .SelectMany(p => p.PaletteNames);
            dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 280, palettes, setupItem);
            return true;
        }

        bool ShowShadowPaletteDropdown(DropDownButtonWidget dropdown, World world)
        {
            Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (name, itemTemplate) =>
            {
                var item = ScrollItemWidget.Setup(itemTemplate,
                    () => currentShadowPalette == name,
                    () => currentShadowPalette = name);
                item.Get<LabelWidget>("LABEL").GetText = () => name;

                return item;
            };

            var palettes = world.WorldActor.TraitsImplementing<IProvidesAssetBrowserPalettes>()
                .SelectMany(p => p.PaletteNames);
            dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 280, palettes, setupItem);
            return true;
        }

        void ShowLightAmbientColorDropDown(DropDownButtonWidget color, ColorPreviewManagerWidget preview, World world)
        {
            Action onExit = () =>
            {
                Color c = preview.Color;
                lightAmbientColor[0] = float.Parse((Convert.ToSingle(c.R) / 255).ToString("0.0"));
                lightAmbientColor[1] = float.Parse((Convert.ToSingle(c.G) / 255).ToString("0.0"));
                lightAmbientColor[2] = float.Parse((Convert.ToSingle(c.B) / 255).ToString("0.0"));
                lightAmbientColorBlock.GetColor = () => c;
                lightAmbientColorValue.GetText = () => string.Format("{0}, {1}, {2}", lightAmbientColor[0].ToString(), lightAmbientColor[1].ToString(), lightAmbientColor[2].ToString());
            };

            color.RemovePanel();

            Action<Color> onChange = c => preview.Color = c;
            
            var colorChooser = Game.LoadWidget(world, "COLOR_CHOOSER", null, new WidgetArgs()
            {
                { "onChange", onChange },
                { "initialColor", Color.FromArgb(
                Convert.ToInt32(lightAmbientColor[0] * 255),
                Convert.ToInt32(lightAmbientColor[1] * 255),
                Convert.ToInt32(lightAmbientColor[2] * 255)
                )},
                { "initialFaction", null }
            });

            color.AttachPanel(colorChooser, onExit);
        }

        void ShowLightDiffuseColorDropDown(DropDownButtonWidget color, ColorPreviewManagerWidget preview, World world)
        {
            Action onExit = () =>
            {
                Color c = preview.Color;
                lightDiffuseColor[0] = float.Parse((Convert.ToSingle(c.R) / 255).ToString("0.0"));
                lightDiffuseColor[1] = float.Parse((Convert.ToSingle(c.G) / 255).ToString("0.0"));
                lightDiffuseColor[2] = float.Parse((Convert.ToSingle(c.B) / 255).ToString("0.0"));
                lightDiffuseColorBlock.GetColor = () => c;
                lightDiffuseColorValue.GetText = () => string.Format("{0}, {1}, {2}", lightDiffuseColor[0].ToString(), lightDiffuseColor[1].ToString(), lightDiffuseColor[2].ToString());
            };

            color.RemovePanel();

            Action<Color> onChange = c => preview.Color = c;

            var colorChooser = Game.LoadWidget(world, "COLOR_CHOOSER", null, new WidgetArgs()
            {
                { "onChange", onChange },
                { "initialColor", Color.FromArgb(
                Convert.ToInt32(lightDiffuseColor[0] * 255),
                Convert.ToInt32(lightDiffuseColor[1] * 255),
                Convert.ToInt32(lightDiffuseColor[2] * 255)
                ) },
                { "initialFaction", null }
            });

            color.AttachPanel(colorChooser, onExit);
        }
    }
}
