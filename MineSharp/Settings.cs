using CmlLib.Core;
using CmlLib.Core.Version;



namespace MineSharp
{
	public static class Settings
	{
		public static MinecraftPath               Path               { get; set; }
		public static MinecraftLauncherParameters LauncherParameters { get; set; }
		public static MinecraftLauncher           Launcher           { get; set; }
		public static MinecraftVersion[]          Versions           { get; set; }
	}
}
