using System.Diagnostics;

using CmlLib.Core;
using CmlLib.Core.Installers;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;



namespace MineSharp
{
	public class Profile
	{
		public string        DisplayName  { get; set; }
		public MinecraftPath Path         { get; set; }
		public IVersion      Version      { get; set; }
		public MLaunchOption LaunchOption { get; set; }
		public ByteProgress  ByteProgress { get; set; }
		public int        ModLoader    { get; set; }

		private MinecraftLauncher Launcher        { get; set; }
		private Process           Process         { get; set; }
		private bool              Running         { get; set; }

		public bool Initialized;
		
		public ProfileJson ProfileJson { get; set; }
		
		public Profile(string displayName, MinecraftPath path, IVersion version, MLaunchOption launchOption, int modLoader, bool install = true)
		{
			DisplayName = displayName;
			Path = path;
			Version = version;
			LaunchOption = launchOption;
			ModLoader = modLoader;

			ProfileJson = new ProfileJson
			{
				DisplayName = displayName,
				Path = path.BasePath,
				Version = version.Id,
				LaunchOption = launchOption,
				ModLoader = modLoader
			};
			
			if (!install)
				return;
			Thread installThread = new Thread(() => Install());
			installThread.Start();
		}
		
		public Profile(string displayName, IVersion version, MLaunchOption launchOption, int modLoader, bool install = true)
		{
			DisplayName = displayName;
			Path = new MinecraftPath($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\profiles\\{displayName}");
			Version = version;
			LaunchOption = launchOption;
			ModLoader = modLoader;

			if (!install)
				return;
			Thread installThread = new Thread(() => Install());
			installThread.Start();
		}

		private async Task Install()
		{
			if (Initialized)
				return;

			Launcher = new MinecraftLauncher(Path);
			Launcher.ByteProgressChanged += (sender, args) => 
			{
				ByteProgress = args;
			};

			string version;
			
			switch (ModLoader)
			{
				case 1:
				{
					FabricInstaller fabric = new FabricInstaller(new HttpClient());
					version = await fabric.Install(Version.Id, Path);
					break;
				}
				default:
				{
					version = Version.Id;
					break;
				}
			}


			Process = await Launcher.InstallAndBuildProcessAsync(version, LaunchOption);
			Initialized = true;
		}

		public void Start()
		{
			if (!Initialized)
			{
				Thread installThread = new Thread(() => Install());
				installThread.Start();
				return;
			}
			Process.Start();
		}

		public void Kill()
		{
			try
			{
				Process.Kill();
			} catch
			{
			}
		}
	}


	public struct ProfileJson
	{
		public string        DisplayName  { get; set; }
		public string        Path         { get; set; }
		public string        Version      { get; set; }
		public MLaunchOption LaunchOption { get; set; }
		public int           ModLoader    { get; set; }
	}
}
