using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;

namespace TGC.MonoGame.TP.Managers
{
    public class SoundManager
    {
        private ContentManager _content;
        private readonly Dictionary<string, SoundEffect> _soundEffects;
        private readonly Dictionary<string, int> _lastPlayTick = new Dictionary<string, int>();
        private Song _currentSong;
        private AudioListener _listener;

        private Dictionary<string, Queue<SoundEffectInstance>> _soundPools = new Dictionary<string, Queue<SoundEffectInstance>>();
        private const int MAX_INSTANCES_PER_SOUND = 8;
        private float _userMusicVolume = -1f;

        public SoundManager()
        {
            _soundEffects = new Dictionary<string, SoundEffect>();
            _listener = new AudioListener();
        }

        public void LoadContent(ContentManager content)
        {
            _content = content;

            // Registrar los sonidos disponibles. 
            // La ruta es relativa a la carpeta Content y no lleva extension.
            AddSoundEffect("cannon_fire", "Sounds/1-cannon_fire");
            AddSoundEffect("colision_casa", "Sounds/2-freesound_community-medium-explosion-40472");
            AddSoundEffect("impacto_mediana_escala", "Sounds/3-dragon-studio-boulder-impact-487673");
            AddSoundEffect("agarrar_combustible_1", "Sounds/4a-Power Up");
            AddSoundEffect("agarrar_combustible_2", "Sounds/4b-gravity_inverter");
            AddSoundEffect("viento", "Sounds/5-tanweraman-desert-wind-2-350417");
            AddSoundEffect("cooldown_not_ready", "Sounds/6-gun_reload_lock_or_click_sound");
            AddSoundEffect("escalera", "Sounds/7-freesound_community-floorcracking-84506");
            AddSoundEffect("enemy_cannon_fire", "Sounds/8-freesound_community-gunner-sound-43794");
            AddSoundEffect("planta_rodadora", "Sounds/9-freesound_community-leaves-14478");
            AddSoundEffect("carroceria_avanzando_1", "Sounds/10a-engine_heavy_loop");
            AddSoundEffect("carroceria_avanzando_2", "Sounds/10b-engine_heavy_slow_loop");
            AddSoundEffect("carroceria_avanzando_3", "Sounds/10c-engine_heavy_average_loop");
            AddSoundEffect("carroceria_avanzando_4", "Sounds/10d-engine_heavy_fast_loop");
            AddSoundEffect("bajo_combustible_1", "Sounds/11a-588220__mehraniiii__magical-warning");
            AddSoundEffect("bajo_combustible_2", "Sounds/11b-629312__greatsoundstube__warning-chime");
            AddSoundEffect("rotar_torreta", "Sounds/12-freesound_community-tank-turret-rotate-14879");
            AddSoundEffect("klaxon", "Sounds/13-universfield-truck-signal-153263");
            AddSoundEffect("fuego", "Sounds/14-dragon-studio-fire-sounds-405444");
            AddSoundEffect("golpear_arbol", "Sounds/15-u_xjrmmgxfru-hit-tree-03-266306");
            AddSoundEffect("golpear_roca", "Sounds/16-u_xjrmmgxfru-hit-rock-03-266305");
            AddSoundEffect("player_muere", "Sounds/17- rock_breaking");
        }

        public void AddSoundEffect(string name, string assetPath)
        {
            if (!_soundEffects.ContainsKey(name))
            {
                _soundEffects.Add(name, _content.Load<SoundEffect>(assetPath));
            }
        }

        public void PlayMusic(string assetPath, bool isLooping = true)
        {
            StopMusic();
            _currentSong = _content.Load<Song>(assetPath);
            MediaPlayer.IsRepeating = isLooping;

            if (_userMusicVolume >= 0f)
                MediaPlayer.Volume = _userMusicVolume;
            else
                MediaPlayer.Volume = GetVolumeForMusic(assetPath);
            
            MediaPlayer.Play(_currentSong);
        }

        public void StopMusic()
        {
            if (MediaPlayer.State == MediaState.Playing)
            {
                MediaPlayer.Stop();
            }
        }

        public void ChangeMusicVolume(float delta)
        {
            if (_userMusicVolume < 0f)
                _userMusicVolume = MediaPlayer.Volume;

            _userMusicVolume = MathHelper.Clamp(_userMusicVolume + delta, 0f, 1f);

            MediaPlayer.Volume = _userMusicVolume;
        }

        //Reproducir sonido 3D
        public void PlaySound3D(string soundName, Vector3 emitterPosition, Vector3 listenerPosition, Vector3 listenerForward, float pitch = 0f)
        {
            if (!_soundEffects.TryGetValue(soundName, out SoundEffect soundEffect))
                return;

            // Obtener o crear el pool
            if (!_soundPools.TryGetValue(soundName, out var pool))
            {
                pool = new Queue<SoundEffectInstance>();
                _soundPools[soundName] = pool;
            }

            SoundEffectInstance instance = null;

            while (pool.Count > 0)
            {
                var candidate = pool.Dequeue();
                if (candidate.State == SoundState.Stopped)
                {
                    instance = candidate;
                    break;
                }
                candidate.Dispose();
            }

            if (instance == null)
            {
                instance = soundEffect.CreateInstance();
            }

            instance.Volume = GetVolumeForSfx(soundName);
            instance.Pitch = MathHelper.Clamp(pitch, -1f, 1f);

            AudioEmitter emitter = new AudioEmitter
            {
                Position = emitterPosition.ToNumerics(),
                Forward = Vector3.Forward.ToNumerics(),
                Up = Vector3.Up.ToNumerics()
            };

            _listener.Position = listenerPosition.ToNumerics();
            _listener.Forward = listenerForward.ToNumerics();
            _listener.Up = Vector3.Up.ToNumerics();
            _listener.Velocity = System.Numerics.Vector3.Zero;

            instance.Apply3D(_listener, emitter);
            instance.Play();

            if (pool.Count < MAX_INSTANCES_PER_SOUND)
            {
                pool.Enqueue(instance);
            }
        }

        //Reproducir sonido stereo
        public void PlaySound(string soundName)
        {
            if (!_soundEffects.TryGetValue(soundName, out SoundEffect soundEffect))
                return;

            // Obtener o crear el pool para este sonido
            if (!_soundPools.TryGetValue(soundName, out var pool))
            {
                pool = new Queue<SoundEffectInstance>();
                _soundPools[soundName] = pool;
            }

            SoundEffectInstance instance = null;

            // Buscar una instancia disponible en el pool
            while (pool.Count > 0)
            {
                var candidate = pool.Dequeue();
                if (candidate.State == SoundState.Stopped)
                {
                    instance = candidate;
                    break;
                }
                // Si esta reproduciendose, descartarla (no deberia pasar pero por seguridad)
                candidate.Dispose();
            }

            // Si no hay instancia disponible, crear una nueva
            if (instance == null)
            {
                instance = soundEffect.CreateInstance();
            }

            instance.Volume = GetVolumeForSfx(soundName);
            instance.Play();

            // Devolver al pool si no esta lleno
            if (pool.Count < MAX_INSTANCES_PER_SOUND)
            {
                pool.Enqueue(instance);
            }
            else
            {
                // Si el pool esta lleno, dejar que se reproduzca y se destruya sola
                // (no la devolvemos al pool)
            }
        }

        /// <summary>
        /// Reproduce un sonido solo si ha pasado el cooldown especificado desde la ultima vez que se reprodujo.
        /// Evita que suena como una amtetralladora
        /// </summary>
        /// <param name="soundName">Nombre del sonido registrado.</param>
        /// <param name="cooldownMs">Milisegundos mínimos entre reproducciones (default: 400ms).</param>
        public void PlaySoundWithCooldown(string soundName, int cooldownMs = 400)
        {
            if (!_soundEffects.ContainsKey(soundName)) return;

            int currentTick = Environment.TickCount;
            if (_lastPlayTick.TryGetValue(soundName, out int lastTick))
            {
                if (currentTick - lastTick < cooldownMs)
                    return; // Aún en cooldown, se descarta
            }

            _lastPlayTick[soundName] = currentTick;
            PlaySound(soundName);
        }

        /// <summary>
        /// Version 3D con cooldown. Evita que sonidos 3D se repitan como ametralladora.
        /// </summary>
        public void PlaySound3DWithCooldown(string soundName, Vector3 emitterPosition,
            Vector3 listenerPosition, Vector3 listenerForward, int cooldownMs = 400)
        {
            int currentTick = Environment.TickCount;
            if (_lastPlayTick.TryGetValue(soundName, out int lastTick))
            {
                if (currentTick - lastTick < cooldownMs)
                    return;
            }

            _lastPlayTick[soundName] = currentTick;
            PlaySound3D(soundName, emitterPosition, listenerPosition, listenerForward);
        }

        /// <summary>
        /// Obtiene el volumen normalizado (0.0f a 1.0f) para un SFX específico basado en GameConfig.
        /// </summary>
        private float GetVolumeForSfx(string soundName)
        {
            float configValue = soundName switch
            {
                "cannon_fire" => GameConfig.Audio.Sfx.CannonFire,
                "colision_casa" => GameConfig.Audio.Sfx.ColisionCasa,
                "impacto_mediana_escala" => GameConfig.Audio.Sfx.ImpactoMedianaEscala,
                "agarrar_combustible_1" => GameConfig.Audio.Sfx.AgarrarCombustible1,
                "agarrar_combustible_2" => GameConfig.Audio.Sfx.AgarrarCombustible2,
                "viento" => GameConfig.Audio.Sfx.Viento,
                "cooldown_not_ready" => GameConfig.Audio.Sfx.CooldownNotReady,
                "escalera" => GameConfig.Audio.Sfx.Escalera,
                "enemy_cannon_fire" => GameConfig.Audio.Sfx.EnemyCannonFire,
                "planta_rodadora" => GameConfig.Audio.Sfx.PlantaRodadora,
                "carroceria_avanzando_1" => GameConfig.Audio.Sfx.CarroceriaAvanzando1,
                "carroceria_avanzando_2" => GameConfig.Audio.Sfx.CarroceriaAvanzando2,
                "carroceria_avanzando_3" => GameConfig.Audio.Sfx.CarroceriaAvanzando3,
                "carroceria_avanzando_4" => GameConfig.Audio.Sfx.CarroceriaAvanzando4,
                "bajo_combustible_1" => GameConfig.Audio.Sfx.BajoCombustible1,
                "bajo_combustible_2" => GameConfig.Audio.Sfx.BajoCombustible2,
                "rotar_torreta" => GameConfig.Audio.Sfx.RotarTorreta,
                "klaxon" => GameConfig.Audio.Sfx.Klaxon,
                "fuego" => GameConfig.Audio.Sfx.Fuego,
                "golpear_arbol" => GameConfig.Audio.Sfx.GolpearArbol,
                "golpear_roca" => GameConfig.Audio.Sfx.GolpearRoca,
                "player_muere" => GameConfig.Audio.Sfx.PlayerMuere,
                _ => 50f
            };

            return MathHelper.Clamp(configValue / 100f, 0f, 1f);
        }

        /// <summary>
        /// Obtiene el volumen normalizado (0.0f a 1.0f) para una pista musical específica basada en GameConfig.
        /// </summary>
        private float GetVolumeForMusic(string assetPath)
        {
            float configValue = assetPath switch
            {
                "Music/100-song18" => GameConfig.Audio.Music.Menu,
                "Music/101-Juhani Junkala [Retro Game Music Pack] Level 1" => GameConfig.Audio.Music.Level1,
                _ => 50f
            };

            return MathHelper.Clamp(configValue / 100f, 0f, 1f);
        }

    }
}