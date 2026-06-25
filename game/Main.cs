using Godot;
using VocaluxeCore;

namespace Vocaluxe
{
    public partial class Main : Node
    {
        public override void _Ready()
        {
            GD.Print($"VOCALUXE-BOOT core={Smoke.Hello()} runtime={System.Environment.Version}");
        }
    }
}
