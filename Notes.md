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
- [x] Render Skybox using embedded assets
- [x] Render the Skybox Game Object by reading the `.scene` file
- [x] Render the Camera by reading the `.scene` file
- [x] Clean up how the Camera is being added to the scene
- [ ] [WIP] Render primitive textured meshes as Game Objects
- [ ] Render primitive textured meshes by reading the `.scene` file
- [ ] Delete `RefOrImmediate.cs` if it's no longer being used by now.
- [x] Rename `EngineEmeddedAssets.cs` to `EngineEmbeddedAssets.cs`
- [ ] Refactor ShaderHelper.LoadBytecode() to use embedded assets instead of loading from a filepath (that may or may not be there depending on how everything is published) 
- [x] Fix the spelling of `EngineSerializationBinder`

## Notes for physics engine

I've removed a bunch of physics code from the Transform component and will have to add them back at some point.

## Thoughts on naming things

- Game => Spark
- Asset System => BiFrost

## MeshRenderer Architecture Notes

MeshRenderer is a `BoundsRenderItem` which is a rendereable item.

 // Serialization Accessors
public RefOrImmediate<MeshData> Mesh
public RefOrImmediate<TextureData> Texture
private void RecreateModel()
private void RecreateTexture()

public float Opacity
public bool CastShadows
private void MakeTransparent()
private void MakeOpaque()

public BoundingBox Bounds
public MeshRenderer(RefOrImmediate<MeshData> meshData, RefOrImmediate<TextureData> texture)
public RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
public IList<string> GetStagesParticipated()
public void Render(RenderContext rc, string pipelineStage)
private unsafe void SortTransparentTriangles()
private ushort[] GetMeshIndices()
private Vector3[] GetMeshVertexPositions()

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