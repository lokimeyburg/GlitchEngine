# Glitch Game Engine


This is the home of _Glitch_, my small and simple game engine. Glitch is written in C# and built on .NET Core which means it can run on Mac, Windows and Linux. 

> This is purely a one-man developer game engine built from scratch for fun. Don't expect much.

## Quick Start

Before  you start, make sure you have [.NET Core v2](https://www.microsoft.com/net/learn/get-started/macos) installed. You may also need to install SDL2 (`brew install sdl2`).

```
$ git clone git@github.com:lokimeyburg/GlitchEngine.git
$ cd GlitchEngine
$ git submodule update --init --recursive
$ cd src/Glitch
$ dotnet restore
$ dotnet build
$ dotnet run
```

## Extrenal Libraries

Glitch uses a few external libraries. Here's a list of the main ones

- [Veldird](https://github.com/mellinoe/veldrid) (v4.3.3) - A low-level, portable graphics and compute library for .NET Core.
