using System.Drawing;
using Launcher.Pet;
using Launcher.Pet.Animation;
using Launcher.Pet.Data;
using Launcher.Pet.Hat;
using Launcher.Pet.Sprites;

// Small runnable smoke check alongside the existing behavior trace runner.
internal static class AppearanceChecks
{
    internal static void Check()
    {
        var pet = PetAppearance.Chibi;
        var body = new PetBody { Appearance = pet };
        foreach (int row in new[] { 0, 1, 2, 3, 4 })
        {
            int[] durations = pet.GetFrameDurations(row);
            long elapsed = 0;
            for (int frame = 0; frame < durations.Length; frame++)
            {
                PetFrames.Loop(body, row, elapsed);
                if (body.Frame != frame || pet.GetSourceRectangle(row, frame).X != frame * 362)
                    throw new Exception("The full chibi sequence was not reached.");
                elapsed += durations[frame];
            }
            PetFrames.Loop(body, row, elapsed);
            if (body.Frame != 0) throw new Exception("Chibi loop did not wrap at the full cycle boundary.");
        }
        if (pet.GetJumpFrames(false).Count() != 16 || pet.GetFrameCount(0) != 12 || pet.GetFrameCount(3) != 12)
            throw new Exception("Chibi frame counts changed.");
        foreach (int row in new[] { 5, 9, 10, 11, 12 })
        {
            int count = row == 11 ? 4 : 8;
            if (pet.GetFrameCount(row) != count || pet.GetSourceRectangle(row, count - 1).Y != row * 362)
                throw new Exception("An existing mechanic did not reach its authored chibi poses.");
        }
        for (int row = 6; row <= 8; row++)
            for (int frame = 0; frame < 16; frame++)
                if (pet.GetSourceRectangle(row, frame) != pet.GetSourceRectangle(0, 0)
                    || pet.GetFrameGeometry(row, frame) != pet.GetFrameGeometry(0, 0))
                    throw new Exception("Missing pose did not use the shared idle fallback.");
        if (PetAppearance.Find("unknown") != PetAppearance.Original || PetAppearance.Find(null) != PetAppearance.Original)
            throw new Exception("Unavailable pets must retain a safe default.");

        var world = new PetWorld(new Random(3));
        var environment = new PetEnvironment(Point.Empty, 800, 400, 800, new(0, 0, 800, 650),
            false, Point.Empty, Array.Empty<Rectangle>(), Array.Empty<HatSurface>(), "floor");
        world.Start(0);
        world.Update(0, environment);
        world.SetAppearance(pet, 10);
        var scene = world.Update(10, environment);
        if (scene.Appearance != pet || scene.SpriteBounds.Width != 292 || !world.BeginHatDrag(Point.Empty))
            throw new Exception("Switching lost sprite dimensions or original hat interaction.");
        world.DropHat(false);
        world.SetAppearance(PetAppearance.Original, 20);
        scene = world.Update(20, environment);
        if (scene.Appearance != PetAppearance.Original || scene.SpriteBounds.Width != 149 || scene.HatAttached)
            throw new Exception("Switching back lost the original layout or detached hat state.");
        Console.WriteLine("Pet appearance switching, 12/16-frame timing, idle fallback and hat-state checks passed.");
    }
}
