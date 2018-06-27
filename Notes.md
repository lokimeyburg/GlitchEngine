# Notes

My scratchpad of notes, ideas and tasks.

## Tasks

- [x] Demo with a sphere, floor and one light
- [ ] Create game object, system registry & component system
- [ ]
- [ ] Split demo into Glitch, Launcher and Editor.
- [ ] Create a seperate Game repository that uses the GlitchEngine

## Notes for physics engine

I've removed a bunch of physics code from the Transform component and will have to add them back at some point.

## DotNET Terminal Commands Cheat Sheet

Creating a new library:

```
dotnet new classlib -o library
dotnet sln add library/library.csproj
```

Creating a new console app:

```
dotnet new console -o app
dotnet sln add app/app.csproj
```

Referencing a package:

```
dotnet add app/app.csproj reference library/library.csproj
```