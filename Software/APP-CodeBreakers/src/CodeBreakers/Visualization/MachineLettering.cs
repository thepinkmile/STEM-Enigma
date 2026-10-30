using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Location = Egodystonic.TinyFFR.Location;

namespace CodeBreakers.Visualization;

internal sealed class MachineLettering(LocalTinyFfrFactory factory) : IDisposable
{
    private const int Resolution = 128;
    private readonly List<IDisposable> _owned = [];
    private readonly Dictionary<(char Letter, bool Dark), Material> _materials = [];

    public Material Material(char letter, bool dark)
    {
        if (_materials.TryGetValue((letter, dark), out var material)) return material;
        var text = new TextBlock
        {
            Text = letter.ToString(), FontFamily = FontFamily.Parse("Georgia"),
            FontSize = 100, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Avalonia.Media.Color.Parse(dark ? "#F3E8C9" : "#161813")),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var control = new Border { Child = text, Width = Resolution, Height = Resolution };
        control.Measure(new Size(Resolution, Resolution));
        control.Arrange(new Rect(0, 0, Resolution, Resolution));
        using var bitmap = new RenderTargetBitmap(new PixelSize(Resolution, Resolution), new Vector(96, 96));
        bitmap.Render(control);
        if (bitmap.Format != Avalonia.Platform.PixelFormat.Bgra8888)
            throw new NotSupportedException($"Unsupported lettering bitmap format: {bitmap.Format}");
        var pixels = new byte[Resolution * Resolution * 4];
        var pinned = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try { bitmap.CopyPixels(new PixelRect(0, 0, Resolution, Resolution), pinned.AddrOfPinnedObject(), pixels.Length, Resolution * 4); }
        finally { pinned.Free(); }
        var inkPixels = Enumerable.Range(0, Resolution * Resolution).Count(i => pixels[i * 4 + 3] > 128);
        if (inkPixels < 200 || inkPixels > Resolution * Resolution / 2)
            throw new InvalidOperationException($"Letter '{letter}' did not rasterize into a readable glyph.");
        var texels = new TexelRgba32[Resolution * Resolution];
        for (var y = 0; y < Resolution; y++)
            for (var x = 0; x < Resolution; x++)
            {
                var p = (y * Resolution + x) * 4;
                var alpha = pixels[p + 3];
                // RenderTargetBitmap is premultiplied; TinyFFR expects straight alpha.
                byte Straight(byte value) => alpha == 0 ? (byte)0 : (byte)Math.Min(255, value * 255 / alpha);
                texels[(Resolution - 1 - y) * Resolution + x] =
                    new(Straight(pixels[p + 2]), Straight(pixels[p + 1]), Straight(pixels[p]), alpha);
            }
        var texture = factory.TextureBuilder.CreateColorMap(
            TexturePattern.GradientHorizontal(ColorVect.WhiteOpaque, ColorVect.WhiteOpaque, resolution: (Resolution, Resolution)),
            includeAlpha: true, TextureCreationConfig.ForColorMap() with { AllowsDynamicWrites = true });
        _owned.Add(texture);
        texture.OverwriteTexels<TexelRgba32>(texels);
        material = factory.MaterialBuilder.CreateLightingIgnoringMaterial(texture);
        _owned.Add(material);
        _materials.Add((letter, dark), material);
        return material;
    }

    public Mesh CreateFace(float width, float height, Vect offset, Rotation rotation = default)
    {
        var normal = Direction.Up * rotation;
        var tangent = new Direction(-1f, 0f, 0f) * rotation;
        var bitangent = new Direction(0f, 0f, 1f) * rotation;
        MeshVertex Vertex(float x, float z, float u, float v) => new(
            Location.Origin + new Vect(x * width, 0f, z * height) * rotation + offset,
            new(u, v), tangent, bitangent, normal);
        MeshVertex[] vertices =
        [
            Vertex(0.5f, -0.5f, 0f, 0f), Vertex(-0.5f, -0.5f, 1f, 0f),
            Vertex(-0.5f, 0.5f, 1f, 1f), Vertex(0.5f, 0.5f, 0f, 1f)
        ];
        var mesh = factory.MeshBuilder.CreateFromVertices(vertices, [new(0, 1, 2), new(0, 2, 3)]);
        _owned.Add(mesh);
        return mesh;
    }

    public void Dispose()
    {
        for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose();
    }
}
