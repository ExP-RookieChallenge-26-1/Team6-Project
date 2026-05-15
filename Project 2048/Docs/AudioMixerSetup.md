# Project 2048 Audio Mixer Setup

## Asset Locations

- Mixer: `Assets/Audio/Mixers/Project2048AudioMixer.mixer`
- Runtime settings: `Assets/Resources/Audio/Project2048AudioSettings.asset`
- Main theme clip: `Assets/Sounds/메인테마.mp3`
- Scripts: `Assets/Scripts/Audio/`

## Mixer Groups

`Project2048AudioMixer` has these groups under `Master`:

- `BGM`: looping main theme and future music
- `SFX`: combat, hit, attack, skill, tile move, tile merge, victory, defeat, reward sounds
- `UI`: button and menu feedback
- `Ambience`: future environment loops

## Exposed Parameters

Expose these group volume parameters in the mixer:

- `BGMVolume`
- `SFXVolume`
- `UIVolume`
- `AmbienceVolume`

`SimpleBgmDucker` controls only `BGMVolume`. It does not permanently lower the music; it ducks the music briefly when important SFX plays.

## Default Ducking Values

- Base BGM volume: `-14 dB`
- Ducked BGM volume: `-20 dB`
- Attack: `0.05 s`
- Hold: `0.15 s`
- Release: `0.35 s`

## AudioSource Output Routing

Set `AudioSource.outputAudioMixerGroup` by sound type:

- Main theme `AudioSource`: `BGM`
- Combat/world/result/reward SFX sources: `SFX`
- Button/menu sounds: `UI`
- Environment loops: `Ambience`

The current runtime code also resolves these automatically through `Project2048AudioSettings`:

- `PersistentBgmPlayer` routes the main theme to `BGM`.
- `CombatUiView`, `CombatWorldSpriteView`, and `PrototypeCombatEventAudioPlayer` route prototype combat sounds to `SFX`.
- `CombatEffectAudioPlayer` copies the template source mixer group to temporary pitched one-shot sources.

## When To Duck BGM

Call `bgmDucker.DuckBgm()` for sounds that carry gameplay information:

- player attack and skill activation
- enemy hit, player hit, enemy death, enemy defend
- tile merge
- victory, defeat, reward-confirm sounds

Avoid ducking on every small tile move unless it becomes important feedback, because repeated move sounds can make the BGM pump too often.
