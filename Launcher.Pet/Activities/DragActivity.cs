using System.Drawing;
using Launcher.Pet.Animation;
using Launcher.Pet.Data;

namespace Launcher.Pet.Activities;

internal sealed class DragActivity : PetActivity
{
    internal static bool TryStart(PetActivityContext c, Point cursor)
    {
        var actor = c.Actor;
        if (actor.Location != PetLocation.Desktop || !actor.Activity.AllowsPetDrag
            || c.Environment.Ruins is null || !actor.Navigation.Initialized)
            return false;
        var drag = new DragActivity(c, cursor);
        actor.Navigation.CancelRoute();
        actor.ContinueWith(drag, c.Now);
        actor.Present(PetMode.Dragging);
        c.Body.Row = PetAnimationCatalog.DragRow;
        c.Body.Frame = 0;
        actor.Speech.Reset(c.Now);
        drag.Move(c, cursor);
        return true;
    }

    private readonly float _offsetX, _offsetY;
    internal DragActivity(PetActivityContext c, Point cursor)
    {
        _offsetX = cursor.X - c.Environment.AreaScreenPosition.X - c.Body.X - c.HalfWidth;
        _offsetY = cursor.Y - c.Environment.AreaScreenPosition.Y - c.Actor.Navigation.FootY;
    }
    internal override PetMode Mode => PetMode.Dragging;
    internal override bool IsDragging => true;
    internal override bool AllowsEarthquake => false;
    internal override bool ShowsRoute => false;
    internal override void Update(PetActivityContext c)
    {
        c.Actor.Present(Mode);
        c.Body.Row = PetAnimationCatalog.DragRow;
        c.Body.Frame = (int)((c.Now - StartedAtMs) / 180 % 2);
    }
    internal override void Move(PetActivityContext c, Point cursor)
    {
        if (c.Environment.Ruins is not { } scene) return;
        c.Body.X = Math.Clamp(cursor.X - c.Environment.AreaScreenPosition.X - _offsetX - c.HalfWidth, 0, Math.Max(0, scene.Size.Width - c.HalfWidth * 2));
        c.Actor.Navigation.FootY = Math.Clamp(cursor.Y - c.Environment.AreaScreenPosition.Y - _offsetY,
            PetLogicalGeometry.Height * c.Environment.Scale, scene.Size.Height);
        c.Body.JumpLift = c.Environment.PetZoneTopY + PetLogicalGeometry.Height - c.Actor.Navigation.FootY;
    }
    internal override void Drop(PetActivityContext c)
    {
        c.Actor.ContinueWith(new FlightActivity(dropped: true), c.Now);
        c.Actor.Present(PetMode.Falling);
        c.Body.Row = PetAnimationCatalog.DragRow;
        c.Body.Frame = 2;
    }
}
