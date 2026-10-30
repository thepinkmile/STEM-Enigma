using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Egodystonic.TinyFFR.Avalonia;
using CodeBreakers.Visualization;
using CodeBreakers.ViewModels;

namespace CodeBreakers.Views;

public sealed class MachineLabView : UserControl
{
    private readonly MainViewModel _viewModel;
    private readonly LabMachine _machine;
    private readonly Queue<char> _pending = new();
    private readonly HashSet<Key> _down = [];
    private readonly TinyFfrSceneView _view = new() { MinHeight = 380, Focusable = true };
    private readonly TextBox _positions = new() { Text = "AAA", MaxLength = 3, PlaceholderText = "AAA" };
    private readonly TextBox _rings = new() { Text = "AAA", MaxLength = 3, PlaceholderText = "AAA" };
    private readonly TextBox _plugs = new() { PlaceholderText = "AB CD EF (optional)" };
    private readonly TextBox _message = new() { Text = "HELLOWORLD", PlaceholderText = "A-Z message" };
    private readonly TextBox _input = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 60 };
    private readonly TextBox _output = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 60 };
    private readonly TextBlock _windows = new() { Text = "A  A  A", FontSize = 30, FontFamily = FontFamily.Parse("Consolas"), Foreground = Brush("#F9CB78") };
    private readonly TextBlock _trace = new() { Text = "Press a letter to follow the current.", TextWrapping = TextWrapping.Wrap, FontSize = 13 };
    private readonly TextBlock _status = new() { Text = "Starting renderer...", TextWrapping = TextWrapping.Wrap, Foreground = Brush("#F9CB78") };
    private readonly CheckBox _exploded = new() { Content = "Exploded assembly" };
    private readonly CheckBox _xray = new() { Content = "See inside rotors" };
    private readonly Slider _speed = new() { Minimum = 0.1, Maximum = 3.5, Value = 0.3 };
    private float _smokeKeyDepression;
    private bool _smokeKeyPressed;
    private MachineScene? _scene;
    private IDisposable? _loop;
    private float _smokeTime;
    private int _smokePhase;
    private string _smokeCiphertext = "";
    private bool _failed;

    public static bool SmokeTest { get; set; }
    internal bool IsRendererActive => _scene != null && _loop != null;
    internal bool IsSynchronized => _input.Text == _viewModel.InputText &&
        _output.Text == _viewModel.CypherText &&
        _windows.Text == string.Join("  ", _machine.Positions.ToCharArray());

    public MachineLabView(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        _machine = viewModel.Machine;
        Background = Brush("#0D141F");
        Content = BuildContent();
        Loaded += (_, _) => { if (_scene == null && !_failed) InitializeScene(); };
        Unloaded += (_, _) => ReleaseScene();
        KeyDown += OnKeyDown;
        KeyUp += (_, e) => _down.Remove(e.Key);
        LostFocus += (_, _) => _down.Clear();
        _exploded.IsCheckedChanged += (_, _) => { if (_scene != null) _scene.Exploded = _exploded.IsChecked == true; };
        _xray.IsCheckedChanged += (_, _) => { if (_scene != null) _scene.XRay = _xray.IsChecked == true; };
        _speed.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty && _scene != null) _scene.CycleSeconds = (float)_speed.Value;
        };
    }

    private Control BuildContent()
    {
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(22) };
        var header = new StackPanel { Spacing = 5, Margin = new Thickness(0, 0, 0, 18) };
        header.Children.Add(new TextBlock { Text = "CODE BREAKERS / ENIGMA LAB", FontSize = 13, Foreground = Brush("#69DCE5"), LetterSpacing = 3 });
        header.Children.Add(new TextBlock { Text = "A machine. Millions of possibilities.", FontSize = 28, FontWeight = FontWeight.SemiBold });
        header.Children.Add(Label("Explore the mechanism, separate its parts, and follow a letter through the circuit."));
        root.Children.Add(header);

        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("*,340"), ColumnSpacing = 20 };
        Grid.SetRow(body, 1);
        var viewport = new Border { Background = Brush("#111D2B"), CornerRadius = new CornerRadius(10), ClipToBounds = true, Child = _view };
        body.Children.Add(viewport);

        var controls = new StackPanel { Spacing = 10 };
        controls.Children.Add(Heading("ROTOR WINDOWS  /  I - II - III"));
        controls.Children.Add(_windows);
        controls.Children.Add(_status);
        controls.Children.Add(Label("Right rotor advances before every letter. The middle rotor also double-steps at its notch."));
        controls.Children.Add(_exploded);
        controls.Children.Add(_xray);
        controls.Children.Add(ActionButton("Reset camera", () => _scene?.ResetCamera()));
        foreach (var target in new[] { "Keyboard / lamps", "Rotor lettering", "Stepping arm", "Reflector", "Plugboard", "Plugboard back" })
            controls.Children.Add(ActionButton(target, () => _scene?.Inspect(target)));
        controls.Children.Add(Label("Plugboard: click two unused letters to connect. Click a plugged socket to remove its cable; click the selected socket again to cancel."));
        controls.Children.Add(Heading("SECONDS PER KEY"));
        controls.Children.Add(_speed);
        controls.Children.Add(Heading("SEND A MESSAGE"));
        controls.Children.Add(_message);
        controls.Children.Add(ActionButton("Type into machine", () => Enqueue(_message.Text ?? "")));
        controls.Children.Add(Heading("INPUT"));
        controls.Children.Add(_input);
        controls.Children.Add(Heading("OUTPUT"));
        controls.Children.Add(_output);
        controls.Children.Add(Label("AMBER: toward reflector    CYAN: return"));
        controls.Children.Add(new Expander { Header = "Last signal path", Content = _trace, HorizontalAlignment = HorizontalAlignment.Stretch });
        var settings = new StackPanel { Spacing = 8 };
        settings.Children.Add(Label("Starting windows (left to right)"));
        settings.Children.Add(_positions);
        settings.Children.Add(Label("Ring settings (AAA = 01 01 01)"));
        settings.Children.Add(_rings);
        settings.Children.Add(Label("Plugboard pairs"));
        settings.Children.Add(_plugs);
        settings.Children.Add(ActionButton("Apply settings / reset", ResetMachine));
        controls.Children.Add(new Expander { Header = "Machine settings / reset", Content = settings, HorizontalAlignment = HorizontalAlignment.Stretch });
        var scroll = new ScrollViewer { Content = controls, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        Grid.SetColumn(scroll, 1);
        body.Children.Add(scroll);
        root.Children.Add(body);

        var footer = Label("Right-drag to orbit / Shift-drag to pan / wheel to zoom / click a 3D key or type A-Z with the scene focused.  |  Enigma I - II - III / reflector B. Educational, not a CAD replica.");
        footer.Margin = new Thickness(0, 16, 0, 0);
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);
        return root;
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
    private static TextBlock Label(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Brush("#A1B2C7") };
    private static TextBlock Heading(string text) => new() { Text = text, FontSize = 11, FontWeight = FontWeight.Bold, Foreground = Brush("#69DCE5"), Margin = new Thickness(0, 8, 0, 0) };
    private static Button ActionButton(string text, Action action)
    {
        var button = new Button { Content = text, HorizontalAlignment = HorizontalAlignment.Stretch };
        button.Click += (_, _) => action();
        return button;
    }

    private void InitializeScene()
    {
        try
        {
            if (SmokeTest) Console.WriteLine("SMOKE: initializing scene");
            _scene = new MachineScene();
            if (SmokeTest)
                _scene.KeyMotionChanged += (_, depression) => _smokeKeyDepression = depression;
            _scene.Reset(_machine.Positions, _machine.Rings, _machine.PlugboardPairs);
            _positions.Text = _machine.Positions;
            _rings.Text = _machine.Rings;
            _plugs.Text = string.Join(" ", _machine.PlugboardPairs.Select(pair => $"{(char)('A' + pair.First)}{(char)('A' + pair.Second)}"));
            _input.Text = _viewModel.InputText;
            _output.Text = _viewModel.CypherText;
            _windows.Text = string.Join("  ", _machine.Positions.ToCharArray());
            _trace.Text = "Press a letter to follow the current.";
            _smokeKeyDepression = 0;
            _scene.Exploded = _exploded.IsChecked == true;
            _scene.XRay = _xray.IsChecked == true;
            _scene.CycleSeconds = (float)_speed.Value;
            _scene.KeyPicked += c => Enqueue(c.ToString());
            _scene.PlugPicked += OnPlugPicked;
            _view.Renderer = _scene.Renderer;
            _loop = _scene.Start(_view, Tick);
            _status.Text = "Ready. Click a key to begin.";
            _view.Focus();
            if (SmokeTest && _smokePhase == 0 && _smokeTime == 0)
            {
                Console.WriteLine("SMOKE: typing HELLOWORLD in exploded view");
                _scene.CycleSeconds = 0.3f;
                _exploded.IsChecked = true;
                _xray.IsChecked = true;
                Enqueue("HELLOWORLD");
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            ReportRenderFailure(ex);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox || e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
            e.KeyModifiers.HasFlag(KeyModifiers.Alt) || e.KeyModifiers.HasFlag(KeyModifiers.Meta)) return;
        if (e.Key >= Key.A && e.Key <= Key.Z)
        {
            if (_down.Add(e.Key)) Enqueue(((char)('A' + e.Key - Key.A)).ToString());
            e.Handled = true;
        }
    }

    private void Enqueue(string text)
    {
        if (_scene == null || _failed)
        {
            _status.Text = "The renderer is unavailable. Restart the application after resolving the error.";
            return;
        }
        var letters = text.ToUpperInvariant().Where(c => !char.IsWhiteSpace(c)).ToArray();
        if (letters.Any(c => c is < 'A' or > 'Z'))
        {
            _status.Text = "Use A-Z letters and spaces only; nothing was queued.";
            return;
        }
        if (_pending.Count + letters.Length > 256)
        {
            _status.Text = "The queue holds at most 256 letters. Wait for it to finish.";
            return;
        }
        foreach (var c in letters) _pending.Enqueue(c);
        _status.Text = $"{_pending.Count} letter(s) queued.";
    }

    private void ResetMachine()
    {
        try
        {
            _machine.Configure(_positions.Text ?? "", _rings.Text ?? "", _plugs.Text ?? "");
            _viewModel.NotifyMachineReset();
            _pending.Clear();
            _input.Text = _output.Text = "";
            _viewModel.InputText = _viewModel.CypherText = "";
            _windows.Text = string.Join("  ", _machine.Positions.ToCharArray());
            _trace.Text = "Settings applied. Press a letter.";
            _scene?.Reset(_machine.Positions, _machine.Rings, _machine.PlugboardPairs);
            if (_scene != null) _scene.SelectedPlug = null;
            _smokeKeyDepression = 0;
            _status.Text = "Machine reset. Use the same settings to decrypt.";
        }
        catch (ArgumentException ex)
        {
            _status.Text = ex.Message;
        }
    }

    private void OnPlugPicked(char letter)
    {
        if (_scene == null) return;
        if (_scene.IsAnimating || _pending.Count > 0)
        {
            _status.Text = "Wait for the queued message to finish before changing cables.";
            return;
        }
        var index = letter - 'A';
        var pairs = _machine.PlugboardPairs.ToList();
        var connected = pairs.FindIndex(pair => pair.First == index || pair.Second == index);
        if (connected >= 0)
            pairs.RemoveAt(connected);
        else if (_scene.SelectedPlug == index)
        {
            _scene.SelectedPlug = null;
            _status.Text = "Cable selection cancelled.";
            return;
        }
        else if (_scene.SelectedPlug is not { } first)
        {
            if (pairs.Count == 10)
            {
                _status.Text = "All ten cables are in use. Click a plugged socket to remove a cable first.";
                return;
            }
            _scene.SelectedPlug = index;
            _status.Text = $"{letter} selected. Click another unused socket to connect.";
            return;
        }
        else
            pairs.Add((first, index));

        var text = string.Join(" ", pairs.Select(pair => $"{(char)('A' + pair.First)}{(char)('A' + pair.Second)}"));
        _machine.Configure(_machine.Positions, _machine.Rings, text);
        _viewModel.NotifyMachineReset();
        _plugs.Text = text;
        _scene.SelectedPlug = null;
        _scene.Reset(_machine.Positions, _machine.Rings, _machine.PlugboardPairs);
        _trace.Text = "Plugboard changed. Subsequent letters use the new connections.";
        _status.Text = connected >= 0 ? $"Cable at {letter} removed." : $"Cable connected. {pairs.Count}/10 in use.";
    }

    private void Tick(float dt)
    {
        if (_scene == null || _failed) return;
        if (!_scene.IsAnimating && _pending.TryDequeue(out var letter))
        {
            var stroke = _machine.Press(letter);
            _scene.Play(stroke);
            _input.Text += stroke.Input;
            _output.Text += stroke.Output;
            _viewModel.InputText = _input.Text ?? "";
            _viewModel.CypherText = _output.Text ?? "";
            _windows.Text = string.Join("  ", stroke.After.ToCharArray());
            _trace.Text = string.Join("\n", stroke.Hops.Select(h =>
                $"{h.Stage}: {(char)('A' + h.Input)} > {(char)('A' + h.Output)}"));
            _status.Text = $"{stroke.Input} > {stroke.Output}  |  {_pending.Count} waiting";
        }
        if (SmokeTest) RunSmokeTest(dt);
    }

    private void RunSmokeTest(float dt)
    {
        _smokeTime += dt;
        if (!_smokeKeyPressed && _input.Text == "H" && _smokeKeyDepression >= 0.98f)
        {
            CaptureSmokeFrame("key-pressed");
            _smokeKeyPressed = true;
        }
        if (_smokeTime > 20f) throw new InvalidOperationException("Smoke test timed out.");
        if (_smokePhase == 0 && _pending.Count == 0 && _scene?.IsAnimating == false)
        {
            Console.WriteLine("SMOKE: checking reference vector and assembling");
            if (_output.Text != "ILBDAAMTAZ") throw new InvalidOperationException($"Unexpected ciphertext: {_output.Text}");
            if (!_smokeKeyPressed || _smokeKeyDepression != 0)
                throw new InvalidOperationException("The 3D key did not press and return to rest.");
            if (_viewModel.CypherText != _output.Text || _viewModel.InputText != _input.Text ||
                _viewModel.RightRotor.Position != _machine.Positions[2] - 'A')
                throw new InvalidOperationException("The 3D view is not synchronized with the existing main viewmodel.");
            _scene.VerifyInteraction();
            CaptureSmokeFrame("exploded");
            _scene.Inspect("Stepping arm");
            _scene.VerifyRotorDriveCycle(CaptureSmokeFrame);
            _scene.Inspect("Plugboard");
            _scene.VerifyPlugPicking();
            CaptureSmokeFrame("plugboard-lettering");
            _scene.Inspect("Plugboard back");
            CaptureSmokeFrame("plugboard-back");
            _scene.Inspect("Reflector");
            CaptureSmokeFrame("reflector-translucent-exploded");
            _scene.XRay = false;
            _scene.VerifyReflectorView();
            CaptureSmokeFrame("reflector-solid-exploded");
            _scene.XRay = true;
            _scene.ResetCamera();
            _exploded.IsChecked = false;
            _xray.IsChecked = false;
            _smokePhase = 1;
            _smokeTime = 0f;
        }
        else if (_smokePhase == 1 && _smokeTime > 1.2f)
        {
            Console.WriteLine("SMOKE: checking 3D key picking and contact alignment");
            _scene!.VerifyInteraction();
            _scene.VerifyCameraPan();
            CaptureSmokeFrame("assembled");
            _scene.Inspect("Keyboard / lamps");
            CaptureSmokeFrame("keyboard-lamp-lettering");
            _scene.Inspect("Rotor lettering");
            CaptureSmokeFrame("rotor-lettering");
            _scene.Inspect("Stepping arm");
            CaptureSmokeFrame("stepping-arm");
            _scene.Inspect("Reflector");
            _scene.VerifyReflectorView();
            CaptureSmokeFrame("reflector-solid");
            _scene.XRay = true;
            _scene.VerifyReflectorView();
            CaptureSmokeFrame("reflector-translucent");
            _scene.XRay = false;
            _scene.VerifyReflectorView();
            _scene.ResetCamera();
            _positions.Text = "MCK";
            _rings.Text = "BDF";
            _plugs.Text = "AB CD";
            ResetMachine();
            _scene.VerifyCableFlow();
            ResetMachine();
            Enqueue("TEST");
            _smokePhase = 2;
            _smokeTime = 0f;
        }
        else if (_smokePhase == 2 && _pending.Count == 0 && _scene?.IsAnimating == false)
        {
            Console.WriteLine("SMOKE: checking configured rotor alignment and decryption");
            _scene.VerifyInteraction();
            _scene.Inspect("Plugboard");
            _scene.VerifyPlugPicking();
            CaptureSmokeFrame("connected-plugboard");
            _scene.ResetCamera();
            _smokeCiphertext = _output.Text ?? "";
            if (_smokeCiphertext.Length != 4) throw new InvalidOperationException("Configured input was not processed.");
            ResetMachine();
            Enqueue(_smokeCiphertext);
            _smokePhase = 3;
        }
        else if (_smokePhase == 3 && _pending.Count == 0 && _scene?.IsAnimating == false)
        {
            if (_output.Text != "TEST") throw new InvalidOperationException("Configured decryption failed.");
            Enqueue("BAD!");
            if (_pending.Count != 0) throw new InvalidOperationException("Invalid input was queued.");
            var before = _machine.Positions;
            _positions.Text = "A1A";
            ResetMachine();
            if (_machine.Positions != before) throw new InvalidOperationException("Invalid settings changed the machine.");
            var key = new KeyEventArgs { Key = Key.A };
            OnKeyDown(this, key);
            OnKeyDown(this, key);
            if (_pending.Count != 1) throw new InvalidOperationException("Physical key repeat was not suppressed.");
            _pending.Clear();
            var previousPairs = _machine.PlugboardPairs.ToArray();
            OnPlugPicked('E');
            OnPlugPicked('E');
            if (_scene.SelectedPlug != null) throw new InvalidOperationException("Plugboard selection did not cancel.");
            OnPlugPicked('E');
            OnPlugPicked('F');
            if (!_machine.PlugboardPairs.Contains((4, 5)) || _machine.Positions != before || _output.Text != "TEST")
                throw new InvalidOperationException("Clicking sockets did not connect without changing rotor/text state.");
            OnPlugPicked('E');
            if (!_machine.PlugboardPairs.SequenceEqual(previousPairs))
                throw new InvalidOperationException("Clicking a connected socket did not remove its cable.");
            _machine.Configure(before, _machine.Rings, "AB CD EF GH IJ KL MN OP QR ST");
            _scene.Reset(_machine.Positions, _machine.Rings, _machine.PlugboardPairs);
            OnPlugPicked('U');
            if (_scene.SelectedPlug != null || _machine.PlugboardPairs.Count != 10)
                throw new InvalidOperationException("The ten-cable limit was not enforced.");
            OnPlugPicked('A');
            OnPlugPicked('U');
            OnPlugPicked('V');
            if (!_machine.PlugboardPairs.Contains((20, 21)) || _machine.PlugboardPairs.Count != 10)
                throw new InvalidOperationException("A cable could not be replaced at the limit.");
            _smokePhase = 4;
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                var window = TopLevel.GetTopLevel(this) as Window;
                SmokeTest = false;
                try
                {
                    await this.GetVisualAncestors().OfType<MainView>().Single().VerifyViewToggleAsync();
                    Console.WriteLine("SMOKE PASS: original simulator, view switching, renderer suspend/resume, shared state, GPU frames, 3D picking, contact alignment, assembly modes, settings, decryption and validation.");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    Environment.ExitCode = 1;
                }
                finally
                {
                    ReleaseScene();
                    window?.Close();
                }
            });
        }
    }

    private void CaptureSmokeFrame(string mode)
    {
        var directory = Environment.GetEnvironmentVariable("ENIGMA_SMOKE_CAPTURE");
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
            _scene!.Renderer.Render();
            _scene!.Renderer.CaptureScreenshot(Path.Combine(directory, mode + ".bmp"), captureResolution: (1280, 900));
            using var ui = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)Bounds.Width, (int)Bounds.Height));
            ui.Render(this);
            ui.Save(Path.Combine(directory, mode + "-ui.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }
    }

    private void ReportRenderFailure(Exception ex)
    {
        _failed = true;
        Console.Error.WriteLine(ex);
        _status.Text = $"3D initialization failed: {ex.Message}\nA supported GPU/driver and native TinyFFR dependencies are required. See the CodeBreakers README.";
        ReleaseScene();
        if (SmokeTest) throw new InvalidOperationException("Renderer smoke test failed.", ex);
    }

    private void ReleaseScene()
    {
        _pending.Clear();
        _down.Clear();
        _loop?.Dispose();
        _loop = null;
        _view.Renderer = null;
        _scene?.Dispose();
        _scene = null;
        _smokeKeyDepression = 0;
    }
}
