using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using CGUI;
using CGUI.Tools;

using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;
using CmlLib.Core.VersionMetadata;

using Microsoft.Identity.Client;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using XboxAuthNet.Game.Accounts;
using XboxAuthNet.Game.Authenticators;
using XboxAuthNet.Game.Msal;
using XboxAuthNet.Game.Msal.OAuth;



namespace MineSharp
{
	internal class Program
	{
		private static int                      PageIndex = 0;
		private static uint                     frameCount        { get; set; } = 0;
		private static List<Profile>            Profiles          { get; set; } = new List<Profile>();
		private static MinecraftLauncher        Launcher          { get; set; } = new MinecraftLauncher();
		private static MSession?                Session           { get; set; }
		private static IPublicClientApplication Application       { get; set; }
		private static List<IVersion>           SupportedVersions { get; set; } = new List<IVersion>();
		private static int                      SelectedInstance = -1;

		private static void Main(string[] args)
		{
			Console.InputEncoding = Encoding.UTF8;
			Console.OutputEncoding = Encoding.UTF8;
			
			//LanguageSetter.SetLanguage("de-DE");
			
			Console.CursorVisible = false;
			Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
			Input.Initialize();
			CURSOR.Initialize();
			Console.Title = "MineSharp  -  Public Beta";
			Console.Clear();
			CURSOR.Shn = false;

			InitialSetup();
			LoadProfiles();

			while (true)
			{
				switch (PageIndex)
				{
					case 0:   PId_0(); break;
					case 101: PId_101(); break;
					case 102: PId_102(); break;
				}
			}
		}

		static void InitialSetup()
		{
			//PInvoke.MessageBoxEx(HWND.Null,
			//	AppResources.MessageBox_Introduction_Text,
			//	AppResources.MessageBox_Introduction_Titile,
			//	MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONINFORMATION | MESSAGEBOX_STYLE.MB_DEFBUTTON1 | MESSAGEBOX_STYLE.MB_TASKMODAL | MESSAGEBOX_STYLE.MB_TOPMOST,
			//	0);
			
			if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp"))
			{
				Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp");
			}
			if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\profiles"))
			{
				Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\profiles");
			}
			if (!File.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\profiles.json"))
			{
				File.Create($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\profiles.json").Close();
				JObject jProfiles = new JObject();
				jProfiles.Add("profiles", new JArray());
				File.WriteAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\profiles.json", jProfiles.ToString());
			}
			if (!File.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\settings.json"))
			{
				File.Create($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\settings.json").Close();
				JObject jSettings = new JObject();
				jSettings.Add("accounts", new JArray());
				File.WriteAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\settings.json", jSettings.ToString());
			}
			
			Application = MsalClientHelper.BuildApplicationWithCache("3014b765-5754-450d-aa26-1a863ce206d3").GetAwaiter().GetResult();
		}

		static void LoadProfiles()
		{
			JObject jProfiles = JObject.Parse(File.ReadAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\profiles.json"));
			JArray profiles = (JArray)jProfiles["profiles"];
			foreach (JObject jProfile in profiles)
			{
				Profiles.Add(new Profile(
					jProfile["displayName"].ToObject<string>(),
					new MinecraftPath(jProfile["path"].ToObject<string>()),
					Launcher.GetVersionAsync(jProfile["version"].ToObject<string>()).GetAwaiter().GetResult(),
					jProfile["launchOption"].ToObject<MLaunchOption>(),
					jProfile["modLoader"].ToObject<int>()
				));
			}
		}

		async static Task Authenticate()
		{
			PInvoke.MessageBoxEx(HWND.Null,
				AppResources.Account_SignIn_TempCracked_Text,
				AppResources.Account_SignIn_TempCracked_Title,
				MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONINFORMATION | MESSAGEBOX_STYLE.MB_DEFBUTTON1 | MESSAGEBOX_STYLE.MB_TASKMODAL | MESSAGEBOX_STYLE.MB_TOPMOST,
				0);
			return;
			
			JELoginHandler loginHandler = JELoginHandlerBuilder.BuildDefault();
			NestedAuthenticator authenticator = loginHandler.CreateAuthenticatorWithNewAccount();
			authenticator.AddMsalOAuth(Application, msal => msal.SystemBrowser());
			authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
			authenticator.AddJEAuthenticator();
			Session = authenticator.ExecuteForLauncherAsync().GetAwaiter().GetResult();
			Debug.Assert(Session != null);
		}


		static void PId_0()
		{
			// Top Toolbar
			Button button_Instance_Create = new Button
			{
				Position = new Point(0, 0), Size = new Size(20, 3),
				Text = AppResources.Instance_Create, LineWrap = false,
				Visible = true, Enabled = true, newDesign = true
			};
			Button button_Instance_StartRandom = new Button
			{
				Position = new Point(20, 0), Size = new Size(20, 3),
				Text = AppResources.Instance_StartRandom, LineWrap = false,
				Visible = true, Enabled = true
			};
			Button button_Settings = new Button
			{
				Position = new Point(40, 0), Size = new Size(20, 3),
				Text = AppResources.Settings, LineWrap = false,
				Visible = true, Enabled = true
			};

			MenuBar menuBar_Top = new MenuBar
			{
				Position = new Point(0, 0), Size = new Size(Console.BufferWidth, 1),
				Elements = new MenuBar.Element[]
				{
					new MenuBar.Element
					{
						Childreen = new MenuBar.Element[0],
						Highlight = 0U,
						Text = AppResources.Instance_Create
					},
					new MenuBar.Element
					{
						Childreen = new MenuBar.Element[0],
						Highlight = 0U,
						Text = AppResources.Instance_StartRandom
					},
					new MenuBar.Element
					{
						Childreen = new MenuBar.Element[0],
						Highlight = 0U,
						Text = AppResources.Settings
					},
					new MenuBar.Element
					{
						Childreen = new MenuBar.Element[0],
						Highlight = 0U,
						Text = AppResources.Account
					}
				}
			};

			Button button_Account = new Button
			{
				Position = new Point(Console.WindowWidth - 20, 0), Size = new Size(20, 3),
				Text = AppResources.Account_SignIn, LineWrap = false,
				Visible = true, Enabled = true
			};
			
			
			List<Button> buttons_Instances = new List<Button>();
			Point pos = new Point(2 + 21 * (buttons_Instances.Count % 4), 0);
			foreach (Profile profile in Profiles)
			{
				pos = pos with
				{
					X = 2 + 21 * (buttons_Instances.Count % 4),
					Y = buttons_Instances.Count % 4 == 0 ? pos.Y + 4 : pos.Y
				};
				buttons_Instances.Add(new Button
				{
					Position = new Point(pos.X, pos.Y), Size = new Size(19, 3),
					Text = profile.DisplayName, LineWrap = false,
					Visible = true, Enabled = true
				});
			}
			
			
			Button button_Instance_Start = new Button
			{
				Position = new Point(Console.WindowWidth - 25, 4), Size = new Size(25, 1),
				Text = AppResources.Instance_Start, LineWrap = false,
				Visible = true, Enabled = true
			};
			Button button_Instance_Stop = new Button
			{
				Position = new Point(Console.WindowWidth - 25, 6), Size = new Size(25, 1),
				Text = AppResources.Instance_Stop, LineWrap = false,
				Visible = true, Enabled = true
			};
			Button button_Instance_Edit = new Button
			{
				Position = new Point(Console.WindowWidth - 25, 8), Size = new Size(25, 1),
				Text = AppResources.Instance_Edit, LineWrap = false,
				Visible = true, Enabled = true
			};
			Button button_Instance_OpenFolder = new Button
			{
				Position = new Point(Console.WindowWidth - 25, 10), Size = new Size(25, 1),
				Text = AppResources.Instance_OpenFolder, LineWrap = false,
				Visible = true, Enabled = true
			};
			Button button_Instance_Delete = new Button
			{
				Position = new Point(Console.WindowWidth - 25, 12), Size = new Size(25, 1),
				Text = AppResources.Instance_Delete, LineWrap = false,
				Visible = true, Enabled = true
			};

			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
			CancellationToken token = cancellationTokenSource.Token;
			
			Thread drawingThread = new Thread(() =>
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.BackgroundColor = ConsoleColor.Black;
				Console.Clear();
				while (!token.IsCancellationRequested)
				{
					button_Instance_Create.Draw().GetAwaiter().GetResult();
					button_Instance_StartRandom.Draw().GetAwaiter().GetResult();
					//button_Settings.Draw().GetAwaiter().GetResult();
					button_Account.Draw().GetAwaiter().GetResult();
					
					//menuBar_Top.Draw().GetAwaiter().GetResult();

					button_Instance_Start.Draw().GetAwaiter().GetResult();
					button_Instance_Stop.Draw().GetAwaiter().GetResult();
					button_Instance_Edit.Draw().GetAwaiter().GetResult();
					button_Instance_OpenFolder.Draw().GetAwaiter().GetResult();
					button_Instance_Delete.Draw().GetAwaiter().GetResult();

					for (int i = 0; i < buttons_Instances.Count; i++)
						buttons_Instances[i].Draw().GetAwaiter().GetResult();
				}
			});
			drawingThread.Start();

			bool running = true;
			while (running)
			{
				Input.Update();
				Input.Mouse.Update();

				CURSOR.Tick(ConsoleKey.None);
				
				if (Session != null)
					Debug.Write("\r" + Session.Username);

				button_Instance_Create.Update();
				if ((button_Instance_Create.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
					button_Instance_Create.InteractionTags &= ~(uint)InteractionTag.Selected;
					PageIndex = 101;
					running = false;
				}
				//button_Instance_Create.Draw();

				button_Instance_StartRandom.Update();
				if ((button_Instance_StartRandom.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
					button_Instance_StartRandom.InteractionTags &= ~(uint)InteractionTag.Selected;
					if (Profiles.Count > 0)
					{
						List<int> profileIds = new List<int>();
						for (int i = 0; i < Profiles.Count; i++)
						{
							if (Profiles[i].Initialized)
								profileIds.Add(i);
						}
						int profileId = Random.Shared.Next(0, profileIds.Count);
						Profiles[profileIds[profileId]].Start();
					}
				}
				//button_Instance_StartRandom.Draw();

				//button_Settings.Update();
				if ((button_Settings.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
					button_Settings.InteractionTags &= ~(uint)InteractionTag.Selected;
					PInvoke.MessageBoxEx(HWND.Null,
						AppResources.Message_NotImplemented_Text,
						AppResources.Message_NotImplemented_Title,
						MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONINFORMATION | MESSAGEBOX_STYLE.MB_DEFBUTTON1 | MESSAGEBOX_STYLE.MB_TASKMODAL | MESSAGEBOX_STYLE.MB_TOPMOST,
						0);
				}
				//button_Settings.Draw();

				button_Account.Update();
				if ((button_Account.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
					button_Account.InteractionTags &= ~(uint)InteractionTag.Selected;
					button_Account.Enabled = false;
					Authenticate();
				}
				//button_Account.Draw();
				
				//menuBar_Top.Update();
				if ((menuBar_Top.Elements[0].InteractionTags & (uint)InteractionTag.Selected) != 0)
				{ // Instance_Create
					menuBar_Top.Elements[0].InteractionTags &= ~(uint)InteractionTag.Selected;
					PageIndex = 101;
					running = false;
				}
				if ((menuBar_Top.Elements[1].InteractionTags & (uint)InteractionTag.Selected) != 0)
				{ // Instance_StartRandom
					menuBar_Top.Elements[1].InteractionTags &= ~(uint)InteractionTag.Selected;
					if (Profiles.Count > 0)
					{
						List<int> profileIds = new List<int>();
						for (int i = 0; i < Profiles.Count; i++)
						{
							if (Profiles[i].Initialized)
								profileIds.Add(i);
						}
						SelectedInstance = Random.Shared.Next(0, profileIds.Count);
						Profiles[profileIds[SelectedInstance]].Start();
					}
				}
				if ((menuBar_Top.Elements[2].InteractionTags & (uint)InteractionTag.Selected) != 0)
				{ // Settings
					menuBar_Top.Elements[2].InteractionTags &= ~(uint)InteractionTag.Selected;
					PInvoke.MessageBoxEx(HWND.Null,
						AppResources.Message_NotImplemented_Text,
						AppResources.Message_NotImplemented_Title,
						MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONINFORMATION | MESSAGEBOX_STYLE.MB_DEFBUTTON1 | MESSAGEBOX_STYLE.MB_TASKMODAL | MESSAGEBOX_STYLE.MB_TOPMOST,
						0);
				}
				if ((menuBar_Top.Elements[3].InteractionTags & (uint)InteractionTag.Selected) != 0)
				{ // Account
					menuBar_Top.Elements[3].InteractionTags &= ~(uint)InteractionTag.Selected;
					Authenticate();
				}



				button_Instance_Start.Update();
				if ((button_Instance_Start.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
					button_Instance_Start.InteractionTags &= ~(uint)InteractionTag.Selected;
					if (SelectedInstance >= 0)
						Profiles[SelectedInstance].Start();
				}
				//button_Instance_Start.Draw();
				
				button_Instance_Stop.Update();
				if ((button_Instance_Stop.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
					button_Instance_Stop.InteractionTags &= ~(uint)InteractionTag.Selected;
					if (SelectedInstance >= 0)
						Profiles[SelectedInstance].Kill();
				}
				//button_Instance_Stop.Draw();
				
				button_Instance_Edit.Update();
				if ((button_Instance_Edit.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
					button_Instance_Edit.InteractionTags &= ~(uint)InteractionTag.Selected;
					if (SelectedInstance >= 0)
					{
						PageIndex = 102;
						running = false;
					}
				}
				//button_Instance_Edit.Draw();
				
				button_Instance_OpenFolder.Update();
				if ((button_Instance_OpenFolder.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
					button_Instance_OpenFolder.InteractionTags &= ~(uint)InteractionTag.Selected;
					if (SelectedInstance >= 0) {
						Process.Start("explorer", Profiles[SelectedInstance].Path.BasePath);
					}
				}
				//button_Instance_OpenFolder.Draw();

				button_Instance_Delete.Update();
				if ((button_Instance_Delete.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
					button_Instance_Delete.InteractionTags &= ~(uint)InteractionTag.Selected;
					if (SelectedInstance >= 0)
					{
						MESSAGEBOX_RESULT result = PInvoke.MessageBoxEx(HWND.Null,
							AppResources.Instance_Delete_Continue_Text,
							AppResources.Instance_Delete_Continue_Title,
							MESSAGEBOX_STYLE.MB_OKCANCEL | MESSAGEBOX_STYLE.MB_ICONWARNING | MESSAGEBOX_STYLE.MB_DEFBUTTON2 | MESSAGEBOX_STYLE.MB_TASKMODAL | MESSAGEBOX_STYLE.MB_TOPMOST,
							0);
						if (result == MESSAGEBOX_RESULT.IDOK)
						{
							Profiles.RemoveAt(SelectedInstance);
							
							JObject jProfiles = JObject.Parse(File.ReadAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\profiles.json"));
							JArray jProfileArray = (JArray)jProfiles["profiles"];

							jProfileArray.RemoveAt(SelectedInstance);

							jProfiles["profiles"] = jProfileArray;
							File.WriteAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\profiles.json", jProfiles.ToString());

							
							buttons_Instances.RemoveAt(SelectedInstance);
							SelectedInstance = -1;
							Console.ForegroundColor = ConsoleColor.White;
							Console.BackgroundColor = ConsoleColor.Black;
							Console.Clear();
						}
					}
				}
				//button_Instance_Delete.Draw();




				for (int i = 0; i < Profiles.Count; i++)
				{
					buttons_Instances[i].Update();
					buttons_Instances[i].Text = Profiles[i].DisplayName;

					if ((buttons_Instances[i].InteractionTags & (uint)InteractionTag.Hover) != 0)
					{
						if (!Profiles[i].Initialized)
						{
							ByteProgress progress = Profiles[i].ByteProgress;
							float percentage = (float)progress.ProgressedBytes / (float)progress.TotalBytes * 10000f;
							buttons_Instances[i].Text = $"{percentage                                       / 100f:0.00}%";
						}
					}
					if ((buttons_Instances[i].InteractionTags & (uint)InteractionTag.Selected) != 0)
					{
						for (int j = 0; j < buttons_Instances.Count; j++)
							if (j != i)
								buttons_Instances[j].InteractionTags &= ~(uint)InteractionTag.Selected;
						SelectedInstance = i;
					}
					//buttons_Instances[i].Draw();
				}
				Thread.Sleep(1);
			}
			cancellationTokenSource.Cancel();
			while (drawingThread.IsAlive) { }
		}

		static void PId_101()
		{
			TextInput input_Instance_Create_Name = new TextInput()
			{
				Position = new Point(2, 1), Size = new Size(Console.WindowWidth - 4, 3), Border = new Size(1, 1),
				PreviewText = AppResources.Instance_Create_Name, LineWrap = false,
				Visible = true, Enabled = true
			};
			TextInput input_Instance_Create_Version = new TextInput
			{
				Position = new Point(2, 5), Size = new Size(Console.WindowWidth - 4, 3), Border = new Size(1, 1),
				PreviewText = AppResources.Instance_Create_Version, LineWrap = false,
				Visible = true, Enabled = true
			};
			RadioBox radio_Instance_Create_ModLoader = new RadioBox
			{
				Position = new Point(2, 9), Size = new Size(Console.WindowWidth - 4, 4),
				Childreen = new RadioBox.Element[]
				{
					new RadioBox.Element
					{
						Text = "Vanilla",
						Highlight = 0
					},
					new RadioBox.Element
					{
						Text = "Fabric",
						Highlight = 0
					}
				},
				Visible = true, Enabled = true
			};
			
			Button button_Instance_Create_Finish = new Button
			{
				Position = new Point(2, 15), Size = new Size(Console.WindowWidth - 20, 3),
				Text = AppResources.Finish, LineWrap = false,
				Visible = true, Enabled = true
			};
			Button button_Instance_Create_Cancel = new Button
			{
				Position = new Point(Console.WindowWidth - 17, 15), Size = new Size(15, 3),
				Text = AppResources.Cancel, LineWrap = false,
				Visible = true, Enabled = true
			};

			TextBlock text_Instance_Create_Info = new TextBlock
			{
				Position = new Point(3, 19), Size = new Size(Console.WindowWidth - 3, 1),
				Text = "                                       ", LineWrap = false,
				ForegroundColor = ConsoleColor.Red, BackgroundColor = ConsoleColor.Black,
				Visible = true
			};
			
			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
			CancellationToken token = cancellationTokenSource.Token;
			
			Thread drawingThread = new Thread(() =>
			{
				Console.ForegroundColor = ConsoleColor.White; 
				Console.BackgroundColor = ConsoleColor.Black;
              	Console.Clear();
				while (!token.IsCancellationRequested) 
				{
					input_Instance_Create_Name.Draw().GetAwaiter().GetResult();
					input_Instance_Create_Version.Draw().GetAwaiter().GetResult();
					radio_Instance_Create_ModLoader.Draw().GetAwaiter().GetResult();
					button_Instance_Create_Finish.Draw().GetAwaiter().GetResult();
					button_Instance_Create_Cancel.Draw().GetAwaiter().GetResult();
					text_Instance_Create_Info.Draw().GetAwaiter().GetResult();
				}
			});
			drawingThread.Start();

			bool running = true;
			while (running)
			{
				Input.Update();
				Input.Mouse.Update();

				CURSOR.Tick(ConsoleKey.None);
				
				input_Instance_Create_Name.Update();
				if ((input_Instance_Create_Name.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
				}
				//input_Instance_Create_Name.Draw();
				
				input_Instance_Create_Version.Update();
				if ((input_Instance_Create_Version.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
				}
				//input_Instance_Create_Version.Draw();
				
				radio_Instance_Create_ModLoader.Update();
				Debug.WriteLine(radio_Instance_Create_ModLoader.Result);
				
				button_Instance_Create_Finish.Update();
				if ((button_Instance_Create_Finish.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
					button_Instance_Create_Finish.InteractionTags &= ~(uint)InteractionTag.Selected;

					VersionMetadataCollection versionMetadataCollection = Launcher.GetAllVersionsAsync().GetAwaiter().GetResult();
					if (!versionMetadataCollection.Contains(input_Instance_Create_Version.Text))
					{
						text_Instance_Create_Info.ForegroundColor = ConsoleColor.Red;
						text_Instance_Create_Info.Text = AppResources.Instance_Create_Info_Version;
						continue;
					}
					if (string.IsNullOrWhiteSpace(input_Instance_Create_Name.Text))
					{
						text_Instance_Create_Info.ForegroundColor = ConsoleColor.DarkYellow;
						text_Instance_Create_Info.Text = AppResources.Instance_Create_Info_Name;
						continue;
					}
					
					IVersion version = Launcher.GetVersionAsync(input_Instance_Create_Version.Text).GetAwaiter().GetResult();
					Profile profile = new Profile(input_Instance_Create_Name.Text, version, new MLaunchOption
					{
						Session = MSession.CreateOfflineSession("gamer123"),
						Features = new string[] { },

						JavaPath = "javaw.exe",
						MaximumRamMb = 4096,
						MinimumRamMb = 1024,

						IsDemo = false,
						ScreenWidth = 854,
						ScreenHeight = 480,
						FullScreen = false,

						ClientId = "clientid",
						VersionType = "CmlLib",
						GameLauncherName = "MineSharp",
						GameLauncherVersion = "PB1.1",
						UserProperties = "{}"
					}, radio_Instance_Create_ModLoader.Result);
					Profiles.Add(profile);
					SelectedInstance = Profiles.Count - 1;
					JObject jProfiles = JObject.Parse(File.ReadAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\profiles.json"));
					JArray jProfileArray = (JArray)jProfiles["profiles"];

					JObject jProfile = new JObject();

					jProfile.Add("displayName", profile.DisplayName);
					jProfile.Add("path", profile.Path.BasePath);
					jProfile.Add("version", profile.Version.Id);
					JObject jLaunchOption = JObject.FromObject(profile.LaunchOption);
					jProfile.Add("launchOption", jLaunchOption);
					jProfile.Add("modLoader", profile.ModLoader);
					jProfileArray.Add(jProfile);

					jProfiles["profiles"] = jProfileArray;
					File.WriteAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\profiles.json", jProfiles.ToString());

					PageIndex = 0;
					running = false;
				}
				//button_Instance_Create_Finish.Draw();
				
				button_Instance_Create_Cancel.Update();
				if ((button_Instance_Create_Cancel.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
					button_Instance_Create_Cancel.InteractionTags &= ~(uint)InteractionTag.Selected;
					
					PageIndex = 0;
					running = false;
				}
				//button_Instance_Create_Cancel.Draw();
				
				Thread.Sleep(1);
			}
			cancellationTokenSource.Cancel();
			while (drawingThread.IsAlive) { }
		}

		static void PId_102()
		{
			// GENERAL
			TextBlock text_Instance_Edit_General = new TextBlock
			{
				Position = new Point(2, 1), Size = new Size(Console.WindowWidth - 4, 1),
				ForegroundColor = ConsoleColor.Gray, BackgroundColor = ConsoleColor.Black,
				Text = AppResources.Instance_Edit_General,
				Enabled = true, Visible = true
			};
			TextInput input_Instance_Edit_Name = new TextInput
			{
				Position = new Point(3, 2), Size = new Size(Console.WindowWidth - 5, 3),
				PreviewText = AppResources.Instance_Edit_Name, LineWrap = false,
				Enabled = true, Visible = true
			};
			input_Instance_Edit_Name.Text = Profiles[SelectedInstance].DisplayName;
			// INSTALLATION
			// WINDOW
			TextBlock text_Instance_Edit_Window = new TextBlock
			{
				Position = new Point(2, 6), Size = new Size(Console.WindowWidth - 4, 1),
				ForegroundColor = ConsoleColor.Gray, BackgroundColor = ConsoleColor.Black,
				Text = AppResources.Instance_Edit_Window,
				Enabled = true, Visible = true
			};
			TextInput input_Instance_Edit_Width = new TextInput
			{
				Position = new Point(3, 7), Size = new Size(Console.WindowWidth / 2 - 2, 3),
				PreviewText = AppResources.Instance_Edit_Width, LineWrap = false,
				Enabled = true, Visible = true
			};
			input_Instance_Edit_Width.Text = Profiles[SelectedInstance].LaunchOption.ScreenWidth.ToString();
			TextInput input_Instance_Edit_Height = new TextInput
			{
				Position = new Point(Console.WindowWidth / 2 + 3, 7), Size = new Size(Console.WindowWidth / 2 - 5, 3),
				PreviewText = AppResources.Instance_Edit_Height, LineWrap = false,
				Enabled = true, Visible = true
			};
			input_Instance_Edit_Height.Text = Profiles[SelectedInstance].LaunchOption.ScreenHeight.ToString();
			// JAVA AND MEMORY
			TextBlock text_Instance_Edit_Java = new TextBlock
			{
				Position = new Point(2, 11), Size = new Size(Console.WindowWidth - 4, 1),
				ForegroundColor = ConsoleColor.Gray, BackgroundColor = ConsoleColor.Black,
				Text = AppResources.Instance_Edit_Java,
				Enabled = true, Visible = true
			};
			TextInput input_Instance_Edit_Memory = new TextInput
			{
				Position = new Point(3, 12), Size = new Size(Console.WindowWidth - 5, 3),
				PreviewText = AppResources.Instance_Edit_Memory, LineWrap = false,
				Enabled = true, Visible = true
			};
			input_Instance_Edit_Memory.Text = Profiles[SelectedInstance].LaunchOption.MaximumRamMb.ToString();
			// LAUNCH HOOKS
			// FINISH
			Button button_Instance_Edit_Finish = new Button
			{
				Position = new Point(3, 20), Size = new Size(Console.WindowWidth - 21, 3),
				Text = AppResources.Finish, LineWrap = false,
				Visible = true, Enabled = true
			};
			Button button_Instance_Edit_Cancel = new Button
			{
				Position = new Point(Console.WindowWidth - 17, 20), Size = new Size(15, 3),
				Text = AppResources.Cancel, LineWrap = false,
				Visible = true, Enabled = true
			};

			TextBlock text_Instance_Edit_Info = new TextBlock
			{
				Position = new Point(3, 23), Size = new Size(Console.WindowWidth - 3, 1),
				Text = "", LineWrap = false,
				ForegroundColor = ConsoleColor.Red, BackgroundColor = ConsoleColor.Black,
				Visible = true
			};
			
			CancellationTokenSource tokenSource = new CancellationTokenSource();
			CancellationToken token = tokenSource.Token;
			
			Thread drawingThread = new Thread(() =>
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.BackgroundColor = ConsoleColor.Black;
				Console.Clear();

				while (!token.IsCancellationRequested)
				{
					text_Instance_Edit_General.Draw().GetAwaiter().GetResult();
					input_Instance_Edit_Name.Draw().GetAwaiter().GetResult();
					text_Instance_Edit_Window.Draw().GetAwaiter().GetResult();
					input_Instance_Edit_Width.Draw().GetAwaiter().GetResult();
					input_Instance_Edit_Height.Draw().GetAwaiter().GetResult();
					text_Instance_Edit_Java.Draw().GetAwaiter().GetResult();
					input_Instance_Edit_Memory.Draw().GetAwaiter().GetResult();
					button_Instance_Edit_Finish.Draw().GetAwaiter().GetResult();
					button_Instance_Edit_Cancel.Draw().GetAwaiter().GetResult();
					text_Instance_Edit_Info.Draw().GetAwaiter().GetResult();
				}
			});
			drawingThread.Start();
			
			bool running = true;
			while (running)
			{
				Input.Update();
				Input.Mouse.Update();
				CURSOR.Tick(ConsoleKey.None);
				
				input_Instance_Edit_Name.Update();
				input_Instance_Edit_Width.Update();
				input_Instance_Edit_Height.Update();
				input_Instance_Edit_Memory.Update();
				
				button_Instance_Edit_Finish.Update();
				if ((button_Instance_Edit_Finish.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
					button_Instance_Edit_Finish.InteractionTags &= ~(uint)InteractionTag.Selected;
					

					if (!Int32.TryParse(input_Instance_Edit_Width.Text, out int windowWidth))
						continue;
					if (!Int32.TryParse(input_Instance_Edit_Height.Text, out int windowHeight))
						continue;
					if (!Int32.TryParse(input_Instance_Edit_Memory.Text, out int maxAmmountRam))
						continue;

					Profile profile = new Profile(input_Instance_Edit_Name.Text, Profiles[SelectedInstance].Path, Profiles[SelectedInstance].Version, new MLaunchOption
					{
						Session = MSession.CreateOfflineSession("Immortal640"),
						MaximumRamMb = maxAmmountRam,
						ScreenWidth = windowWidth,
						ScreenHeight = windowHeight
					}, Profiles[SelectedInstance].ModLoader);
					Profiles[SelectedInstance] = profile;
					JObject jProfiles = JObject.Parse(File.ReadAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\profiles.json"));
					JArray jProfileArray = (JArray)jProfiles["profiles"];

					JObject jProfile = new JObject();

					jProfile.Add("displayName", profile.DisplayName);
					jProfile.Add("path", profile.Path.BasePath);
					jProfile.Add("version", profile.Version.Id);
					JObject jLaunchOption = JObject.FromObject(profile.LaunchOption);
					jProfile.Add("launchOption", jLaunchOption);
					jProfile.Add("modLoader", profile.ModLoader);
					jProfileArray[SelectedInstance] = jProfile;

					jProfiles["profiles"] = jProfileArray;
					File.WriteAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\MineSharp\\profiles.json", jProfiles.ToString());
					
					PageIndex = 0;
					running = false;
				}
				
				button_Instance_Edit_Cancel.Update();
				if ((button_Instance_Edit_Cancel.InteractionTags & (uint)InteractionTag.Selected) != 0)
				{
					button_Instance_Edit_Cancel.InteractionTags &= ~(uint)InteractionTag.Selected;
					PageIndex = 0;
					running = false;
				}
			}
			
			tokenSource.Cancel();
			while (drawingThread.IsAlive) { }
		}
	}
}