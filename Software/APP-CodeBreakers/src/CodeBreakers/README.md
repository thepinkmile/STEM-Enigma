# Enigma in motion

The 3D interface is built into the existing Code Breakers Avalonia application
from `develop`: the existing desktop entry point, `App`, `MainWindow`, `MainView`,
dependency-injection setup and `CodeBreakers.Enigma` cipher library are reused.
There is no separate application or visualization project. The **Simulator**
tab retains the original controls and opens by default; **3D machine** switches
to the interactive visualization. Both views use the same machine and text state.
Switching away stops rendering and cancels unprocessed queued letters; returning
recreates the scene at the current rotor positions.
The existing `MainViewModel` owns the machine used by the viewport and receives
its input/output; its rotor properties refer to that same engine.

## Run

Install the **.NET 10 SDK**, then run from the repository root:

```powershell
Set-Location Software\APP-CodeBreakers\src
dotnet run --project CodeBreakers.Desktop
```

All projects target .NET 10, including the cipher library, tests and Android/iOS
hosts. The shared UI and hosts use Avalonia 12.1.4; desktop visualization uses
TinyFFR 1.0.0. Mobile hosts retain the original 2D controls; the native 3D view is
desktop-only. Android/iOS builds require the corresponding .NET 10 workloads,
Android SDK or a paired Mac with a compatible Xcode installation.
Android requires API 23 (Android 6.0) or newer for Avalonia 12's AndroidX
dependencies. Android startup uses Avalonia 12's application-level lifetime.
NuGet supplies TinyFFR's
native libraries; a supported graphics device and current driver are required.
Windows x64 rendering has been exercised with the Vulkan backend. TinyFFR also
supports Linux and Apple Silicon macOS, but those platforms have not been
verified for this sample. See [TinyFFR](https://tinyffr.dev/) for platform
requirements. In particular, upstream documents a .NET 9/10 Linux type-loading
issue with some resource types; this sample does not use model bundles or canvas
scenes that trigger it in the upstream examples.

## Explore

- Click a round key in the 3D view or focus the
  viewport and type A-Z. Holding a physical key does not repeat it.
  The model keyboard uses the historical staggered QWERTZ rows and round black
  key faces with pale lettering and metal rims. The 3D keys have raised stems
  and lettering fixed to the key surface. Keys, lamp lenses, rotor alphabet bands
  and plugboard sockets use antialiased Georgia font textures rather than
  thin wire-stroke lettering. Pale-on-black keys and plugboard labels contrast
  with dark-on-ivory lamp and rotor labels. Font textures are generated locally
  once per scene and shared; there are no downloaded font or texture assets.
  Camera buttons provide close-up views of the keyboard/lamps, rotor lettering, stepping arm, reflector,
  and plugboard (best inspected with the case exploded).
  Each encoded letter smoothly presses,
  holds and releases its cap, rim and stem, including letters from queued
  messages. The scene fills the available height without a separate on-screen
  keyboard; the original Simulator view remains unchanged.
  The output bulb has a warm emissive lens, a soft bloom halo and a
  gentle local light on the surrounding panel, keeping its letter readable. Unlit lenses stay pale
  so their letters remain readable; reset extinguishes the output bulb.
- Right-drag to orbit; hold either Shift key and left- or right-drag to pan;
  scroll to zoom. Panning moves in the camera's screen plane and scales with
  zoom distance. Shift-clicks do not press keys or edit cables.
  **Reset camera** restores the inspection
  angle. Camera controls do not consume letters used for encryption.
- **Exploded assembly** smoothly separates the case, lid, keyboard, lamps,
  plugboard, reflector and rotor assemblies. The assembled view has its lid open.
  Changing view does not change the cipher state or interrupt a key cycle.
- **See inside rotors** makes the rotor bodies, gears, and reflector housing
  and face plates translucent; the reflector's brass pins remain solid.
  Wiring and electrical trace lines are shown **only in exploded view**,
  regardless of rotor transparency. All 26 rotor wires and the reflector B's
  13 paired connections are drawn there; the active forward route becomes amber and the
  return route cyan, progressively, after the gears finish stepping. The input
  key moves down and the output lamp lights in either view. The last route stays
  visible in exploded view until the next key or reset, so it can be inspected.
- Enter a message and choose **Type into machine**. Whitespace is ignored.
  Invalid characters reject the whole message; up to 256 letters may be queued.
  Each letter completes its animation before the next begins.
- Expand **Machine settings / reset** for the three starting window letters,
  three ring letters, and up to ten disjoint plugboard pairs, e.g. `AB CD EF`.
  Settings take effect only when **Apply settings / reset** is pressed; that
  also clears the queue, text and highlighted route. Invalid settings leave the
  running machine untouched.
  Each configured pair also inserts two dark plugs and a curved physical cable
  into the front plugboard. These cables remain visible in assembled and
  exploded views; only the teaching electrical-trace overlays are exploded-only.
  Clearing the pairs removes the cables. Use **Plugboard** for a close-up.
  You can also click two unused sockets (or their letters) directly in the 3D
  scene to add a cable. The first socket is highlighted green; click it again
  to cancel. Click an occupied socket or its plug to remove that cable.
  Click edits apply immediately to subsequent input without changing rotor
  positions or clearing text, and update the settings field. Finish any queued
  message before editing; at most ten cables can be connected.
- Expand **Last signal path** for the letter entering and leaving every stage.
  The input/output boxes can be selected and copied. Typing in an editor does
  not also operate the machine.

Rotors are **I - II - III from left to right**, with reflector **B**. The right
rotor steps before encoding; its V notch advances the middle rotor, and the
middle rotor's E notch produces the double step. Ring settings offset the wiring,
not the displayed turnover notch. Starting at `AAA`, with rings `AAA` and no
plugboard, `HELLOWORLD` produces `ILBDAAMTAZ`. Reset to the same settings and
type the ciphertext to decrypt.

These are original, stylized models, not dimensionally accurate museum or
manufacturing assets. Parts are deliberately oversized for teaching. Rotor
wheels use dark cores, metal alphabet bands and brass toothed rings;
the reflector has a silver cylindrical casing, dark contact face, 26 brass
pins, a central hub, slotted fasteners and a retaining handle inspired by
the reference photograph.
the housing has deeper wooden walls, contrasting edge rails, subtle grain
strips, a front latch and lid hinges. The open lid has a dark inset lining
and an educational ENIGMA plaque. The three staggered rows of brass-mounted
lamp lenses remain unobstructed: the tall rotor side supports and covered
battery box and broad black rotor base plate are omitted.
The case is deepened to leave clear gaps around the lampboard, with the
keyboard toward the front edge and the rotor assembly farther to the rear.
Solid rotors are shown by default; **See inside rotors** enables the electrical cutaway. These details separate
with their parent assemblies in the exploded view.

Rotor wires represent the actual permutations. Exploded signal traces attach
to the bottom of the moving key stem, run beneath the keyboard and lampboard,
and use separated perimeter lanes to reach the rotor assembly without crossing
the keys. These bent teaching routes and the exploded spacing are schematic.
Plugboard swaps follow the same curved geometry as the connected physical cable,
in either direction. Scrolling emissive textures carry amber/cyan pulses through
translucent wires at 0.9 world units per second, with 2.25 units between pulses.
Arc-length texture coordinates keep their speed and phase consistent through
short segments and bends, including reversed cable traversal. The brighter
pulse stays within the wire diameter; physical cords have a translucent sleeve
around the smaller signal core. Soft bloom remains enabled so the output lamp
stands out clearly. Signal textures are exploded-only and do not block picking.
Incoming traces terminate at labeled rear plugboard terminals; outgoing traces
leave from the front sockets. Rear terminals and their letters are shown only
in exploded view. Use **Plugboard back** to inspect them.
The rotor gears follow the real stepping decisions. A pivoting brass rocker and
nickel pawl beside the right-hand (fast) rotor animate on every key press:
the rocker lifts, the hooked pawl follows one tooth through the rotor's advance,
then lifts clear and returns with the key release. Mounts and pivot pins stay
connected in both assembly modes. Use **Stepping arm** for a close-up.
This is a schematic single-rotor drive, not a full mechanical simulation;
the other rotors' pawls, the full keyboard linkage, batteries
and historically accurate internal wire curves are not simulated. The labels
are screen-facing teaching aids, not physical engravings. The sample uses
TinyFFR's shaded default material rather than photorealistic PBR textures.

## Implementation

| File | Responsibility |
| --- | --- |
| `..\CodeBreakers.Enigma\EnigmaMachine.cs` | Existing GPU-independent cipher, now also producing immutable per-key electrical traces |
| `Visualization\LabMachine.cs` | Validates settings and delegates encryption to the existing shared engine |
| `Visualization\MachineScene.cs` | Shared meshes, instances, wire primitives, stepped rotation, explosion interpolation, inspection camera and picking |
| `Views\MainView.axaml.cs`, `Views\MachineLabView.cs` | Existing main view hosts the desktop 3D controls, bounded input queue, settings validation and animation sequencing |
| `Assets\Models` | Thirteen original reusable OBJ meshes and their coordinate/provenance notes |
| `..\..\tools\generate_models.py` | Deterministic, dependency-free mesh generation and geometry checks |

Avalonia owns the UI thread. A headless `LocalTinyFfrFactory`, bindable renderer,
and `StartAvaloniaUiLoop` keep all TinyFFR access on that same thread. Assets are
loaded once from `AppContext.BaseDirectory`, shared across instances, and copied
to both build and publish outputs. There are no runtime asset downloads.
Wire and label primitives are reused rather than recreated on every frame.

Picking uses `CreateRayFromRenderSurface` and the scene's CPU bounding-volume
query. This intentionally allows the small bounding-box corners around a round
key to count as a click. TinyFFR's GPU pixel-picking path caused a native
driver-level termination on the test machine; CPU picking avoids that path
entirely, not by catching and hiding an error.

On shutdown the UI loop stops, the scene view is unbound, scene membership is
cleared, and owned resources are disposed in reverse creation order before
disposing the factory. Initialization errors appear in the sidebar and stderr,
rather than leaving an unexplained blank viewport.

## TinyFFR documentation and sample review

Review snapshot: upstream commit
[`90fb1aebf7d4bfd595a7ee77ea15b81a0b32d0fd`](https://github.com/Egodystonic/TinyFFR/tree/90fb1aebf7d4bfd595a7ee77ea15b81a0b32d0fd).
The documentation catalogue contains 98 Markdown pages, including eight
tutorials. The reference survey covered the math/coordinate conventions;
windows, input and loops; all UI integrations and camera-controller styles;
asset import, baking and asynchronous loading; materials, textures and lighting;
scene objects, primitives and queries; rendering, compositing and capture;
skeletal/dynamic geometry; memory, threading, performance and resource ownership.
The relevant APIs were then checked against the source and used in this sample.

| Upstream sample | Relevant finding / decision |
| --- | --- |
| **HelloCube** | Minimal factory/scene/camera/render lifecycle and delta-time animation |
| **LoadingAssets** | Mesh sharing and inspection-camera controls; use original small OBJ parts rather than a large downloaded bundle |
| **UserInput** | Press-versus-held semantics; keep text input separate from camera motion |
| **CanvasScenes** | Layered 2D UI and resource cleanup; use existing Avalonia controls instead of a second canvas overlay |
| **SceneProgrammer** | Bindable Avalonia rendering, dispatcher loop, model transforms and selection; use headless mode because there is no standalone playback window |
| **EquationPlotter** | Reusable instances, dynamic geometry and scene labels; rigid Enigma parts need transforms, not mutable vertex buffers or ImGui |
| **VoxelWorld** | Reuse resources and keep GPU work on the primary thread; background terrain streaming is unnecessary for this small fixed assembly |

Important references:
[Avalonia integration](https://tinyffr.dev/reference/avalonia_integration/),
[application flow](https://tinyffr.dev/tutorials/application_flow/),
[conventions](https://tinyffr.dev/reference/conventions/),
[loading meshes](https://tinyffr.dev/reference/loading_meshes/),
[default material](https://tinyffr.dev/reference/the_default_material/),
[scene primitives](https://tinyffr.dev/reference/scene_primitives/),
[scene queries](https://tinyffr.dev/reference/scenes/),
[inspector camera](https://tinyffr.dev/reference/camera_controller_inspector/),
[resource dependencies](https://tinyffr.dev/reference/resource_dependencies/),
and [render capture](https://tinyffr.dev/reference/capturing_render_output/).

Notably, TinyFFR names **+X Left**, not Right. The assembly's left-to-right
placement and the rotor's numeric +X rotation axis account for this explicitly,
so rotating contacts stay attached to their highlighted wires.

## Verification

From `Software\APP-CodeBreakers\src`:

```powershell
dotnet build CodeBreakers.Desktop -c Release
dotnet test CodeBreakers.Enigma.Tests -c Release
dotnet run --project CodeBreakers.Desktop -c Release -- --smoke-test
python ..\tools\generate_models.py
```

The last command is optional regeneration and validation of the checked-in
meshes. It needs Python 3.10+ on `PATH` (standard library only); Python is not
needed to build or run the application.

The existing cipher test project also covers the new trace API and settings adapter without referencing a renderer,
so they require neither a display nor a GPU. They cover known vectors,
reciprocity, ring/plugboard combinations, trace continuity and local contacts,
rollover, double stepping, lowercase input and rejected inputs/settings.

The opt-in smoke test opens a real window and checks native rendering,
assembled/exploded and translucent/solid modes, 3D key picking, mesh/contact
rotation alignment, queue processing, configured decryption, invalid input and
physical key-repeat suppression, then closes. It needs a graphical desktop.
To save BMP scene captures and PNG UI captures, set `ENIGMA_SMOKE_CAPTURE` to
an output directory before running it. Failure exits unsuccessfully rather
than reporting a pass.
