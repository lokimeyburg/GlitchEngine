using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Linq;
using System.Text;
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
    public static class Program
    {
        private Sdl2Window _window;
        private GraphicsDevice _gd;
        private Scene _scene;
        private readonly ImGuiRenderable _igRenderable;
        private readonly GraphicsSystem _gs;
        private Game _game;
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

        public static Main(string[] args)
        {
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
                GraphicsBackend.OpenGL,
                //GraphicsBackend.OpenGLES,
                out _window,
                out _gd);
            _window.Resized += () => _windowResized = true;


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
                        string errorMessage = "Error: Multiple project manifests in this directory: " + currentDir;
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

            // Initialize Game()
            // --------------------------------------------------
            _game = new Game();

            // Assembly & Asset System
            // --------------------------------------------------
            AssemblyLoadSystem als = new AssemblyLoadSystem();
            als.LoadFromProjectManifest(projectManifest, AppContext.BaseDirectory);
            _game.SystemRegistry.Register(als);

            AssetSystem assetSystem = new AssetSystem(Path.Combine(AppContext.BaseDirectory, projectManifest.AssetRoot), als.Binder);
            _game.SystemRegistry.Register(assetSystem);

            // Graphics System
            // --------------------------------------------------
            _gs = new GraphicsSystem(_gd);
            _game.SystemRegistry.Register(_gs);

            // Scene
            // --------------------------------------------------
            _scene = new Scene(_gd, _window.Width, _window.Height);

            // [For Debugging] - Custom SceneAsset Serializer
            // --------------------------------------------------
            SceneAsset programaticSceneAsset = new SceneAsset();
            programaticSceneAsset.Name = "MainMenu";
            // Custom GameObject (for camera & skybox)
            GameObject go1 = new GameObject();
            go1.Name = "PlayerCamera";
            go1.Enabled = true;
            // Add custom camera to GameObject
            Camera camera = new Camera();
            camera.WindowHeight = _window.Height;
            camera.WindowWidth = _window.Width;
            go1.AddComponent(camera);
            go1.Transform.LocalPosition = new Vector3(0f, 0f, 0f);
            // Add custom skybox to GameObject
            Skybox skybox = Skybox.LoadDefaultSkybox(_game.SystemRegistry);
            go1.AddComponent(skybox);
            // Custom GameObject (for sphere mesh)
            GameObject go2 = new GameObject();
            go2.Name = "My Sphere";
            go2.Enabled = true;
            // Add custom sphere MeshRenderer component to GameObject
            Vector3 scale2 = new Vector3(1f);
            Vector3 offset2 = new Vector3(0f, 0f, -5f);
            Quaternion rotation2 = Quaternion.Identity;
            var meshAssetID2 = new AssetID("Internal:SphereModel");
            var meshAssetRef2 = new AssetRef<MeshData>(meshAssetID2);
            var textureAssetID2 = new AssetID("Textures/rust.jpg");
            var textureAssetRef2 = new AssetRef<ImageSharpTexture>(textureAssetID2);
            go2.Transform.LocalPosition = offset2;
            go2.Transform.LocalRotation = rotation2;
            go2.Transform.LocalScale = scale2;
            MeshRenderer meshrenderer2 = new MeshRenderer(meshAssetRef2, textureAssetRef2);
            go2.AddComponent(meshrenderer2);
            // Custom GameObject (for plane mesh)
            GameObject go3 = new GameObject();
            go3.Name = "My Plane Model";
            go3.Enabled = true;
            // Add custom Plane MeshRenderer component to GameObject
            Vector3 scale3 = new Vector3(10f);
            Vector3 offset3 = new Vector3(0f, -1f, -5f);
            Quaternion rotation3 = Quaternion.Identity;
            var meshAssetID3 = new AssetID("Internal:PlaneModel");
            var meshAssetRef3 = new AssetRef<MeshData>(meshAssetID3);
            var textureAssetID3 = new AssetID("Textures/Wood.png");
            var textureAssetRef3 = new AssetRef<ImageSharpTexture>(textureAssetID3);
            go3.Transform.LocalPosition = offset3;
            go3.Transform.LocalRotation = rotation3;
            go3.Transform.LocalScale = scale3;
            MeshRenderer meshrenderer3 = new MeshRenderer(meshAssetRef3, textureAssetRef3);
            go3.AddComponent(meshrenderer3);
            // Custom GameObject (another sphere mesh)
            GameObject go4 = new GameObject();
            go4.Name = "Another Sphere";
            go4.Enabled = true;
            Vector3 scale4 = new Vector3(0.5f);
            Vector3 offset4 = new Vector3(2f, -0.5f, -3f);
            Quaternion rotation4 = Quaternion.Identity;
            var meshAssetID4 = new AssetID("Internal:SphereModel");
            var meshAssetRef4 = new AssetRef<MeshData>(meshAssetID4);
            var textureAssetID4 = new AssetID("Textures/rust.jpg");
            var textureAssetRef4 = new AssetRef<ImageSharpTexture>(textureAssetID4);
            go4.Transform.LocalPosition = offset4;
            go4.Transform.LocalRotation = rotation4;
            go4.Transform.LocalScale = scale4;
            MeshRenderer meshrenderer4 = new MeshRenderer(meshAssetRef4, textureAssetRef4);
            go4.AddComponent(meshrenderer4);
            // Add custom GameObject to SceneAsset
            SerializedGameObject sgo1 = new SerializedGameObject(go1);
            SerializedGameObject sgo2 = new SerializedGameObject(go2);
            SerializedGameObject sgo3 = new SerializedGameObject(go3);
            SerializedGameObject sgo4 = new SerializedGameObject(go4);
            programaticSceneAsset.GameObjects = new SerializedGameObject[4];
            programaticSceneAsset.GameObjects[0] = sgo1;
            programaticSceneAsset.GameObjects[1] = sgo2;
            programaticSceneAsset.GameObjects[2] = sgo3;
            programaticSceneAsset.GameObjects[3] = sgo4;
            // Serialize SceneAsset
            LooseFileDatabase lfd = new LooseFileDatabase("/Assets");
            StringWriter stringwriter = new StringWriter(new StringBuilder());
            using (StreamWriter file = File.CreateText(@"DebugSceneAsset.json"))
            {
                JsonSerializer serializer = lfd.DefaultSerializer;
                serializer.Serialize(file, programaticSceneAsset);
            }

            // Scene Assets
            // --------------------------------------------------
            SceneAsset sceneAsset;
            AssetID mainSceneID = projectManifest.OpeningScene.ID;
            if (mainSceneID.IsEmpty)
            {
                var scenes = assetSystem.Database.GetAssetsOfType(typeof(SceneAsset));
                if (!scenes.Any())
                {
                    Console.WriteLine("No scenes were available to load.");
                    throw new System.Exception("No scenes were available to load.");
                }
                else
                {
                    mainSceneID = scenes.First();
                }
            }

            var readSceneFromProgramaticAsset = true;
            sceneAsset = assetSystem.Database.LoadAsset<SceneAsset>(mainSceneID);
            _scene.LoadSceneAsset(readSceneFromProgramaticAsset ? programaticSceneAsset : sceneAsset);
            _gs.SetCurrentScene(_scene);

            // GUI
            // --------------------------------------------------
            _igRenderable = new ImGuiRenderable(_window.Width, _window.Height);
            _resizeHandled += (w, h) => _igRenderable.WindowResized(w, h);
            _scene.AddRenderable(_igRenderable);
            _scene.AddUpdateable(_igRenderable);

            // Duplicate Screen (for post-processing filters)
            // --------------------------------------------------
            ScreenDuplicator duplicator = new ScreenDuplicator();
            _scene.AddRenderable(duplicator);

            // TODO: rename FullScreenQuad to FinalBufferObject or something
            _fsq = new FullScreenQuad();
            _scene.AddRenderable(_fsq);

            CreateAllObjects();
        }

        private void MakeObjectBigger()
        {
            Console.WriteLine("Pressed M");
            var GOQS = _game.SystemRegistry.GetSystem<GameObjectQuerySystem>();
            var GO = GOQS.FindByName("My Sphere");
            var scale = GO.Transform.Scale;
            GO.Transform.Scale = scale + new Vector3(0.1f);
        }

        private void MakeObjectSmaller()
        {
            Console.WriteLine("Pressed N");
            var GOQS = _game.SystemRegistry.GetSystem<GameObjectQuerySystem>();
            var GO = GOQS.FindByName("My Sphere");
            var scale = GO.Transform.Scale;
            GO.Transform.Scale = scale - new Vector3(0.1f);
        }

        private void MoveObjectRight()
        {
            Console.WriteLine("Pressed K");
            var GOQS = _game.SystemRegistry.GetSystem<GameObjectQuerySystem>();
            var GO = GOQS.FindByName("My Sphere");
            var scale = GO.Transform.Scale;
            GO.Transform.LocalPosition = GO.Transform.Position + new Vector3(0.1f, 0f, 0f);
        }

        private void MoveObjectLeft()
        {
            Console.WriteLine("Pressed J");
            var GOQS = _game.SystemRegistry.GetSystem<GameObjectQuerySystem>();
            var GO = GOQS.FindByName("My Sphere");
            var scale = GO.Transform.Scale;
            GO.Transform.LocalPosition = GO.Transform.Position + new Vector3(-0.1f, 0f, 0f);
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


            string openPopup = null;
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

                if (ImGui.MenuItem("Open Project"))
                {
                    openPopup = "OpenProjectPopup";
                }

                ImGui.Text(_fta.CurrentAverageFramesPerSecond.ToString("000.0 fps / ") + _fta.CurrentAverageFrameTimeMilliseconds.ToString("#00.00 ms"));

                ImGui.EndMainMenuBar();
            }



            if (openPopup != null)
            {
                ImGui.OpenPopup($"###{openPopup}");
            }

            if (ImGui.BeginPopup("###OpenProjectPopup"))
            {
                ImGui.Text("Path to project root:");
                // if (openPopup != null)
                // {
                //     ImGui.SetKeyboardFocusHere();
                // }
                // if (ImGui.InputText(string.Empty, _filenameInputBuffer.Buffer, _filenameInputBuffer.Length, InputTextFlags.EnterReturnsTrue, null))
                // {
                //     // LoadProject(_filenameInputBuffer.ToString());
                //     ImGui.CloseCurrentPopup();
                // }
                ImGui.SameLine();
                if (ImGui.Button("Open"))
                {
                    // LoadProject(_filenameInputBuffer.ToString());
                    ImGui.CloseCurrentPopup();
                }

                if (ImGui.Button("Close"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            if (InputTracker.GetKeyDown(Key.F11))
            {
                ToggleFullscreenState();
            }

            if (InputTracker.GetKeyDown(Key.M))
            {
                MakeObjectBigger();
            }

            if (InputTracker.GetKeyDown(Key.N))
            {
                MakeObjectSmaller();
            }

            if (InputTracker.GetKeyDown(Key.K))
            {
                MoveObjectRight();
            }

            if (InputTracker.GetKeyDown(Key.J))
            {
                MoveObjectLeft();
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
                _gs.RecreateWindowSizedResources(_gd, cl);
                cl.End();
                _gd.SubmitCommands(cl);
                cl.Dispose();
            }

            if (_newSampleCount != null)
            {
                _gs.MainSceneSampleCount = _newSampleCount.Value;
                _newSampleCount = null;
                DestroyAllObjects();
                CreateAllObjects();
            }

            _frameCommands.Begin();

            CommonMaterials.FlushAll(_frameCommands);

            _scene.RenderAllStages(_gd, _frameCommands, _gs);
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
            _gs.UpdateBackend(_gd);

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
            _gs.CreateDeviceObjects(_gd, initCL, _gs);
            CommonMaterials.CreateAllDeviceObjects(_gd, initCL, _gs);
            _scene.CreateAllDeviceObjects(_gd, initCL, _gs);
            initCL.End();
            _gd.SubmitCommands(initCL);
            initCL.Dispose();
        }

        private void DestroyAllObjects()
        {
            _gd.WaitForIdle();
            _frameCommands.Dispose();
            _gs.DestroyDeviceObjects();
            _scene.DestroyAllDeviceObjects();
            CommonMaterials.DestroyAllDeviceObjects();
            StaticResourceCache.DestroyAllDeviceObjects();
            _gd.WaitForIdle();
        }


    }
}
