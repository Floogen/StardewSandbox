using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimelessPocketWatch.Framework.Utilities
{
    public class SoundHelper
    {
        private static float _cachedMusicVolume;
        private static float _cachedSoundVolume;
        private static float _cachedAmbientVolumeLevel;
        private static float _cachedFootstepVolumeLevel;

        public static void SetAndCacheSoundVolumes(float musicVolume, float soundVolume, float ambientVolume, float footstepVolume)
        {
            _cachedMusicVolume = Game1.options.musicVolumeLevel;
            _cachedSoundVolume = Game1.options.soundVolumeLevel;
            _cachedAmbientVolumeLevel = Game1.options.ambientVolumeLevel;
            _cachedFootstepVolumeLevel = Game1.options.footstepVolumeLevel;

            SetMusicVolume(_cachedMusicVolume <= 0 ? 0 : musicVolume * _cachedMusicVolume);
            SetSoundVolume(_cachedSoundVolume <= 0 ? 0 : soundVolume * _cachedSoundVolume);
            SetAmbientSoundVolume(_cachedAmbientVolumeLevel <= 0 ? 0 : ambientVolume * _cachedAmbientVolumeLevel);
            SetFootstepVolume(_cachedFootstepVolumeLevel <= 0 ? 0 : footstepVolume * _cachedFootstepVolumeLevel);
        }

        private static void SetMusicVolume(float volume)
        {
            Game1.musicCategory.SetVolume(volume);
            Game1.musicPlayerVolume = volume;
        }

        private static void SetSoundVolume(float volume)
        {
            Game1.soundCategory.SetVolume(volume);
        }

        private static void SetAmbientSoundVolume(float volume)
        {
            Game1.ambientCategory.SetVolume(volume);
            Game1.ambientPlayerVolume = volume;
        }

        private static void SetFootstepVolume(float volume)
        {
            Game1.footstepCategory.SetVolume(volume);
        }

        public static void RestoreSoundVolumeCache()
        {
            SetMusicVolume(_cachedMusicVolume);
            SetSoundVolume(_cachedSoundVolume);
            SetAmbientSoundVolume(_cachedAmbientVolumeLevel);
            SetFootstepVolume(_cachedFootstepVolumeLevel);
        }
    }
}
