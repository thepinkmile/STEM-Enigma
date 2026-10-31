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
    private static readonly ColorVect ForwardColor = new(1f, 0.63f, 0.12f);
    private static readonly ColorVect ReturnColor = new(0.12f, 0.9f, 1f);
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
    private const int RouteCapacity = 640;
    private const int IdleSamples = 8;
    private const float WireRadius = 0.022f;
    private const float CableWireRadius = 0.02f;
    private const float IdleWireRadius = 0.011f;
    private const float HeadRadius = 0.055f;
    private readonly ModelInstance[,] _plugCables = new ModelInstance[10, CableSegments];
    private readonly ModelInstance[,] _cablePlugs = new ModelInstance[10, 2];
    private IReadOnlyList<(int First, int Second)> _plugboardPairs = Array.Empty<(int, int)>();
    private readonly ModelInstance[,,] _rotorWires = new ModelInstance[3, 26, IdleSamples];
    private readonly ScenePrimitive[,] _contactMarkers = new ScenePrimitive[3, 26];
    private readonly ModelInstance[,] _reflectorWires = new ModelInstance[13, IdleSamples];
    private readonly ModelInstance[] _reflectorPins = new ModelInstance[26];
    private static readonly Reflector ReflectorB = Reflector.EnigmaI.UKW_B();
    private readonly ModelInstance[] _routeTubes = new ModelInstance[RouteCapacity];
    private readonly Material?[] _routeTubeMaterials = new Material?[RouteCapacity];
    private readonly HashSet<ModelInstance> _wireModels = [];
    private ModelInstance _pulseHead;
    private PointLight _pulseLight;
    private Material _wireMaterial;
    private Material _forwardMaterial;
    private Material _returnMaterial;
    private Material _forwardHeadMaterial;
    private Material _returnHeadMaterial;
    private readonly ModelInstance[] _pickHits;
    // The signal route from key to lamp, in signal order, with each segment's starting arc length.
    private readonly List<(Location Start, Location End, bool Returning, bool Cable, float Arc)> _route = [];
    private readonly List<Location> _samples = [];
    private float _routeLength;
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
                for (var i = 0; i < 26; i++) _contactMarkers[r, i] = _scene.AddPrimitive();
            }
            BuildWires();
            _pickHits = new ModelInstance[_parts.Count + _wireModels.Count + _plugCables.Length + _cablePlugs.Length];
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
            Renderer.Render();
        }, tickRateHz: 60);

    // Key press and rotor step take a short fixed window; the pulse then travels until the lamp
    // lights, and the key releases while the lamp is lit. The whole trip lasts CycleSeconds.
    private float StepSeconds => MathF.Min(0.35f, CycleSeconds * 0.25f);
    private float ArrivalSeconds => CycleSeconds - StepSeconds;

    // Maps elapsed time onto the original 0..1 key/rotor animation progress (press 0-0.22, held
    // 0.22-0.68, release 0.68-0.9), stretching the held phase across the pulse's travel.
    private float MechanicalProgress(float elapsed)
    {
        if (elapsed < StepSeconds) return 0.22f * MathF.Max(elapsed, 0f) / StepSeconds;
        if (elapsed < ArrivalSeconds) return 0.22f + 0.46f * (elapsed - StepSeconds) / (ArrivalSeconds - StepSeconds);
        if (elapsed < CycleSeconds) return 0.68f + 0.22f * (elapsed - ArrivalSeconds) / StepSeconds;
        return 1f;
    }

    private float ElapsedForProgress(float progress)
    {
        if (progress < 0.22f) return StepSeconds * progress / 0.22f;
        if (progress < 0.68f) return StepSeconds + (ArrivalSeconds - StepSeconds) * (progress - 0.22f) / 0.46f;
        if (progress < 0.9f) return ArrivalSeconds + StepSeconds * (progress - 0.68f) / 0.22f;
        return CycleSeconds;
    }

    // Fraction of the route the pulse has covered. After arriving, the lit route holds, then the
    // pulse replays from the key until the next letter.
    private float PulseFraction(float elapsed)
    {
        var travel = ArrivalSeconds - StepSeconds;
        if (elapsed < ArrivalSeconds) return Math.Clamp((elapsed - StepSeconds) / travel, 0f, 1f);
        var hold = MathF.Max(0.6f, travel * 0.3f);
        var replay = (elapsed - ArrivalSeconds) % (hold + travel);
        return replay < hold ? 1f : (replay - hold) / travel;
    }

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
        if (!Exploded && (_routeTubes.Any(tube => tube.Position.Y > -50f) || _pulseHead.Position.Y > -50f))
            throw new InvalidOperationException("Signal wires must be hidden in the assembled view.");
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
            var travelling = (StepSeconds + ArrivalSeconds) * 0.5f;
            foreach (var time in new[] { 0f, travelling, ArrivalSeconds - 0.001f, ArrivalSeconds, CycleSeconds })
            {
                _elapsed = time;
                Update(0f);
                VerifyRotorDrive();
                var lit = time >= ArrivalSeconds;
                if (MathF.Abs(_lampLight.Brightness - (lit ? 0.08f : 0f)) > 0.001f)
                    throw new InvalidOperationException("The output bulb light did not follow the signal timing.");
                if (lit &&
                    (_lampLight.Position - (LampPosition(_stroke.Output - 'A', _explosion) + new Vect(0f, 0.18f, 0f))).Length > 0.001f)
                    throw new InvalidOperationException("The output bulb light did not follow the lamp assembly.");
                foreach (var part in _parts.Where(p => p.Key >= 0))
                {
                    var expected = part.Home + part.Explosion * _explosion;
                    if (part.Key == _stroke.Input - 'A' && time == travelling)
                        expected += new Vect(0f, -0.16f, 0f);
                    else if (part.Key == _stroke.Input - 'A' && time != 0f && time != CycleSeconds)
                        continue;
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
            if (!_wireModels.Contains(_pickHits[i]))
                return _pickHits[i];
        return null;
    }

    public void VerifyCableFlow()
    {
        VerifyPulseTiming();
        var machine = new LabMachine();
        machine.Configure("AAA", "AAA", string.Join(" ", _plugboardPairs.Select(p => $"{(char)('A' + p.First)}{(char)('A' + p.Second)}")));
        var exploded = Exploded;
        Exploded = true;
        foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            Play(machine.Press(letter));
            _elapsed = CycleSeconds;
            Update(0f);
            for (var i = 1; i < _route.Count; i++)
                if ((_route[i].Start - _route[i - 1].End).Length > 0.001f)
                    throw new InvalidOperationException("The signal route must be ordered continuously from key to lamp.");
            var lampTip = LampPosition(_stroke!.Output - 'A', _explosion) + new Vect(0f, -0.06f, 0f);
            if ((_route[^1].End - lampTip).Length > 0.001f)
                throw new InvalidOperationException("The signal route must end beneath the output lamp.");
            var entry = _stroke!.Hops[0];
            if (!_route.Any(s => !s.Returning &&
                (s.Start - RearPlugPosition(entry.Input, _explosion)).Length < 0.001f &&
                (s.End - PlugPosition(entry.Input, _explosion)).Length < 0.001f))
                throw new InvalidOperationException("The plugboard input must enter from its rear terminal.");
            var exit = _stroke.Hops[8];
            if (!_route.Any(s => s.Returning &&
                (s.Start - PlugPosition(exit.Output, _explosion)).Length < 0.001f &&
                (s.End - RearPlugPosition(exit.Output, _explosion)).Length < 0.001f))
                throw new InvalidOperationException("The plugboard return must leave through its rear terminal.");
            foreach (var hop in _stroke!.Hops.Where(h => h.Stage.StartsWith("Plug", StringComparison.OrdinalIgnoreCase) && h.Input != h.Output))
            {
                var cable = Enumerable.Range(0, _plugboardPairs.Count)
                    .Single(i => _plugboardPairs[i].First == hop.Input || _plugboardPairs[i].Second == hop.Input);
                var reverse = _plugboardPairs[cable].First != hop.Input;
                var start = CablePoint(cable, reverse ? 1f : 0f);
                var end = CablePoint(cable, reverse ? 1f - 1f / CableSegments : 1f / CableSegments);
                if (!_route.Any(s => s.Returning == hop.Returning &&
                    (s.Start - start).Length < 0.001f && (s.End - end).Length < 0.001f))
                    throw new InvalidOperationException("The plugboard trace does not follow the cable in signal order.");
            }
        }
        Exploded = exploded;
    }

    private void VerifyPulseTiming()
    {
        var cycle = CycleSeconds;
        foreach (var seconds in new[] { 0.3f, 3f, 8f })
        {
            CycleSeconds = seconds;
            var previous = 0f;
            for (var time = 0f; time < ArrivalSeconds; time += seconds / 50f)
            {
                var fraction = PulseFraction(time);
                if (fraction < previous || fraction >= 1f)
                    throw new InvalidOperationException("The pulse must advance steadily until it reaches the lamp.");
                previous = fraction;
            }
            if (PulseFraction(ArrivalSeconds) != 1f || MechanicalProgress(CycleSeconds) != 1f)
                throw new InvalidOperationException("The pulse must reach the lamp before the key finishes releasing.");
            var mid = PulseFraction(ArrivalSeconds + MathF.Max(0.6f, (ArrivalSeconds - StepSeconds) * 0.3f) + 0.001f);
            if (mid > 0.05f)
                throw new InvalidOperationException("The pulse must restart from the key after holding at the lamp.");
            foreach (var progress in new[] { 0f, 0.1f, 0.22f, 0.5f, 0.79f })
                if (MathF.Abs(MechanicalProgress(ElapsedForProgress(progress)) - progress) > 0.0001f)
                    throw new InvalidOperationException("The key timeline mapping must be invertible.");
        }
        CycleSeconds = cycle;
        if (CableWireRadius >= 0.1f * 0.26f)
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
        var progress = _stroke == null ? 1f : MechanicalProgress(_elapsed);
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
        var progress = _stroke == null ? 1f : MechanicalProgress(_elapsed);
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
                _elapsed = ElapsedForProgress(progress);
                Update(0f);
                VerifyRotorDrive();
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

    public void VerifyPulseCycle(Action<string> capture)
    {
        if (_stroke == null || !Exploded)
            throw new InvalidOperationException("The pulse check needs a key stroke in the exploded view.");
        var elapsed = _elapsed;
        try
        {
            var lit = -1;
            foreach (var fraction in new[] { 0.3f, 0.65f, 1f })
            {
                _elapsed = StepSeconds + (ArrivalSeconds - StepSeconds) * fraction;
                Update(0f);
                var count = Enumerable.Range(0, _routeTubes.Length).Count(i => _routeTubes[i].Position.Y > -50f &&
                    !Equals(_routeTubeMaterials[i], _wireMaterial));
                if (count <= lit || (fraction < 1f) != (_pulseHead.Position.Y > -50f))
                    throw new InvalidOperationException("The pulse must leave a growing lit wake and vanish at the lamp.");
                lit = count;
                Renderer.Render();
                capture($"pulse-{(int)(fraction * 100)}");
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

    // The reflector's internal wires loop out toward the casing and back to the paired pin.
    private Location ReflectorCurve(Location input, Location output, float t)
    {
        var bend = 1.94f + 1.5f * _explosion;
        return Cubic(input, new(bend, input.Y, input.Z), new(bend, output.Y, output.Z), output, t);
    }

    // Wires leave each contact square to the disc face, then sweep across along the axle.
    private static Location AxialCurve(Location a, Location b, float t)
    {
        var reach = (b.X - a.X) * 0.45f;
        return Cubic(a, a + new Vect(reach, 0f, 0f), b + new Vect(-reach, 0f, 0f), b, t);
    }

    private static Location Cubic(Location p0, Location p1, Location p2, Location p3, float t)
    {
        var u = 1f - t;
        var v = p0.AsVect() * (u * u * u) + p1.AsVect() * (3f * u * u * t) +
            p2.AsVect() * (3f * u * t * t) + p3.AsVect() * (t * t * t);
        return new Location(v.X, v.Y, v.Z);
    }

    // Centripetal Catmull-Rom (Barry-Goldman form): passes through every anchor without the
    // overshoot or self-loops a uniform spline makes when anchors are unevenly spaced.
    private static Vect CatmullRom(Vect p0, Vect p1, Vect p2, Vect p3, float t)
    {
        static float Knot(Vect a, Vect b) => MathF.Max(MathF.Sqrt((b - a).Length), 0.0001f);
        static Vect Mix(Vect a, Vect b, float ta, float tb, float u) => (a * (tb - u) + b * (u - ta)) * (1f / (tb - ta));
        var t1 = Knot(p0, p1);
        var t2 = t1 + Knot(p1, p2);
        var t3 = t2 + Knot(p2, p3);
        var u = t1 + (t2 - t1) * t;
        var a1 = Mix(p0, p1, 0f, t1, u);
        var a2 = Mix(p1, p2, t1, t2, u);
        var a3 = Mix(p2, p3, t2, t3, u);
        return Mix(Mix(a1, a2, 0f, t2, u), Mix(a2, a3, t1, t3, u), t1, t2, u);
    }

    private void AppendSmooth(Location[] anchors)
    {
        var points = new List<Vect>(anchors.Length);
        foreach (var anchor in anchors)
            if (points.Count == 0 || (anchor.AsVect() - points[^1]).Length > 0.001f)
                points.Add(anchor.AsVect());
        _samples.Add(anchors[0]);
        for (var i = 0; i + 1 < points.Count; i++)
        {
            var p1 = points[i];
            var p2 = points[i + 1];
            var p0 = i > 0 ? points[i - 1] : p1 * 2f - p2;
            var p3 = i + 2 < points.Count ? points[i + 2] : p2 * 2f - p1;
            var steps = Math.Clamp((int)MathF.Ceiling((p2 - p1).Length / 0.15f), 2, 16);
            for (var k = 1; k < steps; k++)
            {
                var v = CatmullRom(p0, p1, p2, p3, k / (float)steps);
                _samples.Add(new Location(v.X, v.Y, v.Z));
            }
            _samples.Add(new Location(p2.X, p2.Y, p2.Z));
        }
    }

    private void Update(float dt)
    {
        _elapsed += dt;
        _explosion += Math.Clamp((Exploded ? 1f : 0f) - _explosion, -dt * 1.8f, dt * 1.8f);
        var turn = Math.Clamp(MechanicalProgress(_elapsed) / 0.22f, 0f, 1f);
        turn = turn * turn * (3f - 2f * turn);
        for (var r = 0; r < 3; r++) _angles[r] = _from[r] + (_to[r] - _from[r]) * turn;
        var depression = _stroke == null ? 0f : EnigmaKeyboard.Depression(MechanicalProgress(_elapsed));
        var activeLamp = _stroke != null && _elapsed >= ArrivalSeconds ? _stroke.Output - 'A' : -1;
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
            for (var wire = 0; wire < 26; wire++)
            {
                var a = Contact(r, wire, true);
                var b = Contact(r, LabMachine.Wirings[r][wire] - 'A', false);
                // The signal route draws its own copy of the wires it passes through.
                var routed = false;
                if (_stroke != null)
                    foreach (var hop in _stroke.Hops)
                        routed |= hop.Rotor == r && (hop.Returning ? hop.LocalOutput : hop.LocalInput) == wire;
                for (var sample = 0; sample < IdleSamples; sample++)
                {
                    if (Exploded && !routed)
                        PlaceTube(_rotorWires[r, wire, sample], AxialCurve(a, b, sample / (float)IdleSamples),
                            AxialCurve(a, b, (sample + 1f) / IdleSamples), IdleWireRadius);
                    else
                        _rotorWires[r, wire, sample].Position = new Location(0f, -100f, 0f);
                }
                // A small contact marker extends beyond the disc face.
                if (Exploded) SetLine(_contactMarkers[r, wire], a, a + new Vect(-0.06f, 0f, 0f));
                else HideLine(_contactMarkers[r, wire]);
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
        var routed = _stroke == null ? -1 : _stroke.Hops[4].Input;
        for (var input = 0; input < 26; input++)
        {
            var output = ReflectorB.Reflect(input);
            if (input > output) continue;
            var a = ReflectorContact(input);
            var b = ReflectorContact(output);
            var visible = Exploded && routed != input && routed != output;
            for (var sample = 0; sample < IdleSamples; sample++)
            {
                if (visible)
                    PlaceTube(_reflectorWires[wire, sample], ReflectorCurve(a, b, sample / (float)IdleSamples),
                        ReflectorCurve(a, b, (sample + 1f) / IdleSamples), IdleWireRadius);
                else
                    _reflectorWires[wire, sample].Position = new Location(0f, -100f, 0f);
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
        _route.Clear();
        _routeLength = 0f;
        foreach (var plug in _plugModels) plug.SetDefaultMaterialBaseColor(Brass);
        foreach (var plug in _rearPlugModels) plug.SetDefaultMaterialBaseColor(Brass);
        if (Exploded && _stroke != null)
        {
            void Segment(Location a, Location b, bool returning, bool cable = false)
            {
                var length = (b - a).Length;
                if (length < 0.0001f) return;
                _route.Add((a, b, returning, cable, _routeLength));
                _routeLength += length;
            }
            void Emit(bool returning)
            {
                for (var i = 1; i < _samples.Count; i++) Segment(_samples[i - 1], _samples[i], returning);
                _samples.Clear();
            }
            void Route(bool returning, params Location[] anchors)
            {
                AppendSmooth(anchors);
                Emit(returning);
            }
            void Axial(Location a, Location b, bool returning)
            {
                var steps = Math.Clamp((int)MathF.Ceiling((b - a).Length / 0.08f), 6, 16);
                for (var i = 0; i <= steps; i++) _samples.Add(AxialCurve(a, b, i / (float)steps));
                Emit(returning);
            }
            // Forward signals enter a rear terminal and leave a front socket; the return enters a
            // front socket and leaves through the rear terminal.
            void ThroughCable(int input, int output, bool returning)
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
                if (!returning) Segment(RearPlugPosition(input, _explosion), PlugPosition(input, _explosion), returning);
                Segment(PlugPosition(input, _explosion), CablePoint(cable, Parameter(0)), returning, true);
                for (var i = 0; i < CableSegments; i++)
                    Segment(CablePoint(cable, Parameter(i)), CablePoint(cable, Parameter(i + 1)), returning, true);
                Segment(CablePoint(cable, Parameter(CableSegments)), PlugPosition(output, _explosion), returning, true);
                if (returning) Segment(PlugPosition(output, _explosion), RearPlugPosition(output, _explosion), returning);
            }
            var forwardLane = -3.1f - 1.1f * _explosion;
            var returnLane = forwardLane - 0.3f;
            var belowKeyboard = 0.3f + 0.5f * _explosion;
            var boardFront = -3.15f - 1.5f * _explosion;
            // Runs beside the case on its own lane, rising gently toward the rotor and meeting it along the axle.
            void BoardToRotor(Location board, Location rotor, bool returning)
            {
                var lane = returning ? returnLane : forwardLane;
                // Both legs approach the front sockets, on separate rows so they never overlap.
                var boardZ = returning ? boardFront - 0.25f : boardFront;
                Location[] anchors =
                [
                    board,
                    new(board.X, board.Y, boardZ),
                    new(lane, board.Y, boardZ),
                    new(lane, rotor.Y, rotor.Z - 0.7f),
                    new(rotor.X - 0.4f, rotor.Y, rotor.Z),
                    rotor,
                ];
                if (returning) Array.Reverse(anchors);
                Route(returning, anchors);
            }
            var hops = _stroke.Hops;
            var plugIn = RearPlugPosition(hops[0].Input, _explosion);
            var plugInMapped = PlugPosition(hops[0].Output, _explosion);
            var plugOut = PlugPosition(hops[8].Input, _explosion);
            var plugOutMapped = RearPlugPosition(hops[8].Output, _explosion);
            var keyPosition = KeyPosition(_stroke.Input - 'A', _explosion) +
                new Vect(0f, -0.31f - 0.16f * EnigmaKeyboard.Depression(MechanicalProgress(_elapsed)), 0f);
            Route(false, keyPosition,
                new(keyPosition.X, belowKeyboard, keyPosition.Z),
                new(plugIn.X, plugIn.Y, plugIn.Z + 0.25f), plugIn);
            if (hops[0].Input != hops[0].Output)
                ThroughCable(hops[0].Input, hops[0].Output, false);
            else Segment(plugIn, plugInMapped, false);
            BoardToRotor(plugInMapped, Contact(2, hops[1].LocalInput, true), false);
            for (var r = 2; r > 0; r--)
            {
                Axial(Contact(r, hops[3 - r].LocalInput, true), Contact(r, hops[3 - r].LocalOutput, false), false);
                Axial(Contact(r, hops[3 - r].LocalOutput, false), Contact(r - 1, hops[4 - r].LocalInput, true), false);
            }
            Axial(Contact(0, hops[3].LocalInput, true), Contact(0, hops[3].LocalOutput, false), false);
            var reflectorIn = ReflectorContact(hops[4].Input);
            var reflectorOut = ReflectorContact(hops[4].Output);
            Axial(Contact(0, hops[3].LocalOutput, false), reflectorIn, false);
            // The signal turns around halfway through the reflector's looped wire.
            for (var i = 0; i <= 6; i++) _samples.Add(ReflectorCurve(reflectorIn, reflectorOut, i / 12f));
            Emit(false);
            for (var i = 6; i <= 12; i++) _samples.Add(ReflectorCurve(reflectorIn, reflectorOut, i / 12f));
            Emit(true);
            Axial(reflectorOut, Contact(0, hops[5].LocalInput, false), true);
            for (var r = 0; r < 2; r++)
            {
                Axial(Contact(r, hops[5 + r].LocalInput, false), Contact(r, hops[5 + r].LocalOutput, true), true);
                Axial(Contact(r, hops[5 + r].LocalOutput, true), Contact(r + 1, hops[6 + r].LocalInput, false), true);
            }
            Axial(Contact(2, hops[7].LocalInput, false), Contact(2, hops[7].LocalOutput, true), true);
            BoardToRotor(plugOut, Contact(2, hops[7].LocalOutput, true), true);
            if (hops[8].Input != hops[8].Output)
                ThroughCable(hops[8].Input, hops[8].Output, true);
            else Segment(plugOut, plugOutMapped, true);
            // From the rear terminal the lamp feed stays inside the case, just inside the left wall.
            var lamp = LampPosition(_stroke.Output - 'A', _explosion);
            var insideWall = -2.75f - 0.35f * _explosion;
            var behindBoard = plugOutMapped.Z + 0.35f;
            Route(true, plugOutMapped,
                new(plugOutMapped.X, plugOutMapped.Y, behindBoard),
                new(insideWall, plugOutMapped.Y, behindBoard),
                new(insideWall, lamp.Y - 0.28f, lamp.Z),
                lamp + new Vect(0f, -0.28f, 0f), lamp + new Vect(0f, -0.06f, 0f));
            _plugModels[hops[0].Input].SetDefaultMaterialBaseColor(ForwardColor);
            _plugModels[hops[0].Output].SetDefaultMaterialBaseColor(ForwardColor);
            _plugModels[hops[8].Input].SetDefaultMaterialBaseColor(ReturnColor);
            _plugModels[hops[8].Output].SetDefaultMaterialBaseColor(ReturnColor);
            _rearPlugModels[hops[0].Input].SetDefaultMaterialBaseColor(ForwardColor);
            _rearPlugModels[hops[8].Output].SetDefaultMaterialBaseColor(ReturnColor);
        }
        UpdateRouteTubes();
    }

    private void BuildWires()
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
        var tube = Own(_factory.MeshBuilder.CreateFromVertices(vertices.ToArray(), indices.ToArray()));
        Material CreateWireMaterial(ColorVect color, float glow)
        {
            var colorMap = Own(_factory.TextureBuilder.CreateColorMap(color, includeAlpha: false));
            if (glow <= 0f) return Own(_factory.MaterialBuilder.CreateStandardMaterial(colorMap));
            var emission = Own(_factory.TextureBuilder.CreateEmissiveMap(color, intensity: glow));
            return Own(_factory.MaterialBuilder.CreateStandardMaterial(colorMap, emissiveMap: emission));
        }
        _wireMaterial = CreateWireMaterial(new ColorVect(0.13f, 0.135f, 0.145f), 0f);
        _forwardMaterial = CreateWireMaterial(ForwardColor, 0.35f);
        _returnMaterial = CreateWireMaterial(ReturnColor, 0.35f);
        _forwardHeadMaterial = CreateWireMaterial(new ColorVect(1f, 0.86f, 0.55f), 1f);
        _returnHeadMaterial = CreateWireMaterial(new ColorVect(0.7f, 0.97f, 1f), 1f);
        ModelInstance Wire(Mesh mesh, Material material)
        {
            var instance = Own(_factory.ObjectBuilder.CreateModelInstance(mesh, material,
                initialPosition: new Location(0f, -100f, 0f)));
            _scene.Add(instance);
            _wireModels.Add(instance);
            return instance;
        }
        for (var r = 0; r < 3; r++)
            for (var wire = 0; wire < 26; wire++)
                for (var sample = 0; sample < IdleSamples; sample++)
                    _rotorWires[r, wire, sample] = Wire(tube, _wireMaterial);
        for (var wire = 0; wire < 13; wire++)
            for (var sample = 0; sample < IdleSamples; sample++)
                _reflectorWires[wire, sample] = Wire(tube, _wireMaterial);
        for (var i = 0; i < _routeTubes.Length; i++)
        {
            _routeTubes[i] = Wire(tube, _wireMaterial);
            _routeTubeMaterials[i] = _wireMaterial;
        }
        _pulseHead = Wire(Own(_factory.MeshBuilder.CreateSphere(new Sphere(1f))), _forwardHeadMaterial);
        _pulseLight = Own(_factory.LightBuilder.CreatePointLight(color: ForwardColor,
            brightness: 0f, maxIlluminationRadius: 0.9f));
        _scene.Add(_pulseLight);
    }

    private static void PlaceTube(ModelInstance tube, Location a, Location b, float radius)
    {
        var delta = b - a;
        var length = delta.Length;
        if (length < 0.0001f)
        {
            tube.Position = new Location(0f, -100f, 0f);
            return;
        }
        // A slight overlap closes the seams between consecutive segments on a bend.
        tube.SetTransform(new Transform(translation: (a + delta * 0.5f).AsVect(),
            rotation: new Direction(1f, 0f, 0f) >> delta.Direction,
            scaling: new Vect(length + radius, radius, radius)));
    }

    // Wire behind the pulse takes its direction's color; wire ahead stays gray.
    private void UpdateRouteTubes()
    {
        var used = 0;
        void Tube(Location a, Location b, float radius, Material material)
        {
            if (used >= _routeTubes.Length)
                throw new InvalidOperationException("Signal route exceeded its wire capacity.");
            PlaceTube(_routeTubes[used], a, b, radius);
            if (!Equals(_routeTubeMaterials[used], material))
            {
                _routeTubes[used].SetMaterial(material);
                _routeTubeMaterials[used] = material;
            }
            used++;
        }
        var fraction = _route.Count == 0 ? 1f : PulseFraction(_elapsed);
        var head = fraction * _routeLength;
        Location? headPosition = null;
        var headReturning = false;
        foreach (var segment in _route)
        {
            var length = (segment.End - segment.Start).Length;
            var radius = segment.Cable ? CableWireRadius : WireRadius;
            var lit = segment.Returning ? _returnMaterial : _forwardMaterial;
            if (segment.Arc + length <= head) Tube(segment.Start, segment.End, radius, lit);
            else if (segment.Arc >= head) Tube(segment.Start, segment.End, radius, _wireMaterial);
            else
            {
                var split = segment.Start + (segment.End - segment.Start) * ((head - segment.Arc) / length);
                Tube(segment.Start, split, radius, lit);
                Tube(split, segment.End, radius, _wireMaterial);
            }
            if (headPosition == null && fraction < 1f && head <= segment.Arc + length)
            {
                headPosition = segment.Start + (segment.End - segment.Start) * Math.Clamp((head - segment.Arc) / length, 0f, 1f);
                headReturning = segment.Returning;
            }
        }
        for (var i = used; i < _routeTubes.Length; i++)
            _routeTubes[i].Position = new Location(0f, -100f, 0f);
        if (headPosition is { } position)
        {
            _pulseHead.SetTransform(new Transform(translation: position.AsVect(),
                scaling: new Vect(HeadRadius, HeadRadius, HeadRadius)));
            _pulseHead.SetMaterial(headReturning ? _returnHeadMaterial : _forwardHeadMaterial);
            _pulseLight.Position = position;
            _pulseLight.Color = headReturning ? ReturnColor : ForwardColor;
            _pulseLight.Brightness = 0.05f;
        }
        else
        {
            _pulseHead.Position = new Location(0f, -100f, 0f);
            _pulseLight.Brightness = 0f;
        }
    }

    private static void HideLine(ScenePrimitive primitive) =>
        primitive.SetGeometryPoint(new Location(0f, -100f, 0f), 0f);

    private static void SetLine(ScenePrimitive primitive, Location a, Location b)
    {
        primitive.SetPaintbrush(Inactive);
        primitive.SetGeometryShape(new BoundedRay(a, b), size: 0.0016f, includeEndpoints: false);
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
