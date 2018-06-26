# Notes

My scratchpad of notes, ideas and tasks.

## Tasks

- [x] Demo with a sphere, floor and one light
- [ ] Split demo into Glitch, Launcher and Editor.
- [ ] Create a seperate Game repository that uses the GlitchEngine

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