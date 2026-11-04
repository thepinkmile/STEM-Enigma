using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Avalonia;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;
using CodeBreakers.Enigma;

namespace CodeBreakers.Visualization;

internal sealed class MachineScene : IDisposable
{
    private sealed class Part(ModelInstance instance, Location home, Vect explosion, Vect scale,
        Rotation rotation, int rotor, int key, int lamp)
    {
        public ModelInstance Instance = instance;
        public readonly Location Home = home;
        public readonly Vect Explosion = explosion;
        public readonly Vect Scale = scale;
        public readonly Rotation Rotation = rotation;
        public readonly int Rotor = rotor;
        public readonly int Key = key;
        public readonly int Lamp = lamp;
        public ColorVect Color;
        public bool Lettering;
        public bool ExplodedOnly;
        public bool ReflectorShell;
    }

    private static readonly ColorVect Wood = new(0.36f, 0.18f, 0.075f);
    private static readonly ColorVect WoodEdge = new(0.52f, 0.30f, 0.13f);
    private static readonly ColorVect Lining = new(0.045f, 0.037f, 0.025f);
    private static readonly ColorVect Metal = new(0.16f, 0.19f, 0.21f);
    private static readonly ColorVect Brass = new(0.78f, 0.56f, 0.23f);
    private static readonly ColorVect Ivory = new(0.86f, 0.85f, 0.71f);
    private static readonly ColorVect KeyBlack = new(0.022f, 0.025f, 0.024f);
    private static readonly ColorVect Nickel = new(0.68f, 0.65f, 0.52f);
    private static readonly PrimitivePaintbrush Forward = new(new ColorVect(1f, 0.63f, 0.12f));
    private static readonly PrimitivePaintbrush Return = new(new ColorVect(0.12f, 0.9f, 1f));
    private static readonly PrimitivePaintbrush Inactive = new(new ColorVect(0.16f, 0.25f, 0.32f));
    private static readonly string[] RotorNames = ["I", "II", "III"];
    private readonly string[] _captions = new string[3];
    private readonly LocalTinyFfrFactory _factory;
    private readonly Scene _scene;
    private readonly Camera _camera;
    private readonly InspectorCameraController _orbit;
    private readonly List<IDisposable> _owned = [];
    private readonly MachineLettering _lettering;
    private readonly Material _lampMaterial;
    private readonly Material _unlitLampMaterial;
    private readonly PointLight _lampLight;
    private readonly Dictionary<string, Mesh> _meshes = [];
    private readonly List<Part> _parts = [];
    private readonly Dictionary<ModelInstance, char> _keyModels = [];
    private readonly Dictionary<ModelInstance, char> _plugLetters = [];
    private readonly ModelInstance[] _plugModels = new ModelInstance[26];
    private readonly ModelInstance[] _rearPlugModels = new ModelInstance[26];
    private readonly ModelInstance[] _driveModels = new ModelInstance[7];
    private const int CableSegments = 32;
    private const float FlowSpacing = 2.25f;
    private const float FlowSpeed = 0.9f;
    private const float FlowRadius = 0.012f;
    private const float CableFlowRadius = 0.018f;
    private readonly ModelInstance[,] _plugCables = new ModelInstance[10, CableSegments];
    private readonly ModelInstance[,] _cablePlugs = new ModelInstance[10, 2];
    private IReadOnlyList<(int First, int Second)> _plugboardPairs = Array.Empty<(int, int)>();
    private readonly ScenePrimitive[,,] _wires = new ScenePrimitive[3, 26, 3];
    private readonly ScenePrimitive[,] _reflectorWires = new ScenePrimitive[13, 2];
    private readonly ModelInstance[] _reflectorPins = new ModelInstance[26];
    private static readonly Reflector ReflectorB = Reflector.EnigmaI.UKW_B();
    private readonly ScenePrimitive[] _path = new ScenePrimitive[144];
    private readonly ModelInstance[] _flowWires = new ModelInstance[144];
    private readonly ModelInstance[] _pickHits;
    private readonly Transform2D?[] _flowTextureTransforms = new Transform2D?[144];
    private readonly List<(Location Start, Location End, bool Returning, int Stage, bool Cable)> _flowSegments = [];
    private float _flowTime;
    private readonly ScenePrimitive[] _rotorLabels = new ScenePrimitive[3];
    private readonly List<(ScenePrimitive Primitive, Location Start, Location End, Vect Explosion)> _caseLetters = [];
    private readonly ScenePrimitive _reflectorLabel;
    private readonly float[] _angles = new float[3];
    private readonly float[] _from = new float[3];
    private readonly float[] _to = new float[3];
    private readonly int[] _rings = new int[3];
    private KeyStroke? _stroke;
    private float _elapsed = 10f;
    private float _explosion;
    private bool _disposed;
    public Renderer Renderer { get; }
    private bool _exploded;
    public bool Exploded
    {
        get => _exploded;
        set
        {
            if (_exploded == value) return;
            _exploded = value;
            _orbit.Target = new Location(0f, value ? 2f : 1.3f, 0f);
            _orbit.Distance = value ? 12f : 7.5f;
        }
    }
    public bool XRay { get; set; }
    public float CycleSeconds { get; set; } = 1.8f;
    public bool IsAnimating => _stroke != null && _elapsed < CycleSeconds;
    public event Action<char>? KeyPicked;
    public event Action<char>? PlugPicked;
    public int? SelectedPlug { get; set; }
    public event Action<char?, float>? KeyMotionChanged;

    public MachineScene()
    {
        _factory = new LocalTinyFfrFactory(factoryConfig: new LocalTinyFfrFactoryConfig { HeadlessMode = true });
        try
        {
            _scene = Own(_factory.SceneBuilder.CreateScene());
            _scene.SetBackdrop(new ColorVect(0.035f, 0.05f, 0.075f));
            _camera = Own(_factory.CameraBuilder.CreateCamera());
            _orbit = Own(_camera.CreateController<InspectorCameraController>());
            ResetCamera();
            _lettering = Own(new MachineLettering(_factory));
            var lensColor = Own(_factory.TextureBuilder.CreateColorMap(new ColorVect(0.55f, 0.49f, 0.32f), includeAlpha: false));
            var emission = Own(_factory.TextureBuilder.CreateEmissiveMap(new ColorVect(1f, 0.72f, 0.23f), intensity: 1f));
            _lampMaterial = Own(_factory.MaterialBuilder.CreateStandardMaterial(lensColor, emissiveMap: emission));
            var unlitColor = Own(_factory.TextureBuilder.CreateColorMap(Ivory, includeAlpha: false));
            _unlitLampMaterial = Own(_factory.MaterialBuilder.CreateLightingIgnoringMaterial(unlitColor));
            _lampLight = Own(_factory.LightBuilder.CreatePointLight(color: new ColorVect(1f, 0.72f, 0.23f),
                brightness: 0f, maxIlluminationRadius: 2f));
            _scene.Add(_lampLight);
            BuildAssembly();
            for (var r = 0; r < 3; r++)
            {
                _rotorLabels[r] = _scene.AddPrimitive();
                for (var i = 0; i < 26; i++)
                    for (var s = 0; s < 3; s++) _wires[r, i, s] = _scene.AddPrimitive();
            }
            for (var i = 0; i < _path.Length; i++) _path[i] = _scene.AddPrimitive();
            for (var wire = 0; wire < 13; wire++)
                for (var segment = 0; segment < 2; segment++)
                    _reflectorWires[wire, segment] = _scene.AddPrimitive();
            BuildFlowWires();
            _pickHits = new ModelInstance[_parts.Count + _flowWires.Length + _plugCables.Length + _cablePlugs.Length];
            _reflectorLabel = _scene.AddPrimitive();
            Renderer = Own(_factory.RendererBuilder.CreateBindableRenderer(_scene, _camera, _factory.ResourceAllocator));
            Renderer.SetQuality(new RenderQualityConfig(BuiltInQualityConfiguration.Medium) { BloomStrength = 0.35f });
            Reset("AAA", "AAA");
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public IDisposable Start(TinyFfrSceneView view, Action<float> tick) =>
        _factory.ApplicationLoopBuilder.StartAvaloniaUiLoop(view, (time, input) =>
        {
            var dt = Math.Clamp((float)time.TotalSeconds, 0f, 0.1f);
            var mouse = input.KeyboardAndMouse;
            var shift = mouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.LeftShift) ||
                mouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.RightShift);
            if (shift && (mouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.MouseRight) ||
                mouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.MouseLeft)))
                PanCamera(mouse.MouseCursorDelta.X, mouse.MouseCursorDelta.Y);
            else if (mouse.KeyIsCurrentlyDown(KeyboardOrMouseKey.MouseRight))
            {
                _orbit.AdjustYawViaMouseCursor(mouse);
                _orbit.AdjustPitchViaMouseCursor(mouse);
            }
            _orbit.AdjustDistancePercentageViaMouseWheel(mouse);
            if (!shift && mouse.KeyWasPressedThisIteration(KeyboardOrMouseKey.MouseLeft))
            {
                var hit = PickModel(Renderer.CreateRayFromRenderSurface(mouse.MouseCursorPosition));
                if (hit is { } pick && _keyModels.TryGetValue(pick, out var letter))
                    KeyPicked?.Invoke(letter);
                else if (hit is { } plug && _plugLetters.TryGetValue(plug, out var plugLetter))
                    PlugPicked?.Invoke(plugLetter);
            }
            tick(dt);
            Update(dt);
            _orbit.Progress(dt);
            ApplyFlowTextureTransforms();
            Renderer.Render();
        }, tickRateHz: 60);

    private void PanCamera(float deltaX, float deltaY)
    {
        var scale = _orbit.Distance * 0.0015f;
        _orbit.Target += _camera.GetRelativeOrientationDirection(Orientation.Right) * (-deltaX * scale) +
            _camera.UpDirection * (deltaY * scale);
    }

    public void VerifyCameraPan()
    {
        var target = _orbit.Target;
        var yaw = _orbit.Yaw;
        var pitch = _orbit.Pitch;
        var distance = _orbit.Distance;
        PanCamera(60f, 40f);
        if ((_orbit.Target - target).Length < 0.1f ||
            _orbit.Yaw != yaw || _orbit.Pitch != pitch || _orbit.Distance != distance)
            throw new InvalidOperationException("Camera panning must translate the target without orbiting or zooming.");
        PanCamera(-60f, -40f);
        if ((_orbit.Target - target).Length > 0.001f)
            throw new InvalidOperationException("Camera panning did not reverse.");
        _orbit.Target = target;
    }

    public void ResetCamera()
    {
        _orbit.Target = new Location(0f, Exploded ? 2f : 1.3f, 0f);
        _orbit.MinDistance = 3f;
        _orbit.MaxDistance = 24f;
        _orbit.Distance = Exploded ? 12f : 7.5f;
        _orbit.Yaw = Angle.FromDegrees(-75f);
        _orbit.Pitch = Angle.FromDegrees(33f);
        SnapCamera();
    }

    public void Inspect(string part)
    {
        switch (part)
        {
            case "Keyboard / lamps":
                _orbit.Target = new Location(0f, 0.8f + _explosion * 0.7f, -0.5f - _explosion * 0.6f);
                _orbit.Distance = 5.5f;
                _orbit.Yaw = Angle.FromDegrees(-90f);
                _orbit.Pitch = Angle.FromDegrees(65f);
                break;
            case "Rotor lettering":
                _orbit.Target = new Location(0f, 1.35f + _explosion * 1.2f, 1.8f + _explosion * 0.8f);
                _orbit.Distance = Exploded ? 7f : 4.5f;
                _orbit.Yaw = Angle.FromDegrees(-65f);
                _orbit.Pitch = Angle.FromDegrees(45f);
                break;
            case "Plugboard":
                _orbit.Target = new Location(0f, 0.2f + _explosion * 0.2f, -3.05f - _explosion * 1.5f);
                _orbit.Distance = 5f;
                _orbit.Yaw = Angle.FromDegrees(-90f);
                _orbit.Pitch = Angle.FromDegrees(5f);
                break;
            case "Reflector":
                _orbit.Target = new Location(1.92f + 1.5f * _explosion, 1.35f + 1.2f * _explosion, 1.8f + 0.8f * _explosion);
                _orbit.Distance = 3.4f;
                _orbit.Yaw = Angle.FromDegrees(-155f);
                _orbit.Pitch = Angle.FromDegrees(22f);
                break;
            case "Stepping arm":
                _orbit.Target = new Location(RotorX(2, _explosion) - 0.3f,
                    0.95f + 1.2f * _explosion, 1.95f + 0.8f * _explosion);
                _orbit.Distance = 3.4f;
                _orbit.Yaw = Angle.FromDegrees(-10f);
                _orbit.Pitch = Angle.FromDegrees(15f);
                break;
            case "Plugboard back":
                _orbit.Target = RearPlugPosition(19, _explosion) + new Vect(0f, -0.1f, 0f);
                _orbit.Distance = 5f;
                _orbit.Yaw = Angle.FromDegrees(90f);
                _orbit.Pitch = Angle.FromDegrees(-8f);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(part), part, "Unknown inspection target.");
        }
        SnapCamera();
    }

    private void SnapCamera()
    {
        var distanceSmoothing = _orbit.DistanceSmoothingStrength;
        var rotationSmoothing = _orbit.RotationSmoothingStrength;
        _orbit.SetGlobalSmoothing(SmoothingStrength.None);
        _orbit.Progress(1f);
        _orbit.DistanceSmoothingStrength = distanceSmoothing;
        _orbit.RotationSmoothingStrength = rotationSmoothing;
    }

    public void Reset(string positions, string rings, IReadOnlyList<(int First, int Second)>? plugboardPairs = null)
    {
        _plugboardPairs = plugboardPairs ?? Array.Empty<(int, int)>();
        _stroke = null;
        _elapsed = 10f;
        for (var r = 0; r < 3; r++)
        {
            _angles[r] = _from[r] = _to[r] = positions[r] - 'A';
            _rings[r] = rings[r] - 'A';
            _captions[r] = $"{RotorNames[r]}  {positions[r]}";
        }
        Update(0f);
    }

    public void Play(KeyStroke stroke)
    {
        _stroke = stroke;
        _elapsed = 0f;
        for (var r = 0; r < 3; r++)
        {
            _from[r] = stroke.Before[r] - 'A';
            _to[r] = _from[r] + LabMachine.Mod(stroke.After[r] - stroke.Before[r]);
            _captions[r] = $"{RotorNames[r]}  {stroke.After[r]}";
        }
    }

    public void VerifyReflectorView()
    {
        Update(0f);
        if (_parts.Count(p => p.ReflectorShell) != 3)
            throw new InvalidOperationException("The reflector housing and two face plates must support the cutaway.");
        for (var letter = 0; letter < 26; letter++)
        {
            var tip = _reflectorPins[letter].Position + new Vect(-0.09f, 0f, 0f);
            if ((tip - ReflectorContact(letter)).Length > 0.001f)
                throw new InvalidOperationException("The reflector signal is not attached to its brass contact pin.");
            var output = ReflectorB.Reflect(letter);
            if (output == letter || ReflectorB.Reflect(output) != letter)
                throw new InvalidOperationException("Reflector connections must form 13 distinct symmetric pairs.");
        }
    }

    public void VerifyInteraction()
    {
        VerifyReflectorView();
        VerifyRotorDrive();
        if (!Exploded && _flowTextureTransforms.Any(transform => transform != null))
            throw new InvalidOperationException("Signal textures must be hidden in the assembled view.");
        var frontLampEdge = Enumerable.Range(0, 26).Min(i => LampPosition(i, 0f).Z) - 0.145f;
        var rearKeyEdge = Enumerable.Range(0, 26).Max(i => KeyPosition(i, 0f).Z) + 0.195f;
        var rearLampEdge = Enumerable.Range(0, 26).Max(i => LampPosition(i, 0f).Z) + 0.145f;
        var frontRotorEdge = _parts.Where(p => p.Rotor >= 0 && !p.Lettering)
            .Min(p => p.Home.Z - p.Scale.Z);
        if (frontLampEdge - rearKeyEdge < 0.5f || frontRotorEdge - rearLampEdge < 0.2f)
            throw new InvalidOperationException("The assembled lampboard needs clear gaps to the keyboard and rotors.");
        if (_stroke == null && _lampLight.Brightness != 0f)
            throw new InvalidOperationException("The output bulb remained lit after reset.");
        for (var cable = 0; cable < 10; cable++)
            for (var end = 0; end < 2; end++)
            {
                var position = _cablePlugs[cable, end].Position;
                if (cable >= _plugboardPairs.Count)
                {
                    if (position.Y != -100f) throw new InvalidOperationException("An unconfigured plugboard cable is visible.");
                    continue;
                }
                var pair = _plugboardPairs[cable];
                var expected = PlugPosition(end == 0 ? pair.First : pair.Second, _explosion) + new Vect(0f, -0.02f, -0.07f);
                if ((position - expected).Length > 0.001f)
                    throw new InvalidOperationException("A cable plug is not attached to its configured letter.");
            }
        if (_parts.Count(p => p.Lettering && p.Key >= 0) != 26 ||
            _parts.Count(p => p.Lettering && p.Rotor >= 0) != 78 ||
            _parts.Count(p => p.Lettering && p.Key < 0 && p.Rotor < 0) != 78)
            throw new InvalidOperationException("Font labels are missing from the keys, rotors, lamps or plugboard.");
        var pickedKeys = new HashSet<char>();
        foreach (var (model, letter) in _keyModels)
        {
            var pixel = Renderer.ProjectOnToRenderSurfacePixels(model.Position + new Vect(0f, 0.07f, 0f));
            if (pixel is not { } coordinate) continue;
            var hit = PickModel(Renderer.CreateRayFromRenderSurface(coordinate));
            if (hit is { } pick && _keyModels.TryGetValue(pick, out var actual) && actual == letter)
                pickedKeys.Add(letter);
        }
        if (pickedKeys.Count != 26) throw new InvalidOperationException($"Only {pickedKeys.Count}/26 3D keys could be picked.");
        if (_stroke != null)
        {
            var elapsed = _elapsed;
            foreach (var progress in new[] { 0f, 0.2f, 0.9f, 1f })
            {
                _elapsed = CycleSeconds * progress;
                Update(0f);
                VerifyRotorDrive();
                if (MathF.Abs(_lampLight.Brightness - (progress >= 0.92f ? 0.08f : 0f)) > 0.001f)
                    throw new InvalidOperationException("The output bulb light did not follow the signal timing.");
                if (progress >= 0.92f &&
                    (_lampLight.Position - (LampPosition(_stroke.Output - 'A', _explosion) + new Vect(0f, 0.18f, 0f))).Length > 0.001f)
                    throw new InvalidOperationException("The output bulb light did not follow the lamp assembly.");
                foreach (var part in _parts.Where(p => p.Key >= 0))
                {
                    var expected = part.Home + part.Explosion * _explosion;
                    if (part.Key == _stroke.Input - 'A' && progress == 0.2f)
                        expected += new Vect(0f, -0.16f, 0f);
                    if (MathF.Abs(part.Instance.Position.Y - expected.Y) > 0.001f)
                        throw new InvalidOperationException("Key cap, rim and stem did not complete their press/release travel.");
                }
            }
            _elapsed = elapsed;
            Update(0f);
        }
        foreach (var part in _parts.Where(p => p.Rotor >= 0 && p.Scale.X > 1f))
        {
            var relative = new Vect(0f, 0.63f, 0f) * part.Instance.Rotation;
            var contact = Contact(part.Rotor, 0, true);
            if (MathF.Abs(part.Instance.Position.Y + relative.Y - contact.Y) > 0.001f ||
                MathF.Abs(part.Instance.Position.Z + relative.Z - contact.Z) > 0.001f)
                throw new InvalidOperationException("The rotating mesh and electrical contacts are misaligned.");
        }
    }

    public void VerifyPlugPicking()
    {
        for (var letter = 0; letter < 26; letter++)
        {
            var pixel = Renderer.ProjectOnToRenderSurfacePixels(PlugPosition(letter, _explosion));
            if (pixel is not { } coordinate)
                throw new InvalidOperationException("A plugboard socket is outside the inspection view.");
            var hit = PickModel(Renderer.CreateRayFromRenderSurface(coordinate));
            if (hit is not { } model || !_plugLetters.TryGetValue(model, out var actual) || actual != 'A' + letter)
                throw new InvalidOperationException($"Plugboard socket {(char)('A' + letter)} could not be picked.");
        }
    }

    private ModelInstance? PickModel(Ray ray)
    {
        var count = _scene.QueryProvider.FindIntersections(ray, _pickHits);
        for (var i = 0; i < count; i++)
            if (!Array.Exists(_flowWires, wire => wire == _pickHits[i]))
                return _pickHits[i];
        return null;
    }

    public void VerifyCableFlow()
    {
        VerifyFlowVelocity();
        var machine = new LabMachine();
        machine.Configure("AAA", "AAA", string.Join(" ", _plugboardPairs.Select(p => $"{(char)('A' + p.First)}{(char)('A' + p.Second)}")));
        var exploded = Exploded;
        Exploded = true;
        foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            Play(machine.Press(letter));
            _elapsed = CycleSeconds;
            Update(0f);
            for (var i = 1; i < _flowSegments.Count; i++)
                if ((_flowSegments[i].Start - _flowSegments[i - 1].End).Length > 0.001f)
                    throw new InvalidOperationException("The signal texture route must be ordered continuously from key to lamp.");
            foreach (var hop in new[] { _stroke!.Hops[0], _stroke.Hops[8] })
            {
                var rear = RearPlugPosition(hop.Input, _explosion);
                var front = PlugPosition(hop.Input, _explosion);
                if (!_flowSegments.Any(s => s.Returning == hop.Returning &&
                    (s.Start - rear).Length < 0.001f && (s.End - front).Length < 0.001f))
                    throw new InvalidOperationException("The plugboard input must enter from its rear terminal.");
            }
            foreach (var hop in _stroke!.Hops.Where(h => h.Stage.StartsWith("Plug", StringComparison.OrdinalIgnoreCase) && h.Input != h.Output))
            {
                var cable = Enumerable.Range(0, _plugboardPairs.Count)
                    .Single(i => _plugboardPairs[i].First == hop.Input || _plugboardPairs[i].Second == hop.Input);
                var reverse = _plugboardPairs[cable].First != hop.Input;
                var start = CablePoint(cable, reverse ? 1f : 0f);
                var end = CablePoint(cable, reverse ? 1f - 1f / CableSegments : 1f / CableSegments);
                if (!_flowSegments.Any(s => s.Returning == hop.Returning &&
                    (s.Start - start).Length < 0.001f && (s.End - end).Length < 0.001f))
                    throw new InvalidOperationException("The plugboard trace does not follow the cable in signal order.");
            }
        }
        Exploded = exploded;
    }

    private static Transform2D FlowTextureTransform(float length, float offset, float time) =>
        new(scaling: (FlowSpacing / length, 1f),
            translation: ((time * FlowSpeed - offset) / FlowSpacing % 1f, 0f));

    private static void VerifyFlowVelocity()
    {
        foreach (var length in new[] { 0.03f, 0.18f, 1.2f, 4.5f })
        foreach (var offset in new[] { 0f, 0.4f, 3f, 12f })
        foreach (var time in new[] { 0.8f, 2.45f, 8f })
        {
            const float dt = 0.1f;
            var start = FlowTextureTransform(length, offset, time);
            var end = FlowTextureTransform(length, offset, time + dt);
            // TinyFFR applies UV / scale - translation, not (UV - translation) / scale.
            var phaseChange = end.Translation.X - start.Translation.X;
            phaseChange -= MathF.Round(phaseChange);
            var travel = phaseChange * start.Scaling.X * length;
            if (MathF.Abs(travel / dt - FlowSpeed) > 0.0001f)
                throw new InvalidOperationException("The wire texture must scroll at a constant world-space speed.");
            var adjacent = FlowTextureTransform(length * 2f, offset + length, time);
            var endPhase = 1f / start.Scaling.X - start.Translation.X;
            var nextPhase = -adjacent.Translation.X;
            if (MathF.Abs(endPhase - nextPhase - MathF.Round(endPhase - nextPhase)) > 0.0001f)
                throw new InvalidOperationException("The pulse phase must be continuous between adjacent wire segments.");
        }
        if (CableFlowRadius >= 0.1f * 0.26f)
            throw new InvalidOperationException("The signal core must fit inside the physical cable sleeve.");
    }

    private T Own<T>(T resource) where T : IDisposable
    {
        _owned.Add(resource);
        return resource;
    }

    private Mesh Mesh(string name)
    {
        if (!_meshes.TryGetValue(name, out var mesh))
        {
            mesh = Own(_factory.AssetLoader.LoadMesh(Path.Combine(AppContext.BaseDirectory, "Assets", "Models", name + ".obj")));
            _meshes.Add(name, mesh);
        }
        return mesh;
    }

    private ModelInstance Add(string mesh, Location home, Vect scale, ColorVect color, Vect explosion = default,
        int rotor = -1, int key = -1, int lamp = -1, Rotation rotation = default)
    {
        var instance = Own(_factory.ObjectBuilder.CreateModelInstance(Mesh(mesh),
            initialPosition: home, initialScaling: scale, initialRotation: rotation));
        instance.SetDefaultMaterialBaseColor(color);
        _scene.Add(instance);
        _parts.Add(new(instance, home, explosion, scale, rotation, rotor, key, lamp) { Color = color });
        if (key >= 0) _keyModels.Add(instance, (char)('A' + key));
        return instance;
    }

    private void BuildAssembly()
    {
        BuildCase();
        Add("keyboard-plate", new(0f, 0.42f, -1.57f), new(4.9f, 0.14f, 1.65f), KeyBlack, new(0f, 0.5f, -1f));
        Add("lamp-panel", new(0f, 0.65f, 0.24f), new(4.9f, 0.12f, 1.12f), new ColorVect(0.46f, 0.37f, 0.22f), new(0f, 1.1f, -0.3f));
        Add("plugboard", new(0f, 0.22f, -2.89f), new(4.9f, 0.95f, 0.12f), KeyBlack, new(0f, 0.2f, -1.5f));
        var cableColor = Own(_factory.TextureBuilder.CreateColorMap(new ColorVect(0.18f, 0.16f, 0.12f, 0.22f), includeAlpha: true));
        var cableMaterial = Own(_factory.MaterialBuilder.CreateStandardMaterial(cableColor,
            alphaMode: StandardMaterialAlphaMode.FullBlending));
        for (var cable = 0; cable < 10; cable++)
        {
            ModelInstance CablePart(string mesh)
            {
                var instance = Own(_factory.ObjectBuilder.CreateModelInstance(Mesh(mesh),
                    material: mesh == "shaft" ? cableMaterial : null, initialPosition: new Location(0f, -100f, 0f)));
                if (mesh != "shaft") instance.SetDefaultMaterialBaseColor(new ColorVect(0.055f, 0.05f, 0.037f));
                _scene.Add(instance);
                return instance;
            }
            for (var segment = 0; segment < CableSegments; segment++)
                _plugCables[cable, segment] = CablePart("shaft");
            for (var end = 0; end < 2; end++)
                _cablePlugs[cable, end] = CablePart("key");
        }
        Add("shaft", new(0f, 1.35f, 1.8f), new(4.5f, 1f, 1f), Nickel, new(0f, 0.5f, 1.2f));
        BuildReflector();
        for (var r = 0; r < 3; r++)
        {
            var x = RotorX(r, 0f);
            var offset = new Vect(-(r - 1) * 1.35f, 1.2f, 0.8f);
            Add("rotor", new(x, 1.35f, 1.8f), new(1.7f, 0.7f, 0.7f), KeyBlack, offset, rotor: r);
            Add("gear", new(x + 0.32f, 1.35f, 1.8f), new(1f, 0.8f, 0.8f), Brass, offset, rotor: r);
            Add("rotor", new(x, 1.35f, 1.8f), new(0.7f, 0.715f, 0.715f), Nickel, offset, rotor: r);
            Add("gear", new(x - 0.27f, 1.35f, 1.8f), new(0.4f, 0.72f, 0.72f), Nickel, offset, rotor: r);
            for (var letter = 0; letter < 26; letter++)
            {
                var angle = letter * MathF.Tau / 26f;
                var rotation = new Rotation(letter * 360f / 26f, new Direction(1f, 0f, 0f));
                var mesh = _lettering.CreateFace(0.19f, 0.165f,
                    new Vect(0f, MathF.Cos(angle) * 0.737f, MathF.Sin(angle) * 0.737f), rotation);
                AddLetter(mesh, letter, false, new(x, 1.35f, 1.8f), offset, rotor: r);
            }
        }
        BuildRotorDrive();
        for (var i = 0; i < 26; i++)
        {
            var home = KeyPosition(i, 0f);
            Add("shaft", home + new Vect(0f, -0.16f, 0f), new(0.3f, 0.48f, 0.48f), Nickel,
                new(0f, 0.5f, -1f), key: i, rotation: new Rotation(90f, Direction.Forward));
            Add("key", home, new(0.39f, 0.3f, 0.39f), Nickel,
                new(0f, 0.5f, -1f), key: i);
            Add("key", home + new Vect(0f, 0.018f, 0f), new(0.335f, 0.3f, 0.335f), KeyBlack,
                new(0f, 0.5f, -1f), key: i);
            Add("contact", LampPosition(i, 0f) + new Vect(0f, -0.025f, 0f), new(0.29f, 0.7f, 0.29f), Brass,
                new(0f, 1.1f, -0.3f));
            var lens = Add("lamp", LampPosition(i, 0f) + new Vect(0f, 0.025f, 0f), new(0.21f, 0.3f, 0.21f), Ivory,
                new(0f, 1.1f, -0.3f), lamp: i);
            lens.SetMaterial(_unlitLampMaterial);
            _plugModels[i] = Add("contact", PlugPosition(i, 0f),
                new(0.13f, 0.25f, 0.13f), Brass, new(0f, 0.2f, -1.5f),
                rotation: new Rotation(90f, Direction.Right));
            _plugLetters.Add(_plugModels[i], (char)('A' + i));
            AddLetter(_lettering.CreateFace(0.29f, 0.29f, new(0f, 0.061f, 0f)), i, true,
                home, new(0f, 0.5f, -1f), key: i);
            AddLetter(_lettering.CreateFace(0.19f, 0.19f, new(0f, 0.067f, 0f)), i, false,
                LampPosition(i, 0f), new(0f, 1.1f, -0.3f));
            AddLetter(_lettering.CreateFace(0.18f, 0.16f, new(0f, 0.105f, -0.04f),
                new Rotation(90f, Direction.Right)), i, true,
                PlugPosition(i, 0f), new(0f, 0.2f, -1.5f));
            _plugLetters.Add(_parts[^1].Instance, (char)('A' + i));
            _rearPlugModels[i] = Add("contact", RearPlugPosition(i, 0f),
                new(0.13f, 0.25f, 0.13f), Brass, new(0f, 0.2f, -1.5f),
                rotation: new Rotation(90f, Direction.Right));
            _parts[^1].ExplodedOnly = true;
            _plugLetters.Add(_rearPlugModels[i], (char)('A' + i));
            AddLetter(_lettering.CreateFace(0.18f, 0.16f, new(0f, 0.105f, 0.04f),
                new Rotation(180f, Direction.Up) + new Rotation(-90f, Direction.Right)), i, true,
                RearPlugPosition(i, 0f), new(0f, 0.2f, -1.5f));
            _parts[^1].ExplodedOnly = true;
            _plugLetters.Add(_parts[^1].Instance, (char)('A' + i));
        }
    }

    private void BuildRotorDrive()
    {
        var offset = new Vect(-1.35f, 1.2f, 0.8f);
        for (var i = 0; i < _driveModels.Length; i++)
            _driveModels[i] = Add(i is 1 or 3 or 6 ? "shaft" : "case-base",
                new(RotorX(2, 0f) - 0.34f, 0.4f, 0.7f), new(0.1f, 0.1f, 0.1f),
                i is 2 or 5 ? Brass : Nickel, offset);
    }

    private static (Location Fulcrum, Location Rear, Location Tip) RotorDrivePose(
        float progress, float advance, float explosion)
    {
        var origin = new Location(RotorX(2, explosion) - 0.34f, 1.2f * explosion, 0.8f * explosion);
        var fulcrum = origin + new Vect(0f, 0.4f, 1.7f);
        var release = Math.Clamp((progress - 0.68f) / 0.22f, 0f, 1f);
        release = release * release * (3f - 2f * release);
        // Follow a tooth during the drive, then lift clear while returning to the next tooth.
        var angle = (9f - advance * (1f - release)) * MathF.Tau / 26f;
        var radius = 0.705f + 0.12f * MathF.Sin(release * MathF.PI);
        var tip = origin + new Vect(0f, 1.35f + radius * MathF.Cos(angle),
            1.8f + radius * MathF.Sin(angle));
        // Intersect the rocker and pawl circles so both metal links remain rigid.
        var dy = tip.Y - fulcrum.Y;
        var dz = tip.Z - fulcrum.Z;
        var distance = MathF.Sqrt(dy * dy + dz * dz);
        const float rockerLength = 0.8f;
        const float pawlLength = 0.65f;
        if (distance <= rockerLength - pawlLength || distance >= rockerLength + pawlLength)
            throw new InvalidOperationException("The stepping linkage cannot reach the rotor tooth.");
        var along = (rockerLength * rockerLength - pawlLength * pawlLength + distance * distance) / (2f * distance);
        var height = MathF.Sqrt(rockerLength * rockerLength - along * along);
        var rear = fulcrum + new Vect(0f, (along * dy - height * dz) / distance,
            (along * dz + height * dy) / distance);
        return (fulcrum, rear, tip);
    }

    private void UpdateRotorDrive()
    {
        var progress = _stroke == null ? 1f : _elapsed / CycleSeconds;
        var advance = _stroke == null ? 0f : _angles[2] - _from[2];
        var (fulcrum, rear, tip) = RotorDrivePose(progress, advance, _explosion);
        void Beam(int index, Location start, Location end, float width, float depth)
        {
            var delta = end - start;
            _driveModels[index].SetTransform(new Transform(translation: (start + delta * 0.5f).AsVect(),
                rotation: new Direction(1f, 0f, 0f) >> delta.Direction,
                scaling: new Vect(delta.Length, width, depth)));
        }
        Beam(0, fulcrum + new Vect(0f, -0.22f, 0f), fulcrum, 0.22f, 0.25f);
        Beam(1, fulcrum + new Vect(-0.1f, 0f, 0f), fulcrum + new Vect(0.1f, 0f, 0f), 0.65f, 0.65f);
        Beam(2, fulcrum + (fulcrum - rear) * 0.85f, rear, 0.09f, 0.12f);
        Beam(3, rear + new Vect(-0.08f, 0f, 0f), rear + new Vect(0.08f, 0f, 0f), 0.5f, 0.5f);
        Beam(4, rear, tip + new Vect(-0.055f, 0f, 0f), 0.085f, 0.085f);
        Beam(5, tip + new Vect(-0.075f, 0f, 0f), tip + new Vect(0.07f, 0f, 0f), 0.055f, 0.07f);
        Beam(6, tip + new Vect(-0.11f, 0f, 0f), tip + new Vect(-0.055f, 0f, 0f), 0.4f, 0.4f);
    }

    private void VerifyRotorDrive()
    {
        foreach (var explosion in new[] { 0f, 1f })
        {
            var rest = RotorDrivePose(1f, 0f, explosion);
            var pressed = RotorDrivePose(0.22f, 1f, explosion);
            var returned = RotorDrivePose(1f, 1f, explosion);
            var center = new Location(RotorX(2, explosion) - 0.34f,
                1.35f + 1.2f * explosion, 1.8f + 0.8f * explosion);
            if ((rest.Tip - returned.Tip).Length > 0.001f ||
                pressed.Rear.Y - rest.Rear.Y < 0.12f ||
                MathF.Abs((pressed.Tip - center).Length - 0.705f) > 0.001f ||
                MathF.Abs((rest.Tip - pressed.Tip).Length - 2f * 0.705f * MathF.Sin(MathF.PI / 26f)) > 0.001f)
                throw new InvalidOperationException("The stepping arm must lift, drive one tooth and return to rest.");
            var lifted = RotorDrivePose(0.79f, 1f, explosion);
            if ((lifted.Tip - center).Length < 0.8f)
                throw new InvalidOperationException("The returning pawl must lift clear of the rotor teeth.");
            foreach (var sample in new[] { rest, pressed, lifted, returned })
                if (MathF.Abs((sample.Rear - sample.Fulcrum).Length - 0.8f) > 0.001f ||
                    MathF.Abs((sample.Tip - sample.Rear).Length - 0.65f) > 0.001f)
                    throw new InvalidOperationException("The stepping arm links must rotate without stretching.");
        }
        var progress = _stroke == null ? 1f : _elapsed / CycleSeconds;
        var advance = _stroke == null ? 0f : _angles[2] - _from[2];
        var pose = RotorDrivePose(progress, advance, _explosion);
        if ((_driveModels[3].Position - pose.Rear).Length > 0.001f ||
            (_driveModels[5].Position - (pose.Tip + new Vect(-0.0025f, 0f, 0f))).Length > 0.001f)
            throw new InvalidOperationException("The animated stepping linkage is not attached to its pivots.");
    }

    public void VerifyRotorDriveCycle(Action<string> capture)
    {
        if (_stroke == null || LabMachine.Mod(_stroke.After[2] - _stroke.Before[2]) != 1)
            throw new InvalidOperationException("Every key press must drive the fast rotor by one tooth.");
        var elapsed = _elapsed;
        try
        {
            foreach (var progress in new[] { 0f, 0.1f, 0.22f, 0.79f, 1f })
            {
                _elapsed = CycleSeconds * progress;
                Update(0f);
                VerifyRotorDrive();
                ApplyFlowTextureTransforms();
                Renderer.Render();
                if (progress == 0f) capture("stepping-arm-rest");
                if (progress == 0.22f) capture("stepping-arm-drive");
            }
        }
        finally
        {
            _elapsed = elapsed;
            Update(0f);
        }
    }

    private void BuildReflector()
    {
        var offset = new Vect(1.5f, 1.2f, 0.8f);
        var silver = new ColorVect(0.66f, 0.69f, 0.70f);
        void Shell(string mesh, Location home, Vect scale, ColorVect color)
        {
            Add(mesh, home, scale, color, offset);
            _parts[^1].ReflectorShell = true;
        }
        Shell("reflector", new(1.92f, 1.35f, 1.8f), new(1f, 1f, 1f), silver);
        Shell("shaft", new(1.69f, 1.35f, 1.8f), new(0.06f, 7.3f, 7.3f), KeyBlack);
        Shell("shaft", new(2.15f, 1.35f, 1.8f), new(0.045f, 7.3f, 7.3f), silver);
        Add("shaft", new(1.64f, 1.35f, 1.8f), new(0.07f, 2.7f, 2.7f), Metal, offset);
        Add("shaft", new(1.59f, 1.35f, 1.8f), new(0.08f, 1.2f, 1.2f), Brass, offset);
        Add("case-base", new(1.52f, 1.35f, 1.8f), new(0.12f, 0.16f, 0.16f), silver, offset);
        for (var letter = 0; letter < 26; letter++)
        {
            var angle = letter * MathF.Tau / 26f;
            var radial = new Vect(0f, MathF.Cos(angle) * 0.63f, MathF.Sin(angle) * 0.63f);
            _reflectorPins[letter] = Add("shaft", new Location(1.57f, 1.35f, 1.8f) + radial,
                new(0.18f, 0.29f, 0.29f), Brass, offset);
        }
        for (var screw = 0; screw < 3; screw++)
        {
            var angle = (screw / 3f + 0.08f) * MathF.Tau;
            var radial = new Vect(0f, MathF.Cos(angle) * 0.43f, MathF.Sin(angle) * 0.43f);
            Add("shaft", new Location(1.647f, 1.35f, 1.8f) + radial,
                new(0.025f, 0.72f, 0.72f), new ColorVect(0.43f, 0.16f, 0.035f), offset);
            Add("shaft", new Location(1.629f, 1.35f, 1.8f) + radial,
                new(0.018f, 0.44f, 0.44f), silver, offset);
            Add("case-base", new Location(1.617f, 1.35f, 1.8f) + radial,
                new(0.008f, 0.055f, 0.009f), KeyBlack, offset);
        }
        // A radial retaining handle and its rounded end sit outside the contact face.
        Add("shaft", new(1.94f, 1.35f, 0.96f), new(0.38f, 0.65f, 0.65f), silver, offset,
            rotation: new Rotation(90f, Direction.Up));
        Add("shaft", new(1.94f, 1.35f, 0.78f), new(0.25f, 0.8f, 0.8f), silver, offset);
    }

    private void AddLetter(Mesh mesh, int letter, bool dark, Location home, Vect explosion, int rotor = -1, int key = -1)
    {
        var instance = Own(_factory.ObjectBuilder.CreateModelInstance(mesh, _lettering.Material((char)('A' + letter), dark),
            initialPosition: home));
        _scene.Add(instance);
        _parts.Add(new(instance, home, explosion, new Vect(1f, 1f, 1f), default, rotor, key, -1) { Lettering = true });
        if (key >= 0) _keyModels.Add(instance, (char)('A' + key));
    }

    private void BuildCase()
    {
        Add("case-base", new(0f, -0.65f, 0f), new(5.5f, 0.24f, 5.6f), Wood);
        foreach (var side in new[] { -1f, 1f })
        {
            var outward = new Vect(side, 0f, 0f);
            Add("case-wall", new(side * 2.64f, -0.04f, 0f), new(0.22f, 1.38f, 5.6f), Wood, outward);
            Add("case-wall", new(side * 2.64f, 0.65f, 0f), new(0.25f, 0.09f, 5.6f), WoodEdge, outward);
            Add("case-wall", new(side * 2.49f, 0.54f, 0f), new(0.06f, 0.12f, 5.2f), Lining, outward);
        }
        foreach (var end in new[] { -1f, 1f })
        {
            var outward = new Vect(0f, end < 0f ? -1.2f : 0f, end);
            Add("case-wall", new(0f, -0.04f, end * 2.71f), new(5.5f, 1.38f, 0.2f), Wood, outward);
            Add("case-wall", new(0f, 0.65f, end * 2.71f), new(5.5f, 0.09f, 0.23f), WoodEdge, outward);
            for (var grain = 0; grain < 12; grain++)
            {
                var tone = grain % 3 == 0 ? new ColorVect(0.32f, 0.15f, 0.06f) : new ColorVect(0.39f, 0.20f, 0.085f);
                Add("case-wall", new(0f, -0.62f + grain * 0.105f, end * 2.815f),
                    new(5.32f, 0.012f + grain % 3 * 0.004f, 0.006f), tone, outward);
            }
        }
        var front = new Vect(0f, -1.2f, -1f);
        Add("case-base", new(0f, 0.44f, -2.84f), new(0.72f, 0.2f, 0.06f), Nickel, front);
        Add("case-base", new(0f, 0.15f, -2.85f), new(0.38f, 0.4f, 0.07f), KeyBlack, front);
        Add("case-base", new(0f, 0.17f, -2.90f), new(0.1f, 0.2f, 0.06f), Nickel, front);
        foreach (var x in new[] { -0.26f, 0.26f })
            Add("contact", new(x, 0.44f, -2.88f), new(0.06f, 0.2f, 0.06f), KeyBlack, front,
                rotation: new Rotation(90f, Direction.Right));

        var lidPosition = new Location(0f, 2.6f, 3.10f);
        var lidRotation = new Rotation(-78f, Direction.Right);
        var lidExplosion = new Vect(0f, 2.2f, 1.4f);
        void LidPart(Location local, Vect scale, ColorVect color) =>
            Add("lid", lidPosition + local.AsVect() * lidRotation, scale, color, lidExplosion, rotation: lidRotation);
        LidPart(new(0f, 0f, 0f), new(5.5f, 0.16f, 3.65f), Wood);
        LidPart(new(0f, -0.095f, 0f), new(5.05f, 0.025f, 3.23f), Lining);
        foreach (var x in new[] { -2.62f, 2.62f })
            LidPart(new(x, -0.11f, 0f), new(0.18f, 0.13f, 3.65f), WoodEdge);
        foreach (var z in new[] { -1.73f, 1.73f })
            LidPart(new(0f, -0.11f, z), new(5.4f, 0.13f, 0.18f), WoodEdge);
        LidPart(new(0f, -0.12f, 1.05f), new(1.7f, 0.018f, 0.4f), Ivory);
        LidPart(new(1.8f, -0.12f, 1.05f), new(0.7f, 0.018f, 0.25f), Metal);
        const string inscription = "ENIGMA";
        for (var letter = 0; letter < inscription.Length; letter++)
            foreach (var path in EnigmaKeyboard.Glyphs[inscription[letter] - 'A'].Split('|'))
            {
                var points = path.Split(' ').Select(point => lidPosition + new Vect(
                    0.67f - letter * 0.24f - (point[0] - '0') * 0.04f,
                    -0.14f, 0.97f + (point[1] - '0') * 0.04f) * lidRotation).ToArray();
                for (var p = 1; p < points.Length; p++)
                    _caseLetters.Add((_scene.AddPrimitive(), points[p - 1], points[p], lidExplosion));
            }
        foreach (var x in new[] { -1.8f, 1.8f })
        {
            Add("shaft", new(x, 0.76f, 2.59f), new(0.65f, 0.65f, 0.65f), Nickel, new(0f, 0f, 1f));
            Add("case-base", new(x, 0.64f, 2.51f), new(0.55f, 0.04f, 0.35f), Nickel, new(0f, 0f, 1f));
        }
        // Small fasteners tie the lampboard and rear cover together visually.
        foreach (var x in new[] { -2.3f, 2.3f })
            foreach (var z in new[] { -0.2f, 0.68f })
                Add("contact", new(x, 0.73f, z), new(0.075f, 0.2f, 0.075f), Nickel, new(0f, 1.1f, -0.3f));
    }

    private static Location KeyPosition(int letter, float explosion)
    {
        var (row, col) = EnigmaKeyboard.Layout(letter);
        return new Location(1.94f - col * 0.48f - (row == 1 ? 0.24f : 0f),
            0.91f - row * 0.045f + 0.5f * explosion, -1.02f - row * 0.5f - explosion);
    }

    private static Location LampPosition(int letter, float explosion)
    {
        var (row, column) = EnigmaKeyboard.Layout(letter);
        return new Location(1.94f - column * 0.48f - (row == 1 ? 0.24f : 0f), 0.77f + explosion * 1.1f,
            0.62f - row * 0.37f - explosion * 0.3f);
    }

    private static float RotorX(int rotor, float explosion) => 0.75f - rotor * 0.9f - (rotor - 1) * 1.35f * explosion;

    private static Location PlugPosition(int letter, float explosion) => new(
        2.1f - letter % 13 * 0.35f, 0.27f + letter / 13 * 0.25f + 0.2f * explosion,
        -2.98f - 1.5f * explosion);

    private static Location RearPlugPosition(int letter, float explosion) =>
        PlugPosition(letter, explosion) + new Vect(0f, 0f, 0.18f);

    private Location Contact(int rotor, int local, bool right)
    {
        var angle = (local - _angles[rotor] + _rings[rotor]) * MathF.Tau / 26f;
        return new Location(RotorX(rotor, _explosion) + (right ? -0.27f : 0.27f),
            1.35f + _explosion * 1.2f + MathF.Cos(angle) * 0.63f,
            1.8f + _explosion * 0.8f + MathF.Sin(angle) * 0.63f);
    }

    private Location FixedContact(float x, int letter) => new(x,
        1.35f + _explosion * 1.2f + MathF.Cos(letter * MathF.Tau / 26f) * 0.63f,
        1.8f + _explosion * 0.8f + MathF.Sin(letter * MathF.Tau / 26f) * 0.63f);

    private Location ReflectorContact(int letter) => FixedContact(1.48f + 1.5f * _explosion, letter);

    private Location ReflectorBend(Location input, Location output) =>
        new(1.94f + 1.5f * _explosion, (input.Y + output.Y) / 2f, (input.Z + output.Z) / 2f);

    private void Update(float dt)
    {
        _flowTime += dt;
        _elapsed += dt;
        _explosion += Math.Clamp((Exploded ? 1f : 0f) - _explosion, -dt * 1.8f, dt * 1.8f);
        var turn = Math.Clamp(_elapsed / (CycleSeconds * 0.22f), 0f, 1f);
        turn = turn * turn * (3f - 2f * turn);
        for (var r = 0; r < 3; r++) _angles[r] = _from[r] + (_to[r] - _from[r]) * turn;
        var depression = _stroke == null ? 0f : EnigmaKeyboard.Depression(_elapsed / CycleSeconds);
        var activeLamp = _stroke != null && _elapsed >= CycleSeconds * 0.92f ? _stroke.Output - 'A' : -1;
        // Soft bloom identifies the lit lens; the point light illuminates nearby parts.
        _lampLight.Brightness = activeLamp >= 0 ? 0.08f : 0f;
        if (activeLamp >= 0)
            _lampLight.Position = LampPosition(activeLamp, _explosion) + new Vect(0f, 0.18f, 0f);
        KeyMotionChanged?.Invoke(_stroke?.Input, depression);
        foreach (var part in _parts)
        {
            var position = part.Home + part.Explosion * _explosion;
            if (part.ExplodedOnly && !Exploded)
                position = new Location(0f, -100f, 0f);
            if (_stroke != null && part.Key == _stroke.Input - 'A')
                position += new Vect(0f, -0.16f * depression, 0f);
            var rotation = part.Rotor >= 0
                // TinyFFR's named Right is -X; the authored mesh axis and contact math use +X.
                ? new Rotation((-_angles[part.Rotor] + _rings[part.Rotor]) * 360f / 26f, new Direction(1f, 0f, 0f))
                : part.Rotation;
            part.Instance.SetTransform(new Transform(translation: position.AsVect(), rotation: rotation, scaling: part.Scale));
            if ((part.Rotor >= 0 || part.ReflectorShell) && !part.Lettering)
                part.Instance.SetDefaultMaterialBaseColor(XRay
                    ? new ColorVect(0.75f, 0.78f, 0.69f, part.Scale.X > 1f ? 0.13f : 0.18f)
                    : part.Color);
            if (part.Lamp >= 0)
            {
                part.Instance.SetMaterial(activeLamp == part.Lamp ? _lampMaterial : _unlitLampMaterial);
                part.Instance.SetMaterialEmissiveStrengthMultiplier(activeLamp == part.Lamp ? 1f : 0f);
            }
        }
        UpdateRotorDrive();
        foreach (var glyph in _caseLetters)
        {
            glyph.Primitive.SetGeometryShape(new BoundedRay(glyph.Start + glyph.Explosion * _explosion,
                glyph.End + glyph.Explosion * _explosion), size: 0.009f, constantScreenSize: false, includeEndpoints: true);
            glyph.Primitive.SetPaintbrush(new PrimitivePaintbrush(KeyBlack));
        }
        for (var r = 0; r < 3; r++)
        {
            _rotorLabels[r].SetGeometryString(new Location(RotorX(r, _explosion), 2.35f + _explosion * 1.2f, 1.8f + _explosion * 0.8f),
                _captions[r], 0.024f);
            if (!Exploded)
            {
                for (var wire = 0; wire < 26; wire++)
                    for (var segment = 0; segment < 3; segment++)
                        HideLine(_wires[r, wire, segment]);
                continue;
            }
            for (var wire = 0; wire < 26; wire++)
            {
                var output = LabMachine.Wirings[r][wire] - 'A';
                var a = Contact(r, wire, true);
                var b = Contact(r, output, false);
                var mid = new Location((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f, (a.Z + b.Z) * 0.5f);
                var brush = Inactive;
                if (_stroke != null && _elapsed >= CycleSeconds * 0.22f)
                    for (var h = 0; h < _stroke.Hops.Count; h++)
                    {
                        var hop = _stroke.Hops[h];
                        if (hop.Rotor == r && (hop.Returning ? hop.LocalOutput : hop.LocalInput) == wire &&
                            _elapsed >= CycleSeconds * (0.22f + h * 0.075f))
                            brush = hop.Returning ? Return : Forward;
                    }
                if (brush.Equals(Inactive))
                {
                    SetLine(_wires[r, wire, 0], a, mid, brush);
                    SetLine(_wires[r, wire, 1], mid, b, brush);
                }
                else
                {
                    HideLine(_wires[r, wire, 0]);
                    HideLine(_wires[r, wire, 1]);
                }
                // Third segment is a small contact marker extending beyond the disc face.
                SetLine(_wires[r, wire, 2], a, a + new Vect(-0.06f, 0f, 0f), brush);
            }
        }
        UpdateReflectorWires();
        _reflectorLabel.SetGeometryString(new Location(1.92f + 1.5f * _explosion, 2.4f + 1.2f * _explosion, 1.8f + 0.8f * _explosion),
            "Reflector B", 0.02f);
        UpdatePath();
        if (SelectedPlug is { } selected)
            _plugModels[selected].SetDefaultMaterialBaseColor(new ColorVect(0.15f, 1f, 0.55f));
        UpdatePlugboardCables();
    }

    private void UpdateReflectorWires()
    {
        var wire = 0;
        for (var input = 0; input < 26; input++)
        {
            var output = ReflectorB.Reflect(input);
            if (input > output) continue;
            var a = ReflectorContact(input);
            var b = ReflectorContact(output);
            var bend = ReflectorBend(a, b);
            for (var segment = 0; segment < 2; segment++)
            {
                if (Exploded)
                    SetLine(_reflectorWires[wire, segment], segment == 0 ? a : bend,
                        segment == 0 ? bend : b, Inactive);
                else
                    HideLine(_reflectorWires[wire, segment]);
            }
            wire++;
        }
    }

    private void UpdatePlugboardCables()
    {
        for (var cable = 0; cable < 10; cable++)
        {
            if (cable >= _plugboardPairs.Count)
            {
                for (var segment = 0; segment < CableSegments; segment++)
                    _plugCables[cable, segment].Position = new Location(0f, -100f, 0f);
                for (var end = 0; end < 2; end++)
                    _cablePlugs[cable, end].Position = new Location(0f, -100f, 0f);
                continue;
            }
            var pair = _plugboardPairs[cable];
            _plugLetters[_cablePlugs[cable, 0]] = (char)('A' + pair.First);
            _plugLetters[_cablePlugs[cable, 1]] = (char)('A' + pair.Second);
            for (var end = 0; end < 2; end++)
            {
                var socket = PlugPosition(end == 0 ? pair.First : pair.Second, _explosion);
                _cablePlugs[cable, end].SetTransform(new Transform(
                    translation: (socket + new Vect(0f, -0.02f, -0.07f)).AsVect(),
                    rotation: new Rotation(90f, Direction.Right), scaling: new Vect(0.17f, 0.8f, 0.23f)));
            }
            for (var segment = 0; segment < CableSegments; segment++)
            {
                var start = CablePoint(cable, segment / (float)CableSegments);
                var finish = CablePoint(cable, (segment + 1f) / CableSegments);
                var delta = finish - start;
                _plugCables[cable, segment].SetTransform(new Transform(
                    translation: (start + delta * 0.5f).AsVect(),
                    rotation: new Direction(1f, 0f, 0f) >> delta.Direction,
                    scaling: new Vect(delta.Length + 0.008f, 0.26f, 0.26f)));
            }
        }
    }

    private Location CablePoint(int cable, float t)
    {
        var pair = _plugboardPairs[cable];
        var a = PlugPosition(pair.First, _explosion) + new Vect(0f, -0.02f, -0.12f);
        var b = PlugPosition(pair.Second, _explosion) + new Vect(0f, -0.02f, -0.12f);
        var sag = MathF.Sin(t * MathF.PI);
        return a + (b - a) * t + new Vect(0f, -(0.3f + cable * 0.025f) * sag,
            -(0.22f + cable * 0.035f) * sag);
    }

    private void UpdatePath()
    {
        var count = 0;
        _flowSegments.Clear();
        foreach (var plug in _plugModels) plug.SetDefaultMaterialBaseColor(Brass);
        foreach (var plug in _rearPlugModels) plug.SetDefaultMaterialBaseColor(Brass);
        if (Exploded && _stroke != null)
        {
            void Segment(Location a, Location b, bool returning, int stage, bool cable = false)
            {
                var reached = _elapsed >= CycleSeconds * (0.22f + stage * 0.075f);
                var line = _path[count++];
                if (reached)
                {
                    HideLine(line);
                    _flowSegments.Add((a, b, returning, stage, cable));
                }
                else if (cable) HideLine(line);
                else SetLine(line, a, b, Inactive);
            }
            void ThroughCable(int input, int output, bool returning, int stage)
            {
                var cable = -1;
                for (var i = 0; i < _plugboardPairs.Count; i++)
                {
                    var pair = _plugboardPairs[i];
                    if ((pair.First == input && pair.Second == output) || (pair.Second == input && pair.First == output))
                    {
                        cable = i;
                        break;
                    }
                }
                if (cable < 0) throw new InvalidOperationException("The signal's plugboard mapping has no physical cable.");
                var reverse = _plugboardPairs[cable].First != input;
                float Parameter(int segment) => reverse ? 1f - segment / (float)CableSegments : segment / (float)CableSegments;
                Segment(RearPlugPosition(input, _explosion), PlugPosition(input, _explosion), returning, stage);
                Segment(PlugPosition(input, _explosion), CablePoint(cable, Parameter(0)), returning, stage, true);
                for (var i = 0; i < CableSegments; i++)
                    Segment(CablePoint(cable, Parameter(i)), CablePoint(cable, Parameter(i + 1)), returning, stage, true);
                Segment(CablePoint(cable, Parameter(CableSegments)), PlugPosition(output, _explosion), returning, stage, true);
            }
            void Route(bool returning, int stage, params Location[] points)
            {
                for (var i = 1; i < points.Length; i++)
                    Segment(points[i - 1], points[i], returning, stage);
            }
            var forwardLane = -3.1f - 1.1f * _explosion;
            var returnLane = forwardLane - 0.3f;
            var belowKeyboard = 0.3f + 0.5f * _explosion;
            var boardFront = -3.15f - 1.5f * _explosion;
            void BoardToRotor(Location board, Location rotor, bool returning, int stage)
            {
                var lane = returning ? returnLane : forwardLane;
                var boardZ = returning ? board.Z + 0.2f : boardFront;
                var face = new Location(board.X, board.Y, boardZ);
                var side = new Location(lane, board.Y, boardZ);
                var rear = new Location(lane, board.Y, rotor.Z);
                var rise = new Location(lane, rotor.Y, rotor.Z);
                // Approach the rotor along its axle, outside the adjacent disc face.
                var approach = new Location(rotor.X - 0.18f, rotor.Y, rotor.Z);
                if (returning) Route(true, stage, rotor, approach, rise, rear, side, face, board);
                else Route(false, stage, board, face, side, rear, rise, approach, rotor);
            }
            var hops = _stroke.Hops;
            var plugIn = RearPlugPosition(hops[0].Input, _explosion);
            var plugInMapped = PlugPosition(hops[0].Output, _explosion);
            var plugOut = RearPlugPosition(hops[8].Input, _explosion);
            var plugOutMapped = PlugPosition(hops[8].Output, _explosion);
            var keyPosition = KeyPosition(_stroke.Input - 'A', _explosion) +
                new Vect(0f, -0.31f - 0.16f * EnigmaKeyboard.Depression(_elapsed / CycleSeconds), 0f);
            Route(false, 0, keyPosition,
                new(keyPosition.X, belowKeyboard, keyPosition.Z),
                new(keyPosition.X, belowKeyboard, plugIn.Z + 0.2f),
                new(plugIn.X, belowKeyboard, plugIn.Z + 0.2f),
                new(plugIn.X, plugIn.Y, plugIn.Z + 0.2f), plugIn);
            if (hops[0].Input != hops[0].Output)
                ThroughCable(hops[0].Input, hops[0].Output, false, 0);
            else Segment(plugIn, plugInMapped, false, 0);
            BoardToRotor(plugInMapped, Contact(2, hops[1].LocalInput, true), false, 1);
            for (var r = 2; r > 0; r--)
            {
                Segment(Contact(r, hops[3 - r].LocalInput, true), Contact(r, hops[3 - r].LocalOutput, false), false, 3 - r);
                Segment(Contact(r, hops[3 - r].LocalOutput, false), Contact(r - 1, hops[4 - r].LocalInput, true), false, 3 - r);
            }
            Segment(Contact(0, hops[3].LocalInput, true), Contact(0, hops[3].LocalOutput, false), false, 3);
            var reflectorIn = ReflectorContact(hops[4].Input);
            var reflectorOut = ReflectorContact(hops[4].Output);
            Segment(Contact(0, hops[3].LocalOutput, false), reflectorIn, false, 4);
            var bend = ReflectorBend(reflectorIn, reflectorOut);
            Segment(reflectorIn, bend, false, 4);
            Segment(bend, reflectorOut, true, 4);
            Segment(reflectorOut, Contact(0, hops[5].LocalInput, false), true, 5);
            for (var r = 0; r < 2; r++)
            {
                Segment(Contact(r, hops[5 + r].LocalInput, false), Contact(r, hops[5 + r].LocalOutput, true), true, 5 + r);
                Segment(Contact(r, hops[5 + r].LocalOutput, true), Contact(r + 1, hops[6 + r].LocalInput, false), true, 6 + r);
            }
            Segment(Contact(2, hops[7].LocalInput, false), Contact(2, hops[7].LocalOutput, true), true, 7);
            BoardToRotor(plugOut, Contact(2, hops[7].LocalOutput, true), true, 8);
            if (hops[8].Input != hops[8].Output)
                ThroughCable(hops[8].Input, hops[8].Output, true, 8);
            else Segment(plugOut, plugOutMapped, true, 8);
            var lamp = LampPosition(_stroke.Output - 'A', _explosion);
            var lampUnderside = lamp + new Vect(0f, -0.2f, 0f);
            Route(true, 9, plugOutMapped,
                new(plugOutMapped.X, plugOutMapped.Y, boardFront - 0.16f),
                new(returnLane + 0.15f, plugOutMapped.Y, boardFront - 0.16f),
                new(returnLane + 0.15f, lampUnderside.Y, boardFront - 0.16f),
                new(returnLane + 0.15f, lampUnderside.Y, lamp.Z),
                lampUnderside, lamp + new Vect(0f, -0.06f, 0f));
            _plugModels[hops[0].Input].SetDefaultMaterialBaseColor(new ColorVect(1f, 0.63f, 0.12f));
            _plugModels[hops[0].Output].SetDefaultMaterialBaseColor(new ColorVect(1f, 0.63f, 0.12f));
            _plugModels[hops[8].Input].SetDefaultMaterialBaseColor(new ColorVect(0.12f, 0.9f, 1f));
            _plugModels[hops[8].Output].SetDefaultMaterialBaseColor(new ColorVect(0.12f, 0.9f, 1f));
            _rearPlugModels[hops[0].Input].SetDefaultMaterialBaseColor(new ColorVect(1f, 0.63f, 0.12f));
            _rearPlugModels[hops[8].Input].SetDefaultMaterialBaseColor(new ColorVect(0.12f, 0.9f, 1f));
        }
        for (; count < _path.Length; count++)
            HideLine(_path[count]);
        UpdateFlowWires();
    }

    private void BuildFlowWires()
    {
        const int sides = 12;
        var vertices = new List<MeshVertex>();
        var indices = new List<VertexTriangle>();
        for (var side = 0; side <= sides; side++)
        {
            var angle = side * MathF.Tau / sides;
            var normal = new Direction(0f, MathF.Cos(angle), MathF.Sin(angle));
            for (var end = 0; end < 2; end++)
                vertices.Add(new MeshVertex(new Location(end - 0.5f, normal.Y, normal.Z),
                    new(end, side / (float)sides), new Direction(1f, 0f, 0f),
                    new Direction(0f, -MathF.Sin(angle), MathF.Cos(angle)), normal));
            if (side == sides) continue;
            var i = side * 2;
            indices.Add(new(i, i + 2, i + 1));
            indices.Add(new(i + 1, i + 2, i + 3));
        }
        var mesh = Own(_factory.MeshBuilder.CreateFromVertices(vertices.ToArray(), indices.ToArray()));
        Material CreateFlowMaterial(ColorVect color)
        {
            var baseMap = Own(_factory.TextureBuilder.CreateColorMap(
                TexturePattern.GradientHorizontal(color, color, resolution: (128, 2)), includeAlpha: true,
                TextureCreationConfig.ForColorMap() with { AllowsDynamicWrites = true }));
            var emission = Own(_factory.TextureBuilder.CreateEmissiveMap(
                TexturePattern.GradientHorizontal(color, color, resolution: (128, 2)),
                TexturePattern.GradientHorizontal((Real)0f, (Real)1f, resolution: (128, 2)),
                TextureCreationConfig.ForEmissiveMap() with { AllowsDynamicWrites = true }));
            var texels = new TexelRgba32[256];
            var colors = new TexelRgba32[256];
            for (var i = 0; i < texels.Length; i++)
            {
                var u = (i % 128) / 128f;
                var distance = MathF.Min(u, 1f - u);
                var pulse = MathF.Exp(-distance * distance / 0.0008f);
                texels[i] = ITextureBuilder.CreateEmissiveTexel(color, 0.04f + 0.96f * pulse);
                colors[i] = new TexelRgba32(new ColorVect(color.Red, color.Green, color.Blue, 0.22f + 0.63f * pulse));
            }
            baseMap.OverwriteTexels<TexelRgba32>(colors);
            emission.OverwriteTexels<TexelRgba32>(texels);
            return Own(_factory.MaterialBuilder.CreateStandardMaterial(baseMap, emissiveMap: emission,
                alphaMode: StandardMaterialAlphaMode.FullBlending, enablePerInstanceEffects: true));
        }
        var forward = CreateFlowMaterial(new ColorVect(1f, 0.63f, 0.12f));
        var returning = CreateFlowMaterial(new ColorVect(0.12f, 0.9f, 1f));
        // Separate pools preserve direction colors without changing materials every frame.
        for (var i = 0; i < _flowWires.Length; i++)
        {
            _flowWires[i] = Own(_factory.ObjectBuilder.CreateModelInstance(mesh, i < 72 ? forward : returning,
                initialPosition: new Location(0f, -100f, 0f)));
            _scene.Add(_flowWires[i]);
        }
    }

    private void UpdateFlowWires()
    {
        var forwardCount = 0;
        var returnCount = 72;
        var offset = 0f;
        foreach (var segment in _flowSegments)
        {
            var delta = segment.End - segment.Start;
            var length = delta.Length;
            if (length < 0.001f) continue;
            var index = segment.Returning ? returnCount++ : forwardCount++;
            if (index >= (segment.Returning ? 144 : 72))
                throw new InvalidOperationException("Signal flow exceeded its textured wire capacity.");
            var wire = _flowWires[index];
            var radius = segment.Cable ? CableFlowRadius : FlowRadius;
            wire.SetTransform(new Transform(translation: (segment.Start + delta * 0.5f).AsVect(),
                rotation: new Direction(1f, 0f, 0f) >> delta.Direction,
                scaling: new Vect(length, radius, radius)));
            _flowTextureTransforms[index] = FlowTextureTransform(length, offset, _flowTime);
            offset += length;
        }
        for (var i = forwardCount; i < 72; i++)
        {
            _flowWires[i].Position = new Location(0f, -100f, 0f);
            _flowTextureTransforms[i] = null;
        }
        for (var i = returnCount; i < 144; i++)
        {
            _flowWires[i].Position = new Location(0f, -100f, 0f);
            _flowTextureTransforms[i] = null;
        }
    }

    private void ApplyFlowTextureTransforms()
    {
        for (var i = 0; i < _flowWires.Length; i++)
            if (_flowTextureTransforms[i] is { } transform)
            {
                var effects = _flowWires[i].MaterialEffects ??
                    throw new InvalidOperationException("Wire material requires texture transforms.");
                effects.SetTransform(transform);
            }
    }

    private static void HideLine(ScenePrimitive primitive) =>
        primitive.SetGeometryPoint(new Location(0f, -100f, 0f), 0f);

    private static void SetLine(ScenePrimitive primitive, Location a, Location b, PrimitivePaintbrush brush)
    {
        primitive.SetPaintbrush(brush);
        primitive.SetGeometryShape(new BoundedRay(a, b), size: brush.Equals(Inactive) ? 0.0008f : 0.0035f, includeEndpoints: false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // The scene depends on its objects; release membership before disposing shared resources.
        if (!_scene.Equals(default(Scene))) _scene.RemoveAll();
        for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose();
        _factory.Dispose();
    }
}
