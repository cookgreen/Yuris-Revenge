#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.YR.Widgets.Logic
{
    public enum SoundPlayType
    {
        Voice,
        Notification_Speech,
        Notification_Sounds,
    }
	public class SoundPlayerLogic : ChromeLogic
	{
		readonly ScrollPanelWidget soundList;
		readonly ScrollItemWidget itemTemplate;

        int soundIndex;
		readonly MusicPlaylist soundPlaylist;
		KeyValuePair<string, string[]> currentSound;
        KeyValuePair<string, SoundPlayType> currentSoundEntry;
        bool currentSoundAvaliable;
        Dictionary<string, SoundPlayType> sounds;
        Dictionary<string, string[]> voiceSounds;
        Dictionary<string, string[]> notificationSounds;
        World world;

        [ObjectCreator.UseCtor]
		public SoundPlayerLogic(Widget widget, ModData modData, World world, Action onExit)
		{
            this.world = world;

			var panel = widget;

            var notifications = world.Map.Rules.Notifications;
            var voices = world.Map.Rules.Voices;
            sounds = new Dictionary<string, SoundPlayType>();
            voiceSounds = new Dictionary<string, string[]>();
            notificationSounds = new Dictionary<string, string[]>();
            foreach (var notification in notifications)
            {
                foreach (var n in notification.Value.Notifications)
                {
                    if (notification.Key == "speech")
                    {
                        foreach (var prefix in notification.Value.Prefixes)
                        {
                            string key = n.Key + " - " + prefix.Value[0];
                            if (!sounds.ContainsKey(key))
                            {
                                sounds.Add(key, SoundPlayType.Notification_Speech);
                            }
                            if (!notificationSounds.ContainsKey(key))
                            {
                                string[] newValues = new string[n.Value.Length];
                                for (int i = 0; i < newValues.Length; i++)
                                {
                                    newValues[i] = prefix.Key;
                                }
                                notificationSounds.Add(key, newValues);
                            }
                        }
                    }
                    //else if(notification.Key=="sounds")
                    //{
                    //    sounds.Add(n.Key, SoundPlayType.Notification_Sounds);
                    //    notificationSounds.Add(n.Key, n.Value);
                    //}
                }
            }
            //foreach (var voice in voices)
            //{
            //    foreach (var v in voice.Value.Voices)
            //    {
            //        sounds.Add(string.Format("{0} - {1}", voice.Key, v.Key), SoundPlayType.Voice);
            //        voiceSounds.Add(string.Format("{0} - {1}", voice.Key, v.Key), v.Value);
            //    }
            //}
            soundIndex = 0;

            soundList = panel.Get<ScrollPanelWidget>("SOUND_LIST");
			itemTemplate = soundList.Get<ScrollItemWidget>("SOUND_TEMPLATE");
			soundPlaylist = world.WorldActor.Trait<MusicPlaylist>();
            
			BuildSoundTable();

			Func<bool> noMusic = () => !soundPlaylist.IsMusicAvailable || soundPlaylist.CurrentSongIsBackground;

			if (soundPlaylist.IsMusicAvailable)
			{
				panel.Get<LabelWidget>("MUTE_LABEL").GetText = () =>
				{
					if (Game.Settings.Sound.Mute)
						return "Audio has been muted in settings.";

					return "";
				};
			}

			var playButton = panel.Get<ButtonWidget>("BUTTON_PLAY");
			playButton.OnClick = Play;
            playButton.IsDisabled = () => sounds.Count == 0;
			playButton.IsVisible = () => !Game.Sound.MusicPlaying;

			var pauseButton = panel.Get<ButtonWidget>("BUTTON_PAUSE");
			pauseButton.OnClick = Game.Sound.PauseMusic;
			pauseButton.IsDisabled = () => sounds.Count == 0;
            pauseButton.IsVisible = () => Game.Sound.MusicPlaying;

			var stopButton = panel.Get<ButtonWidget>("BUTTON_STOP");
            stopButton.IsDisabled = () => sounds.Count == 0;
            stopButton.OnClick = () => { soundPlaylist.Stop(); };

            var nextButton = panel.Get<ButtonWidget>("BUTTON_NEXT");
            nextButton.IsDisabled = () => sounds.Count == 0;
            nextButton.OnClick = () => {
                KeyValuePair<string, string[]> sound;
                if (currentSoundAvaliable = GetSound(soundIndex++, out sound))
                {
                    currentSound = sound;
                    Play();
                }
            };

            var prevButton = panel.Get<ButtonWidget>("BUTTON_PREV");
			prevButton.OnClick = () => {
                KeyValuePair<string, string[]> sound;
                if (currentSoundAvaliable = GetSound(soundIndex--, out sound))
                {
                    currentSound = sound;
                    Play();
                }
            };
			prevButton.IsDisabled = noMusic;

			var shuffleCheckbox = panel.Get<CheckboxWidget>("SHUFFLE");
			shuffleCheckbox.IsChecked = () => Game.Settings.Sound.Shuffle;
			shuffleCheckbox.OnClick = () => Game.Settings.Sound.Shuffle ^= true;
			shuffleCheckbox.IsDisabled = () => soundPlaylist.CurrentSongIsBackground;

			var repeatCheckbox = panel.Get<CheckboxWidget>("REPEAT");
			repeatCheckbox.IsChecked = () => Game.Settings.Sound.Repeat;
			repeatCheckbox.OnClick = () => Game.Settings.Sound.Repeat ^= true;
			repeatCheckbox.IsDisabled = () => soundPlaylist.CurrentSongIsBackground;

			panel.Get<LabelWidget>("TIME_LABEL").GetText = () =>
			{
				if (!currentSoundAvaliable)
					return "";

                return string.Empty;
				//var seek = Game.Sound.MusicSeekPosition;
				//var minutes = (int)seek / 60;
				//var seconds = (int)seek % 60;
				//var totalMinutes = currentSound.Length / 60;
				//var totalSeconds = currentSound.Length % 60;
                //
				//return "{0:D2}:{1:D2} / {2:D2}:{3:D2}".F(minutes, seconds, totalMinutes, totalSeconds);
			};

			var musicTitle = panel.GetOrNull<LabelWidget>("TITLE_LABEL");
			if (musicTitle != null)
				musicTitle.GetText = () => currentSoundAvaliable ? currentSound.Key : "No sound playing";

			var musicSlider = panel.Get<SliderWidget>("MUSIC_SLIDER");
			musicSlider.OnChange += x => Game.Sound.MusicVolume = x;
			musicSlider.Value = Game.Sound.MusicVolume;

			var soundWatcher = widget.GetOrNull<LogicTickerWidget>("SOUND_WATCHER");
			if (soundWatcher != null)
			{
				soundWatcher.OnTick = () =>
				{
					//if (currentSoundAvaliable)
					//	currentSound = new KeyValuePair<string, SoundInfo>();
                    //
					//if (Game.Sound.CurrentMusic == null || currentSound == Game.Sound. || soundPlaylist.CurrentSongIsBackground)
					//	return;
                    //
					//currentSound = Game.Sound.CurrentMusic;
				};
			}

			var backButton = panel.GetOrNull<ButtonWidget>("BACK_BUTTON");
			if (backButton != null)
				backButton.OnClick = () => { Game.Settings.Save(); Ui.CloseWindow(); onExit(); };
		}

        private bool GetSound(int soundIndex, out KeyValuePair<string, string[]> sound)
        {
            int index = 0;
            sound = new KeyValuePair<string, string[]>();
            foreach (var s in sounds)
            {
                if(index==soundIndex)
                {
                    sound = new KeyValuePair<string, string[]>(s.Key, s.Value == SoundPlayType.Voice ? voiceSounds[s.Key] : notificationSounds[s.Key]);
                    currentSoundEntry = s;
                    return true;
                }
                index++;
            }
            return false;
        }

        public void BuildSoundTable()
		{
            currentSoundAvaliable = GetSound(soundIndex, out currentSound);

            soundList.RemoveChildren();
			foreach (var sound in sounds)
			{
				var item = ScrollItemWidget.Setup(sound.Key, itemTemplate, () => currentSound.Key == sound.Key, 
                    () => {
                        currentSound = new KeyValuePair<string, string[]>(sound.Key, sound.Value == SoundPlayType.Voice ? voiceSounds[sound.Key] : notificationSounds[sound.Key]); ;
                        Play();
                    }, 
                    () => { });
				var label = item.Get<LabelWithTooltipWidget>("TITLE");
				WidgetUtils.TruncateLabelToTooltip(label, sound.Key);

				//item.Get<LabelWidget>("LENGTH").GetText = () => SongLengthLabel(sound);
				soundList.AddChild(item);
			}

			if (currentSoundAvaliable)
				soundList.ScrollToItem(currentSound.Key);
		}

		void Play()
		{
            if (!currentSoundAvaliable)
                return;

			soundList.ScrollToItem(currentSound.Key);

            if (currentSoundEntry.Value == SoundPlayType.Voice)
            {
                Game.Sound.Play(SoundType.UI, currentSound.Value, world);
            }
            else
            {
                if (currentSoundEntry.Value == SoundPlayType.Notification_Speech)
                {
                    if (currentSound.Value.Length > 0)
                    {
                        Game.Sound.PlayNotification(world.Map.Rules, null, "Speech",
                           currentSound.Key.Split('-')[0].Trim(), currentSound.Value[0]);
                    }
                }
                else if (currentSoundEntry.Value == SoundPlayType.Notification_Sounds)
                {
                    if (currentSound.Value.Length > 0)
                    {
                        Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds",
                        currentSound.Key, null);
                    }
                }
            }

		}

		static string SongLengthLabel(MusicInfo song)
		{
			return "{0:D1}:{1:D2}".F(song.Length / 60, song.Length % 60);
		}
	}
}
