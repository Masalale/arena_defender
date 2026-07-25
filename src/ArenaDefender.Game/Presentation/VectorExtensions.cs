using XnaPoint = Microsoft.Xna.Framework.Point;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using SimVector2 = System.Numerics.Vector2;

namespace ArenaDefender.Presentation;

/// <summary>
/// Converts between the simulation's <see cref="System.Numerics.Vector2"/> and MonoGame's, which
/// is what keeping Core engine independent costs.
/// </summary>
public static class VectorExtensions
{
    public static XnaVector2 ToXna(this SimVector2 value) => new(value.X, value.Y);

    public static XnaVector2 ToXna(this SimVector2 value, XnaVector2 offset) =>
        new(value.X + offset.X, value.Y + offset.Y);

    public static SimVector2 ToNumerics(this XnaVector2 value) => new(value.X, value.Y);

    public static SimVector2 ToNumerics(this XnaPoint value) => new(value.X, value.Y);
}
