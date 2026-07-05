using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// <see cref="IRenderer"/> over an HTML canvas, driven by <c>wwwroot/renderer.js</c> through
/// <see cref="RendererInterop"/>.
///
/// <para>Every primitive issued between <see cref="BeginFrame"/> and <see cref="EndFrame"/> is
/// appended, opcode-tagged, to a single growable <c>float[]</c> (see the encoding documented on
/// <c>docs/m2-spec.md</c>: header, then <c>OP_POLYLINE</c>/<c>OP_CIRCLE</c> records). Text is rare
/// (~10 labels/frame) so it is batched separately as JSON rather than packed into the float buffer.
/// <see cref="EndFrame"/> flushes both in exactly two interop calls — never one per primitive, which
/// matters at 500+ polylines/frame.</para>
/// </summary>
public sealed class CanvasRenderer : IRenderer
{
    private const float OpPolyline = 1f;
    private const float OpCircle = 2f;
    private const float OpPolygon = 3f;

    private readonly string _canvasId;
    private float[] _buffer = new float[8192];
    private int _length;
    private readonly List<TextCommand> _texts = [];

    public CanvasRenderer(string canvasId)
    {
        _canvasId = canvasId;
    }

    public void BeginFrame(int widthPx, int heightPx, RgbaColor background)
    {
        _length = 0;
        _texts.Clear();

        EnsureCapacity(6);
        _buffer[_length++] = widthPx;
        _buffer[_length++] = heightPx;
        _buffer[_length++] = background.R;
        _buffer[_length++] = background.G;
        _buffer[_length++] = background.B;
        _buffer[_length++] = background.A;
    }

    public void DrawCircle(float xPx, float yPx, float radiusPx, RgbaColor? fill, RgbaColor stroke, float strokeWidthPx = 1f)
    {
        RgbaColor f = fill ?? default;

        EnsureCapacity(14);
        _buffer[_length++] = OpCircle;
        _buffer[_length++] = fill.HasValue ? 1f : 0f;
        _buffer[_length++] = f.R;
        _buffer[_length++] = f.G;
        _buffer[_length++] = f.B;
        _buffer[_length++] = f.A;
        _buffer[_length++] = stroke.R;
        _buffer[_length++] = stroke.G;
        _buffer[_length++] = stroke.B;
        _buffer[_length++] = stroke.A;
        _buffer[_length++] = strokeWidthPx;
        _buffer[_length++] = xPx;
        _buffer[_length++] = yPx;
        _buffer[_length++] = radiusPx;
    }

    public void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float widthPx = 1f)
    {
        if (pointsXY.Length == 0 || pointsXY.Length % 2 != 0)
        {
            return;
        }

        int n = pointsXY.Length / 2;

        EnsureCapacity(7 + pointsXY.Length);
        _buffer[_length++] = OpPolyline;
        _buffer[_length++] = stroke.R;
        _buffer[_length++] = stroke.G;
        _buffer[_length++] = stroke.B;
        _buffer[_length++] = stroke.A;
        _buffer[_length++] = widthPx;
        _buffer[_length++] = n;

        pointsXY.CopyTo(_buffer.AsSpan(_length));
        _length += pointsXY.Length;
    }

    public void DrawPolygon(ReadOnlySpan<float> pointsXY, RgbaColor? fill, RgbaColor stroke, float strokeWidthPx = 1f)
    {
        if (pointsXY.Length < 6 || pointsXY.Length % 2 != 0)
        {
            return; // fewer than 3 points isn't a polygon
        }

        RgbaColor f = fill ?? default;
        int n = pointsXY.Length / 2;

        EnsureCapacity(12 + pointsXY.Length);
        _buffer[_length++] = OpPolygon;
        _buffer[_length++] = fill.HasValue ? 1f : 0f;
        _buffer[_length++] = f.R;
        _buffer[_length++] = f.G;
        _buffer[_length++] = f.B;
        _buffer[_length++] = f.A;
        _buffer[_length++] = stroke.R;
        _buffer[_length++] = stroke.G;
        _buffer[_length++] = stroke.B;
        _buffer[_length++] = stroke.A;
        _buffer[_length++] = strokeWidthPx;
        _buffer[_length++] = n;

        pointsXY.CopyTo(_buffer.AsSpan(_length));
        _length += pointsXY.Length;
    }

    public void DrawText(float xPx, float yPx, string text, RgbaColor color, string font = "12px sans-serif", TextAlign align = TextAlign.Left)
    {
        string alignJs = align switch
        {
            TextAlign.Center => "center",
            TextAlign.Right => "right",
            _ => "left",
        };

        _texts.Add(new TextCommand(xPx, yPx, text, color.R, color.G, color.B, color.A, font, alignJs));
    }

    public void EndFrame()
    {
        Span<byte> bytes = MemoryMarshal.AsBytes(_buffer.AsSpan(0, _length));
        RendererInterop.DrawFrame(_canvasId, bytes, _length);

        string json = _texts.Count == 0
            ? "[]"
            : System.Text.Json.JsonSerializer.Serialize(_texts, RendererJsonContext.Default.ListTextCommand);
        RendererInterop.DrawTexts(_canvasId, json);
    }

    private void EnsureCapacity(int extra)
    {
        if (_length + extra <= _buffer.Length)
        {
            return;
        }

        int newSize = Math.Max(_buffer.Length * 2, _length + extra);
        Array.Resize(ref _buffer, newSize);
    }
}

/// <summary>One text label. Kept out of the float command buffer — see <see cref="IRenderer.DrawText"/>.</summary>
internal sealed record TextCommand(float X, float Y, string Text, byte R, byte G, byte B, byte A, string Font, string Align);

/// <summary>
/// Source-generated JSON serialization for <see cref="TextCommand"/> — avoids reflection-based
/// <c>System.Text.Json</c> (trim-unsafe and slower) for a payload sent once per frame.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<TextCommand>))]
internal sealed partial class RendererJsonContext : JsonSerializerContext;
