using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;  
using Veldrid.ImageSharp;
using Glitch.Objects;
using Veldrid.StartupUtilities;
using Veldrid.Utilities;
using Veldrid.Sdl2;
using Newtonsoft.Json;
using Veldrid;
using Glitch.Graphics;
using Glitch.Assets;
using Glitch.ProjectSystem;

namespace Glitch
{
    public class GlitchDemo
    {
        private Sdl2Window _window;
        private GraphicsDevice _gd;
        private Scene _scene;
        private readonly ImGuiRenderable _igRenderable;
        private readonly SceneContext _sc = new SceneContext();
        private bool _windowResized;
        private RenderOrderKeyComparer _renderOrderKeyComparer = new RenderOrderKeyComparer();
        private bool _recreateWindow = true;

        private static double _desiredFrameLengthSeconds = 1.0 / 60.0;
        private static bool _limitFrameRate = false;
        private static FrameTimeAverager _fta = new FrameTimeAverager(0.666);
        private CommandList _frameCommands;

        private event Action<int, int> _resizeHandled;

        private readonly string[] _msaaOptions = new string[] { "Off", "2x", "4x", "8x", "16x", "32x" };
        private int _msaaOption = 0;
        private TextureSampleCount? _newSampleCount;

        private readonly Dictionary<string, ImageSharpTexture> _textures = new Dictionary<string, ImageSharpTexture>();
        private FullScreenQuad _fsq;

        public GlitchDemo()
        {
            // Project Manifest
            // --------------------------------------------------
            ProjectManifest projectManifest;
            string currentDir = AppContext.BaseDirectory;
            string manifestName = null;
            
            foreach (var file in Directory.EnumerateFiles(currentDir + "Assets"))
            {
                if (file.EndsWith("manifest"))
                {
                    if (manifestName != null)
                    {
                        string errorMessage  = "Error: Multiple project manifests in this directory: " + currentDir;
                        Console.WriteLine(errorMessage);
                        throw new System.Exception(errorMessage);
                    }
                    manifestName = file;
                }
            }

            using (var fs = File.OpenRead(manifestName))
            using (var sr = new StreamReader(fs))
            using (var jtr = new JsonTextReader(sr))
            {
                var js = new JsonSerializer();
                try
                {
                    projectManifest = js.Deserialize<ProjectManifest>(jtr);
                }
                catch (Exception e)
                {
                    string errorMessage = "An error was encountered while loading the project manifest.";
                    Console.WriteLine(errorMessage);
                    Console.WriteLine(e);
                    throw new System.NullReferenceException(errorMessage);
                }
            }

            Game game = new Game();

            AssemblyLoadSystem als = new AssemblyLoadSystem();
            als.LoadFromProjectManifest(projectManifest, AppContext.BaseDirectory);
            game.SystemRegistry.Register(als);

            // Window & Graphics Device
            // --------------------------------------------------
            WindowCreateInfo windowCI = new WindowCreateInfo
            {
                X = 50,
                Y = 50,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowInitialState = WindowState.Normal,
                WindowTitle = "Glitch Demo"
            };
            GraphicsDeviceOptions gdOptions = new GraphicsDeviceOptions(false, null, false, ResourceBindingModel.Improved, true);
#if DEBUG
            gdOptions.Debug = true;
#endif

            VeldridStartup.CreateWindowAndGraphicsDevice(
                windowCI,
                gdOptions,
                //GraphicsBackend.Metal,
                //GraphicsBackend.Vulkan,
                //GraphicsBackend.OpenGL,
                //GraphicsBackend.OpenGLES,
                out _window,
                out _gd);
            _window.Resized += () => _windowResized = true;


            // SCENE
            // --------------------------------------------------

            // Create a new scene and set it as the current Scene Context
            _scene = new Scene(_gd, _window.Width, _window.Height);
            // sets the SceneContext.Camera to the scene's camera (scene.camera)
            _sc.SetCurrentScene(_scene);

            // GUI
            // --------------------------------------------------

            _igRenderable = new ImGuiRenderable(_window.Width, _window.Height);
            _resizeHandled += (w, h) => _igRenderable.WindowResized(w, h);
            _scene.AddRenderable(_igRenderable);
            _scene.AddUpdateable(_igRenderable);

            // START DOODLE
            // -------------------------------

            // Add the Skybox
            Skybox skybox = Skybox.LoadDefaultSkybox();
            _scene.AddRenderable(skybox);



            AssetSystem assetSystem = new AssetSystem(Path.Combine(AppContext.BaseDirectory, projectManifest.AssetRoot), als.Binder);
            game.SystemRegistry.Register(assetSystem);


            // SceneAsset sceneAsset;
            // AssetID mainSceneID = projectManifest.OpeningScene.ID;
            // if (mainSceneID.IsEmpty)
            // {
            //     var scenes = assetSystem.Database.GetAssetsOfType(typeof(SceneAsset));
            //     if (!scenes.Any())
            //     {
            //         Console.WriteLine("No scenes were available to load.");
            //         // return -1;
            //     }
            //     else
            //     {
            //         mainSceneID = scenes.First();
            //     }
            // }

            // sceneAsset = assetSystem.Database.LoadAsset<SceneAsset>(mainSceneID);
            // sceneAsset.GenerateGameObjects();

            
            // END DOODLE
            // -------------------------------

            // AddSphere(new Vector3(0f));

            // AddSphere(new Vector3(0f, 0f, 25f));

            // AddFloor(new Vector3(0f, -12f, 0f));

            _sc.Camera.Position = new Vector3(-80, 25, -4.3f);
            _sc.Camera.Yaw = -MathF.PI / 2;
            _sc.Camera.Pitch = -MathF.PI / 9;

            ScreenDuplicator duplicator = new ScreenDuplicator();
            _scene.AddRenderable(duplicator);

            // TODO: rename FullScreenQuad to FinalBufferObject or something
            _fsq = new FullScreenQuad();
            _scene.AddRenderable(_fsq);

            CreateAllObjects();
        }

        private void AddFloor(Vector3 offset)
        {
            ObjParser parser = new ObjParser();
            using (FileStream objStream = File.OpenRead(AssetHelper.GetPath("Models/floor.obj")))
            {
                ObjFile atriumFile = parser.Parse(objStream);
                foreach (ObjFile.MeshGroup group in atriumFile.MeshGroups)
                {
                    Vector3 scale = new Vector3(30f);
                    ConstructedMeshInfo mesh = atriumFile.GetMesh(group);
                    
                    MaterialPropsAndBuffer materialProps = null;
                    ImageSharpTexture overrideTextureData = null;
                    ImageSharpTexture alphaTexture = null;

                    overrideTextureData = LoadTexture(AssetHelper.GetPath("Models/gray.png"), true);

                    AddTexturedMesh(
                        mesh,
                        overrideTextureData,
                        alphaTexture,
                        materialProps,
                        offset,
                        Quaternion.Identity,
                        scale,
                        group.Name);
                }
            }
        }
        private void AddSphere(Vector3 offset)
        {
            ObjParser parser = new ObjParser();
            using (FileStream objStream = File.OpenRead("Assets/Models/sphere.obj"))
            {
                ObjFile atriumFile = parser.Parse(objStream);
                foreach (ObjFile.MeshGroup group in atriumFile.MeshGroups)
                {
                    Vector3 scale = new Vector3(0.3f);
                    ConstructedMeshInfo mesh = atriumFile.GetMesh(group);
                    
                    MaterialPropsAndBuffer materialProps = null;
                    ImageSharpTexture overrideTextureData = null;
                    ImageSharpTexture alphaTexture = null;

                    overrideTextureData = LoadTexture(AssetHelper.GetPath("Models/gray.png"), true);

                    AddTexturedMesh(
                        mesh,
                        overrideTextureData,
                        alphaTexture,
                        materialProps,
                        offset,
                        Quaternion.Identity,
                        scale,
                        group.Name);
                }
            }
        }

        private ImageSharpTexture LoadTexture(string texturePath, bool mipmap) // Plz don't call this with the same texturePath and different mipmap values.
        {
            if (!_textures.TryGetValue(texturePath, out ImageSharpTexture tex))
            {
                tex = new ImageSharpTexture(texturePath, mipmap);
                _textures.Add(texturePath, tex);
            }

            return tex;
        }

        private void AddTexturedMesh(
            MeshData meshData,
            ImageSharpTexture texData,
            ImageSharpTexture alphaTexData,
            MaterialPropsAndBuffer materialProps,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            string name)
        {
            TexturedMesh mesh = new TexturedMesh(name, meshData, texData, alphaTexData, materialProps ?? CommonMaterials.Brick);
            mesh.Transform.Position = position;
            mesh.Transform.Rotation = rotation;
            mesh.Transform.Scale = scale;
           _scene.AddRenderable(mesh);
        }

        public void Run()
        {
            long previousFrameTicks = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (_window.Exists)
            {
                long currentFrameTicks = sw.ElapsedTicks;
                double deltaSeconds = (currentFrameTicks - previousFrameTicks) / (double)Stopwatch.Frequency;

                while (_limitFrameRate && deltaSeconds < _desiredFrameLengthSeconds)
                {
                    currentFrameTicks = sw.ElapsedTicks;
                    deltaSeconds = (currentFrameTicks - previousFrameTicks) / (double)Stopwatch.Frequency;
                }

                previousFrameTicks = currentFrameTicks;

                InputSnapshot snapshot = null;
                snapshot = _window.PumpEvents();
                InputTracker.UpdateFrameInput(snapshot);
                Update((float)deltaSeconds);
                if (!_window.Exists)
                {
                    break;
                }

                Draw();
            }

            DestroyAllObjects();
            _gd.Dispose();
        }

        private void Update(float deltaSeconds)
        {
            _fta.AddTime(deltaSeconds);
            _scene.Update(deltaSeconds);

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("Settings"))
                {
                    if (ImGui.BeginMenu("Graphics Backend"))
                    {

                        if (ImGui.MenuItem("Vulkan", GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan)))
                        {
                            ChangeBackend(GraphicsBackend.Vulkan);
                        }
                        if (ImGui.MenuItem("OpenGL", GraphicsDevice.IsBackendSupported(GraphicsBackend.OpenGL)))
                        {
                            ChangeBackend(GraphicsBackend.OpenGL);
                        }
                        if (ImGui.MenuItem("OpenGL ES", GraphicsDevice.IsBackendSupported(GraphicsBackend.OpenGLES)))
                        {
                            ChangeBackend(GraphicsBackend.OpenGLES);
                        }
                        if (ImGui.MenuItem("Direct3D 11", GraphicsDevice.IsBackendSupported(GraphicsBackend.Direct3D11)))
                        {
                            ChangeBackend(GraphicsBackend.Direct3D11);
                        }
                        if (ImGui.MenuItem("Metal", GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal)))
                        {
                            ChangeBackend(GraphicsBackend.Metal);
                        }
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("MSAA"))
                    {
                        if (ImGui.Combo("MSAA", ref _msaaOption, _msaaOptions))
                        {
                            ChangeMsaa(_msaaOption);
                        }

                        ImGui.EndMenu();
                    }
                    bool isFullscreen = _window.WindowState == WindowState.BorderlessFullScreen;
                    if (ImGui.MenuItem("Fullscreen", "F11", isFullscreen, true))
                    {
                        ToggleFullscreenState();
                    }
                    if (ImGui.MenuItem("Always Recreate Sdl2Window", string.Empty, _recreateWindow, true))
                    {
                        _recreateWindow = !_recreateWindow;
                    }
                    if (ImGui.IsItemHovered(HoveredFlags.Default))
                    {
                        ImGui.SetTooltip(
                            "Causes a new OS window to be created whenever the graphics backend is switched. This is much safer, and is the default.");
                    }
                    bool threadedRendering = _scene.ThreadedRendering;
                    if (ImGui.MenuItem("Render with multiple threads", string.Empty, threadedRendering, true))
                    {
                        _scene.ThreadedRendering = !_scene.ThreadedRendering;
                    }
                    bool tinted = _fsq.UseTintedTexture;
                    if (ImGui.MenuItem("Tinted output", string.Empty, tinted, true))
                    {
                        _fsq.UseTintedTexture = !tinted;
                    }
                    bool vsync = _gd.SyncToVerticalBlank;
                    if (ImGui.MenuItem("VSync", string.Empty, vsync, true))
                    {
                        _gd.SyncToVerticalBlank = !_gd.SyncToVerticalBlank;
                    }

                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Materials"))
                {
                    if (ImGui.BeginMenu("Brick"))
                    {
                        DrawIndexedMaterialMenu(CommonMaterials.Brick);
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("Vase"))
                    {
                        DrawIndexedMaterialMenu(CommonMaterials.Vase);
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("Reflective"))
                    {
                        DrawIndexedMaterialMenu(CommonMaterials.Reflective);
                        ImGui.EndMenu();
                    }

                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Debug"))
                {
                    if (ImGui.MenuItem("Refresh Device Objects"))
                    {
                        RefreshDeviceObjects(1);
                    }
                    if (ImGui.MenuItem("Refresh Device Objects (10 times)"))
                    {
                        RefreshDeviceObjects(10);
                    }
                    if (ImGui.MenuItem("Refresh Device Objects (100 times)"))
                    {
                        RefreshDeviceObjects(100);
                    }

                    ImGui.EndMenu();
                }

                ImGui.Text(_fta.CurrentAverageFramesPerSecond.ToString("000.0 fps / ") + _fta.CurrentAverageFrameTimeMilliseconds.ToString("#00.00 ms"));

                ImGui.EndMainMenuBar();
            }

            if (InputTracker.GetKeyDown(Key.F11))
            {
                ToggleFullscreenState();
            }

            if (InputTracker.GetKeyDown(Key.Keypad6))
            {
                _window.X += 10;
            }
            if (InputTracker.GetKeyDown(Key.Keypad4))
            {
                _window.X -= 10;
            }
            if (InputTracker.GetKeyDown(Key.Keypad8))
            {
                _window.Y += 10;
            }
            if (InputTracker.GetKeyDown(Key.Keypad2))
            {
                _window.Y -= 10;
            }

            _window.Title = _gd.BackendType.ToString();
        }

        private void ChangeMsaa(int msaaOption)
        {
            TextureSampleCount sampleCount = (TextureSampleCount)msaaOption;
            _newSampleCount = sampleCount;
        }

        private void RefreshDeviceObjects(int numTimes)
        {
            for (int i = 0; i < numTimes; i++)
            {
                DestroyAllObjects();
                CreateAllObjects();
            }
        }

        private void DrawIndexedMaterialMenu(MaterialPropsAndBuffer propsAndBuffer)
        {
            MaterialProperties props = propsAndBuffer.Properties;
            float intensity = props.SpecularIntensity.X;
            float reflectivity = props.Reflectivity;
            if (ImGui.SliderFloat("Intensity", ref intensity, 0f, 10f, intensity.ToString(), 1f)
                | ImGui.SliderFloat("Power", ref props.SpecularPower, 0f, 1000f, props.SpecularPower.ToString(), 1f)
                | ImGui.SliderFloat("Reflectivity", ref props.Reflectivity, 0f, 1f, props.Reflectivity.ToString(), 1f))
            {
                props.SpecularIntensity = new Vector3(intensity);
                propsAndBuffer.Properties = props;
            }
        }

        private void ToggleFullscreenState()
        {
            bool isFullscreen = _window.WindowState == WindowState.BorderlessFullScreen;
            _window.WindowState = isFullscreen ? WindowState.Normal : WindowState.BorderlessFullScreen;
        }

        private void Draw()
        {
            Debug.Assert(_window.Exists);
            int width = _window.Width;
            int height = _window.Height;

            if (_windowResized)
            {
                _windowResized = false;

                _gd.ResizeMainWindow((uint)width, (uint)height);
                _scene.Camera.WindowResized(width, height);
                _resizeHandled?.Invoke(width, height);
                CommandList cl = _gd.ResourceFactory.CreateCommandList();
                cl.Begin();
                _sc.RecreateWindowSizedResources(_gd, cl);
                cl.End();
                _gd.SubmitCommands(cl);
                cl.Dispose();
            }

            if (_newSampleCount != null)
            {
                _sc.MainSceneSampleCount = _newSampleCount.Value;
                _newSampleCount = null;
                DestroyAllObjects();
                CreateAllObjects();
            }

            _frameCommands.Begin();

            CommonMaterials.FlushAll(_frameCommands);

            _scene.RenderAllStages(_gd, _frameCommands, _sc);
            _gd.SwapBuffers();
        }

        private void ChangeBackend(GraphicsBackend backend)
        {
            DestroyAllObjects();
            bool syncToVBlank = _gd.SyncToVerticalBlank;
            _gd.Dispose();

            if (_recreateWindow)
            {

                WindowCreateInfo windowCI = new WindowCreateInfo
                {
                    X = _window.X,
                    Y = _window.Y,
                    WindowWidth = _window.Width,
                    WindowHeight = _window.Height,
                    WindowInitialState = _window.WindowState,
                    WindowTitle = "Veldrid Glitch"
                };

                _window.Close();

                _window = VeldridStartup.CreateWindow(ref windowCI);
                _window.Resized += () => _windowResized = true;
            }

            GraphicsDeviceOptions gdOptions = new GraphicsDeviceOptions(false, null, syncToVBlank, ResourceBindingModel.Improved, true);
#if DEBUG
            gdOptions.Debug = true;
#endif
            _gd = VeldridStartup.CreateGraphicsDevice(_window, gdOptions, backend);

            _scene.Camera.UpdateBackend(_gd);

            CreateAllObjects();
        }

        /// <summary>
        /// This method creates all of the other objects of the scene such as the lights, shadow maps etc.
        /// </summary>
        private void CreateAllObjects()
        {
            _frameCommands = _gd.ResourceFactory.CreateCommandList();
            _frameCommands.Name = "Frame Commands List";
            CommandList initCL = _gd.ResourceFactory.CreateCommandList();
            initCL.Name = "Recreation Initialization Command List";
            initCL.Begin();
            _sc.CreateDeviceObjects(_gd, initCL, _sc);
            CommonMaterials.CreateAllDeviceObjects(_gd, initCL, _sc);
            _scene.CreateAllDeviceObjects(_gd, initCL, _sc);
            initCL.End();
            _gd.SubmitCommands(initCL);
            initCL.Dispose();
        }

        private void DestroyAllObjects()
        {
            _gd.WaitForIdle();
            _frameCommands.Dispose();
            _sc.DestroyDeviceObjects();
            _scene.DestroyAllDeviceObjects();
            CommonMaterials.DestroyAllDeviceObjects();
            StaticResourceCache.DestroyAllDeviceObjects();
            _gd.WaitForIdle();
        }


    }
}
