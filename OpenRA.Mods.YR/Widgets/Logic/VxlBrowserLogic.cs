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

namespace OpenRA.Mods.YR.Widgets.Logic
{
    public class VxlBrowserLogic : ChromeLogic
    {
        readonly string[] allowedExtensions;
        readonly IEnumerable<IReadOnlyPackage> acceptablePackages;

        readonly World world;
        readonly ModData modData;

        Widget panel;

        TextFieldWidget filenameInput;
        ScrollPanelWidget assetList;
        ScrollItemWidget template;

        TextFieldWidget scaleInput;
        TextFieldWidget lightPitchInput;
        TextFieldWidget lightYawInput;
        ColorBlockWidget lightAmbientColorBlock;
        ColorBlockWidget lightDiffuseColorBlock;
        LabelWidget lightAmbientColorValue;
        LabelWidget lightDiffuseColorValue;

        IReadOnlyPackage assetSource = null;

        string currentPalette;
        string currentPlayerPalette = "player";
        string currentNormalsPalette = "normals";
        string currentShadowPalette = "shadow";
        int scale = 12;
        int lightPitch = 142;
        int lightYaw = 682;
        float[] lightAmbientColor = new float[] {0.6f, 0.6f, 0.6f };
        float[] lightDiffuseColor = new float[] { 0.4f, 0.4f, 0.4f };

        string currentFilename;
        IReadOnlyPackage currentPackage;
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

            var sourceDropdown = panel.GetOrNull<DropDownButtonWidget>("SOURCE_SELECTOR");
            if (sourceDropdown != null)
            {
                sourceDropdown.OnMouseDown = _ => ShowSourceDropdown(sourceDropdown);
                sourceDropdown.GetText = () =>
                {
                    var name = assetSource != null ? Platform.UnresolvePath(assetSource.Name) : "All Packages";
                    if (name.Length > 15)
                        name = "..." + name.Substring(name.Length - 15);

                    return name;
                };
            }

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
                lightAmbientColorPreview.Color = HSLColor.FromRGB(
                    Convert.ToInt32(lightAmbientColor[0] * 255),
                    Convert.ToInt32(lightAmbientColor[1] * 255),
                    Convert.ToInt32(lightAmbientColor[2] * 255)
                );

            var lightDiffuseColorPreview = panel.GetOrNull<ColorPreviewManagerWidget>("LIGHT_DIFFUSE_COLOR_MANAGER");
            if (lightDiffuseColorPreview != null)
                lightDiffuseColorPreview.Color = HSLColor.FromRGB(
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
                lightAmbientColorBlock.GetColor = () => System.Drawing.Color.FromArgb(
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
                lightDiffuseColorBlock.GetColor = () => System.Drawing.Color.FromArgb(
                    Convert.ToInt32(lightDiffuseColor[0] * 255),
                    Convert.ToInt32(lightDiffuseColor[1] * 255),
                    Convert.ToInt32(lightDiffuseColor[2] * 255)
                );
            }

            filenameInput = panel.Get<TextFieldWidget>("FILENAME_INPUT");
            filenameInput.OnTextEdited = () => ApplyFilter();
            filenameInput.OnEscKey = filenameInput.YieldKeyboardFocus;

            if (logicArgs.ContainsKey("SupportedFormats"))
                allowedExtensions = FieldLoader.GetValue<string[]>("SupportedFormats", logicArgs["SupportedFormats"].Value);
            else
                allowedExtensions = new string[0];

            acceptablePackages = modData.ModFiles.MountedPackages.Where(p =>
                p.Contents.Any(c => allowedExtensions.Contains(Path.GetExtension(c).ToLowerInvariant())));

            assetList = panel.Get<ScrollPanelWidget>("ASSET_LIST");
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

        Dictionary<string, bool> assetVisByName = new Dictionary<string, bool>();

        bool FilterAsset(string filename)
        {
            var filter = filenameInput.Text;

            if (string.IsNullOrWhiteSpace(filter))
                return true;

            if (filename.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        void ApplyFilter()
        {
            assetVisByName.Clear();
            assetList.Layout.AdjustChildren();
            assetList.ScrollToTop();

            // Select the first visible
            var firstVisible = assetVisByName.FirstOrDefault(kvp => kvp.Value);
            IReadOnlyPackage package;
            string filename;

            if (firstVisible.Key != null && modData.DefaultFileSystem.TryGetPackageContaining(firstVisible.Key, out package, out filename))
                LoadAsset(package, filename);
        }

        void AddAsset(ScrollPanelWidget list, string filepath, IReadOnlyPackage package, ScrollItemWidget template)
        {
            var item = ScrollItemWidget.Setup(template,
                () => currentFilename == filepath && currentPackage == package,
                () => { LoadAsset(package, filepath); });

            item.Get<LabelWidget>("TITLE").GetText = () => filepath;
            item.IsVisible = () =>
            {
                bool visible;
                if (assetVisByName.TryGetValue(filepath, out visible))
                    return visible;

                visible = FilterAsset(filepath);
                assetVisByName.Add(filepath, visible);
                return visible;
            };

            list.AddChild(item);
        }

        bool LoadAsset(IReadOnlyPackage package, string filename)
        {
            if (isVideoLoaded)
            {
                player.Stop();
                player = null;
                isVideoLoaded = false;
            }

            if (string.IsNullOrEmpty(filename))
                return false;

            if (!package.Contains(filename))
                return false;

            isLoadError = false;

            try
            {
                currentPackage = package;
                currentFilename = filename;
                var prefix = "";
                var fs = modData.DefaultFileSystem as OpenRA.FileSystem.FileSystem;

                if (fs != null)
                {
                    prefix = fs.GetPrefix(package);
                    if (prefix != null)
                        prefix += "|";
                }

                VxlReader vxl;
                HvaReader hva;
                string filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
                using (var s = modData.DefaultFileSystem.Open(filenameWithoutExtension + ".vxl"))
                    vxl = new VxlReader(s);
                using (var s = modData.DefaultFileSystem.Open(filenameWithoutExtension + ".hva"))
                    hva = new HvaReader(s, filenameWithoutExtension + ".hva");
                VoxelLoader loader = new VoxelLoader(modData.DefaultFileSystem);
                currentVoxel = new Voxel(loader, vxl, hva);
            }
            catch (Exception ex)
            {
                isLoadError = true;
                Log.AddChannel("vxlbrowser", "vxlbrowser.log");
                Log.Write("vxlbrowser", "Error reading {0}:{3} {1}{3}{2}", filename, ex.Message, ex.StackTrace, Environment.NewLine);

                return false;
            }

            return true;
        }

        bool ShowSourceDropdown(DropDownButtonWidget dropdown)
        {
            Func<IReadOnlyPackage, ScrollItemWidget, ScrollItemWidget> setupItem = (source, itemTemplate) =>
            {
                var item = ScrollItemWidget.Setup(itemTemplate,
                    () => assetSource == source,
                    () => { assetSource = source; PopulateAssetList(); });
                item.Get<LabelWidget>("LABEL").GetText = () => source != null ? Platform.UnresolvePath(source.Name) : "All Packages";
                return item;
            };

            var sources = new[] { (IReadOnlyPackage)null }.Concat(acceptablePackages);
            dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 280, sources, setupItem);
            return true;
        }

        void PopulateAssetList()
        {
            assetList.RemoveChildren();

            var files = new SortedList<string, List<IReadOnlyPackage>>();

            if (assetSource != null)
                foreach (var content in assetSource.Contents)
                    files.Add(content, new List<IReadOnlyPackage> { assetSource });
            else
            {
                foreach (var mountedPackage in modData.ModFiles.MountedPackages)
                {
                    foreach (var content in mountedPackage.Contents)
                    {
                        if (!files.ContainsKey(content))
                            files.Add(content, new List<IReadOnlyPackage> { mountedPackage });
                        else
                            files[content].Add(mountedPackage);
                    }
                }
            }

            foreach (var file in files.OrderBy(s => s.Key))
            {
                if (!allowedExtensions.Any(ext => file.Key.EndsWith(ext, true, CultureInfo.InvariantCulture)))
                    continue;

                foreach (var package in file.Value)
                    AddAsset(assetList, file.Key, package, template);
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
                System.Drawing.Color c = preview.Color.RGB;
                lightAmbientColor[0] = float.Parse((Convert.ToSingle(c.R) / 255).ToString("0.0"));
                lightAmbientColor[1] = float.Parse((Convert.ToSingle(c.G) / 255).ToString("0.0"));
                lightAmbientColor[2] = float.Parse((Convert.ToSingle(c.B) / 255).ToString("0.0"));
                lightAmbientColorBlock.GetColor = () => c;
                lightAmbientColorValue.GetText = () => string.Format("{0}, {1}, {2}", lightAmbientColor[0].ToString(), lightAmbientColor[1].ToString(), lightAmbientColor[2].ToString());
            };

            color.RemovePanel();

            Action<HSLColor> onChange = c => preview.Color = c;
            
            var colorChooser = Game.LoadWidget(world, "COLOR_CHOOSER", null, new WidgetArgs()
            {
                { "onChange", onChange },
                { "initialColor", HSLColor.FromRGB(
                Convert.ToInt32(lightAmbientColor[0] * 255),
                Convert.ToInt32(lightAmbientColor[1] * 255),
                Convert.ToInt32(lightAmbientColor[2] * 255)
                )}
            });

            color.AttachPanel(colorChooser, onExit);
        }

        void ShowLightDiffuseColorDropDown(DropDownButtonWidget color, ColorPreviewManagerWidget preview, World world)
        {
            Action onExit = () =>
            {
                System.Drawing.Color c = preview.Color.RGB;
                lightDiffuseColor[0] = float.Parse((Convert.ToSingle(c.R) / 255).ToString("0.0"));
                lightDiffuseColor[1] = float.Parse((Convert.ToSingle(c.G) / 255).ToString("0.0"));
                lightDiffuseColor[2] = float.Parse((Convert.ToSingle(c.B) / 255).ToString("0.0"));
                lightDiffuseColorBlock.GetColor = () => c;
                lightDiffuseColorValue.GetText = () => string.Format("{0}, {1}, {2}", lightDiffuseColor[0].ToString(), lightDiffuseColor[1].ToString(), lightDiffuseColor[2].ToString());
            };

            color.RemovePanel();

            Action<HSLColor> onChange = c => preview.Color = c;

            var colorChooser = Game.LoadWidget(world, "COLOR_CHOOSER", null, new WidgetArgs()
            {
                { "onChange", onChange },
                { "initialColor", HSLColor.FromRGB(
                Convert.ToInt32(lightDiffuseColor[0] * 255),
                Convert.ToInt32(lightDiffuseColor[1] * 255),
                Convert.ToInt32(lightDiffuseColor[2] * 255)
                ) }
            });

            color.AttachPanel(colorChooser, onExit);
        }
    }
}
