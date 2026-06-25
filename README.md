# Vocaluxe-Godot

A from-scratch rewrite of the [Vocaluxe](https://github.com/Vocaluxe/Vocaluxe) karaoke game
in the **Godot 4** engine using **C# / .NET 10** — exploring a long-term, cross-platform
modernization of the project (see upstream issue
[#768](https://github.com/Vocaluxe/Vocaluxe/issues/768)).

> ⚠️ Experimental. This is an independent rework with its own history — **not** a port of the
> existing codebase. The alternative big-bang .NET 10 port lives in upstream PR #881.

## Approach

- **`VocaluxeCore/`** — portable `netstandard2.1` library: song model, UltraStar `.txt`
  parser/writer and scoring logic, ported from upstream `VocaluxeLib` (no engine dependency).
- **`VocaluxeCore.Tests/`** — NUnit regression tests for the core.
- **`game/`** — the Godot 4.7 C# project (`net10.0`), references `VocaluxeCore`.
- **`native/`** — the C++ pitch tracker (built as a shared lib, used via P/Invoke).

Microphone capture and pitch detection reuse the upstream native stack (PortAudio + pitch
tracker via P/Invoke); song audio plays through Godot's `AudioStreamPlayer`; UI is rebuilt
natively in Godot scenes. Desktop-first (Windows/Linux/macOS).

## Build & run

```bash
make -C native/PitchTracker                # build the native pitch tracker (libPitchTracker.so)
dotnet test VocaluxeGodot.sln              # core + audio regression tests
dotnet build game/Vocaluxe.csproj          # build the Godot C# game
godot --path game                          # run (Godot 4.7 .NET build)
```

The native pitch tracker (`native/PitchTracker/`, C++) must be built before the audio tests
can run; without it the pitch tests skip cleanly.

GPLv3 (inherited from upstream Vocaluxe).
