# Notes

My scratchpad of notes, ideas and tasks.

## Tasks

- [x] Demo with a sphere, floor and one light
- [x] Strip the scene code
- [x] Add IRenderable and ICullRenderable interfaces. 
- [x] Make sure to uncomment all CullRenderable code.
- [x] Create a seperate Game repository that uses the GlitchEngine
- [x] Get Spark to compile and output it's DLL in Glitch's output
- [x] Get the Asset System to read both loose file and embedded assets
- [ ] [WIP] Render the Skybox Game Object by reading the `.scene` file
- [ ] Render primitive textured meshes as Game Objects
- [ ] Rename `EngineEmeddedAssets.cs` to `EngineEmbeddedAssets.cs`
- [ ] Refactor ShaderHelper.LoadBytecode() to use embedded assets instead of loading from a filepath (that may or may not be there depending on how everything is published) 
- [ ] Fix the spelling of `EngineSerializationBinder`

## Notes for physics engine

I've removed a bunch of physics code from the Transform component and will have to add them back at some point.

## Thoughts on naming things

- Game => Spark
- Asset System => BiFrost

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