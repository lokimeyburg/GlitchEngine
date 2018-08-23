# Notes

My scratchpad of notes, ideas and tasks.

## Roadmap

- [x] Camera
- [x] Asset System
- [x] Mesh Renderer
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
- [x] Make the camera component adhere to it's parent's GameObject
- [x] Add two circles and a floor
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

## Notes about meshes randomly not being rendered

Update #3: Bug resolved! Turns out it was a multi-threading issue which was resolved by seperating the creation of game objects (still multi-threaded) and adding cull meshes to the render queue (main thread)

Update #2: `SceneAsset.cs:22` reproduces the error only if "parallel == true" 

Update: I've isolated the cause of the problem to be related to parallel tasks causing intermittent rendering

- [x] Ensuring both meshes get added to the render tree (`scene.cs:77`)
- [X] Enabling/disabling multi-threading
- [x] Perhaps it's getting culled from the render view? Update: Yes, `Scene.CollectVisibleObjects()` is excluding the objects
- [x] It looks like it might be that the camera fustrum is not being set correctly. Update: it does not seem to be adhering to it's GameObject Transform position.
- [x] Confirm that intermittent rendering occurs, while holding the frustum constant

=> Initial Load
    FarBottomLeft: {<-170.786, 424.329, 14.38985>}
    FarBottomRight: {<-64.79684, 424.329, 120.3791>}
    FarTopLeft: {<-67.104, 455.4336, -89.29219>}
    FarTopRight: {<38.88521, 455.4336, 16.69702>}
    NearBottomLeft: {<-82.80892, -162.185, -73.58727>}
    NearBottomRight: {<23.18029, -162.185, 32.40194>}
    NearTopLeft: {<20.87312, -131.0804, -177.2693>}
    NearTopRight: {<126.8623, -131.0804, -71.2801>}

=> After going very backwards (+Z)
    FarBottomLeft: {<-170.839, 424.3381, 80.89304>}
    FarBottomRight: {<-64.84976, 424.3381, 186.8822>}
    FarTopLeft: {<-67.15693, 455.4427, -22.78898>}
    FarTopRight: {<38.83226, 455.4427, 83.20023>}
    NearBottomLeft: {<-82.86185, -162.176, -7.084068>}
    NearBottomRight: {<23.12735, -162.176, 98.90514>}
    NearTopLeft: {<20.82018, -131.0714, -110.7661>}
    NearTopRight: {<126.8094, -131.0714, -4.776894>}

	I can confirm that the _octree is getting both ICullRenderable objects (circle & plane) while still not rendering anything.


	Sphere Bounding Box
	{Min:<-1, -1, -5.994522>, Max:<1, 1, -4.005478>}
    Max: {<1, 1, -4.005478>}
    Min: {<-1, -1, -5.994522>}

	Plane Bounding Box
	{Min:<-2.5, -1, -7.5>, Max:<2.5, -1, -2.5>}
    Max: {<2.5, -1, -2.5>}
    Min: {<-2.5, -1, -7.5>}

