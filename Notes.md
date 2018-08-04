# Notes

My scratchpad of notes, ideas and tasks.

## Roadmap

- [x] Camera
- [x] Asset System
- [ ] [WIP] Mesh Renderer
- [ ] Scene Serializer Workflow
- [ ] Lighting
- [ ] Editor Launcher
- [ ] Editor Inspectors
- [ ] Physics System

## Tasks

### Mesh Renderer

- [x] Render primitive textured meshes as Game Objects
- [x] Render primitive textured meshes by reading the `.scene` file
- [x] Scale the rendered mesh based on the parent GameObject's Transform component
- [x] Remove `renderable.cs` and move renderable interfaces to `Glitch.Behaviors`
- [x] Position the rendered mesh based on the parent GameObject's Transform componenet
- [ ] [WIP] Add two circles and a floor
- [ ] Add additional properties to the mesh object (alpha texture & material properties)
 
### Scene Serializer Workflow

- [ ] Have the ability to easily switch between two debug states: serialization/deserialization

### Refactoring

These tasks are low priority (get to these whenever blocked by another task)

- [x] Fix the spelling of `EngineSerializationBinder`
- [x] Rename `EngineEmeddedAssets.cs` to `EngineEmbeddedAssets.cs`
- [ ] Move `GraphicsSystem.cs` to `Glitch.Graphics` namespace
- [ ] Delete `RefOrImmediate.cs` if it's no longer being used by now.
- [ ] Improve build process inside Visual Studio (make sure everything ends up under the /Debug folder)
- [ ] Refactor ShaderHelper.LoadBytecode() to use embedded assets instead of loading from a filepath (that may or may not be there depending on how everything is published)
- [ ] Refactor out "Custom SceneAsset Serializer" in `GlitchDemo.cs:153`

### Physics System

- [ ] I've removed a bunch of physics code from the Transform component and will have to add them back at some point.

### [Archived] Camera & Asset System

- [x] Demo with a sphere, floor and one light
- [x] Strip the scene code
- [x] Add IRenderable and ICullRenderable interfaces. 
- [x] Make sure to uncomment all CullRenderable code.
- [x] Create a seperate Game repository that uses the GlitchEngine
- [x] Get Spark to compile and output it's DLL in Glitch's output
- [x] Get the Asset System to read both loose file and embedded assets
- [x] Render Skybox using embedded assets
- [x] Render the Skybox Game Object by reading the `.scene` file
- [x] Render the Camera by reading the `.scene` file
- [x] Clean up how the Camera is being added to the scene

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
