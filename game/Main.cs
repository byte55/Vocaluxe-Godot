using Godot;
using VocaluxeCore;

namespace Vocaluxe
{
    public partial class Main : Node
    {
        public override void _Ready()
        {
            // Temporary boot smoke check: proves the net10 game + the VocaluxeCore reference
            // load and run inside Godot. Replaced by the real screens/SingSlice in later phases.
            GD.Print($"VOCALUXE-BOOT toneRange=[{CSettings.ToneMin},{CSettings.ToneMax}] runtime={System.Environment.Version}");
        }
    }
}
