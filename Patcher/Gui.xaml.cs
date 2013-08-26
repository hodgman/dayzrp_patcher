using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Net;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting.Messaging;
using util;
using ArmaServerInfo;
using System.Timers;
using Microsoft.Win32;
using SevenZip;
using LZMA = SevenZip.Compression.LZMA;

namespace Patcher
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		private string ManifestName { get { return "DayZRP.xml"; } }
		private string m_installPath_Arma2;
		private string m_installPath_Arma2OA;
		private string m_installPath_DayZRP;
		public Window1()
		{
			InitializeComponent();

			m_devOptions.Visibility = Visibility.Hidden;

			m_versionText.Text = m_versionText.Text + " " + App.LauncherVersion;

			m_steamTickBox.IsChecked = Properties.Settings.Default.useSteam;
			m_launchCommands.Text = Properties.Settings.Default.launchArgs;

			//Default servers to display before the XML file has been downloaded
			m_servers.Add("RP1 : S1", new GameServer("81.170.227.227", 2302));
			m_servers.Add("RP1 : S2", new GameServer("81.170.229.148", 2302));

			List<ServerListItem> initialServerList = new List<ServerListItem>();
			foreach (var server in m_servers)
			{
				initialServerList.Add(new ServerListItem()
				{
					locked = true,
					online = true,
					Name = server.Key,
					Players = "? / ?",
					PlayerList = "Refreshing..."
				});
			}
			m_serverListBox.ItemsSource = initialServerList;
			if (Properties.Settings.Default.lastServerIdx >= 0 && Properties.Settings.Default.lastServerIdx < initialServerList.Count)
				m_serverListBox.SelectedIndex = Properties.Settings.Default.lastServerIdx;

			m_installPath_Arma2 = ReadRegString("Bohemia Interactive Studio\\ArmA 2", "MAIN");
			m_installPath_Arma2OA = ReadRegString("Bohemia Interactive Studio\\ArmA 2 OA", "MAIN");
			if (m_installPath_Arma2 == null)
			{
				MessageBox.Show("Could not locate Arma 2 installation");
				Application.Current.Shutdown();
			}
			if (m_installPath_Arma2OA == null)
			{
				MessageBox.Show("Could not locate Arma 2 Operation Arrowhead installation");
				Application.Current.Shutdown();
			}
			m_installPath_DayZRP = Path.Combine(m_installPath_Arma2OA, "./@DayZRP");

			m_baseUrl = "http://launcher.dayzrp.com/";
		//	m_baseUrl = "http://localhost/test/";
			m_manifestUri = new Uri(Path.Combine(m_baseUrl, ManifestName));
			m_patch = null;
			m_patchServerBox.Text = m_baseUrl;
			Begin();
		}

		private void m_patchServerBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			try
			{
				string url = m_patchServerBox.Text;
				if (!url.EndsWith("/"))
					url += "/";
				Uri baseUri = new Uri(url);
				m_baseUrl = baseUri.AbsoluteUri;
				m_manifestUri = null;
				m_manifestUri = new Uri(Path.Combine(m_baseUrl, ManifestName));
			}
			catch (Exception) { }
			m_xmlUrlBox.Text = m_manifestUri == null ? "null" : m_manifestUri.AbsoluteUri;
		}

		private void SteamTickBox_Changed(object sender, RoutedEventArgs e)
		{
			Properties.Settings.Default.useSteam = (m_steamTickBox.IsChecked == true);
			Properties.Settings.Default.Save();
		}

		private void LaunchCommands_TextChanged(object sender, TextChangedEventArgs e)
		{
			Properties.Settings.Default.launchArgs = m_launchCommands.Text;
			Properties.Settings.Default.Save();
		}

		private void Close_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}

		private string m_easter = "";
		void Window_TextInput(object sender, TextCompositionEventArgs e)
		{
			string text = e.Text;
			if (text == "\b")
				m_easter = "";
			else
				m_easter += text.ToLower();

			if (m_easter.EndsWith("devmode"))
			{
				m_devOptions.Visibility = Visibility.Visible;
				m_tabs.SelectedItem = m_tabAbout;
			}
			else if (m_easter.EndsWith("justice"))
			{
				m_badgeImg.Visibility = Visibility.Visible;
				m_tabs.SelectedItem = m_tabAbout;
			}
			else if (m_easter.EndsWith("nofunallowed"))
				ProcessOutput.Run("http://www.youtube.com/watch?v=HPSBzs8a3NY", "", "", null, false, true);
			else if (m_easter.EndsWith("tir5"))
			{
				string tirDir = ReadRegString("NaturalPoint\\NaturalPoint\\NPClient Location", "Path");
				if (!String.IsNullOrEmpty(tirDir))
					ProcessOutput.Run(Path.Combine(tirDir, "TrackIR5.exe"), "", tirDir, null, false, false);
			}
		//	else if (m_easter.EndsWith("test"))
		//		Test();
			else
				return;
			m_easter = "";
		}

		void Test()
		{
			LZMA.Encoder encoder = new LZMA.Encoder();

			CoderPropID[] propIDs = 
			{
				CoderPropID.DictionarySize,
				CoderPropID.PosStateBits,
				CoderPropID.LitContextBits,
				CoderPropID.LitPosBits,
				CoderPropID.NumFastBytes,
				CoderPropID.MatchFinder,
				CoderPropID.EndMarker,
			};
			object[] properties = 
			{
				(Int32)(1<<26),
				(Int32)(2),
				(Int32)(3),
				(Int32)(2),
				(Int32)(128),
				"bt4",
				false,
			};

			encoder.SetCoderProperties(propIDs, properties);

			FileInfo inputFile = new FileInfo("D:\\test.data");
			Stream inStream = inputFile.OpenRead();
			Stream outStream = new FileStream("D:\\test.lzma", FileMode.Create, FileAccess.Write);

			encoder.Code(inStream, outStream, -1, -1, null);

/*
			FileInfo inputFile = new FileInfo("D:\\test.7z");
			Stream inStream = inputFile.OpenRead();
			Stream outStream = new FileStream("D:\\test.out", FileMode.Create, FileAccess.Write);

			byte[] properties = new byte[5];
			if (inStream.Read(properties, 0, 5) != 5)
				throw (new Exception("input .lzma is too short"));
			LZMA.Decoder decoder = new LZMA.Decoder();
			decoder.SetDecoderProperties(properties);
			long outSize = 0;
			for (int i = 0; i < 8; i++)
			{
				int v = inStream.ReadByte();
				if (v < 0)
					throw (new Exception("Can't Read 1"));
				outSize |= ((long)(byte)v) << (8 * i);
			}
			long compressedSize = inStream.Length - inStream.Position;
			decoder.Code(inStream, outStream, compressedSize, outSize, null);*/

			inStream.Close();
			outStream.Close();
		}

		private string LauncherVersion { get { return App.LauncherVersion; } }

		private DirectoryScanner m_scanner = new DirectoryScanner();
		private Downloader m_downloader = new Downloader();

		ProcessOutput m_game;

		private string m_baseUrl;
		private Uri m_manifestUri;
		private PatchManifest m_patch;
		private FileHashDatabase m_cache;

		private struct DownloadLocation
		{
			public DownloadLocation(FileInfo dest)
			{
				destination = dest;
				tempCompressed = null;
			}
			public DownloadLocation(FileInfo dest, FileInfo comp)
			{
				destination = dest;
				tempCompressed = comp;
			}
			public FileInfo destination;
			public FileInfo tempCompressed;
		}
		class CompressionTask
		{
			public CompressionTask(DownloadLocation d)
			{
				dl = d;
				done = false;
			}
			public DownloadLocation dl;
			public bool done;
		}
		private FileInfo TempDownloadFile(string relativeFile)
		{
			return new FileInfo(Path.GetTempFileName());
		}

		class PatchTasks
		{
			public int countPatchTasks;
			public int totalPatchTasks;
			public List<FileInfo> toDelete;
			public Dictionary<DownloadLocation, Uri> toDownload;
			public int numErrors;
		}
		private PatchTasks m_tasks;

		FileInfo CacheFile
		{
			get { return new FileInfo(Path.Combine(m_installPath_DayZRP, "Launcher.Cache")); }
		}

		private void Log_ThreadSafe(string text)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action<string>)delegate(string t) { this.Log(t); }, text);
		}
		private void Log(string text)
		{
			display.Items.Add(text);
		}

		private void Retry_Click(object sender, RoutedEventArgs e)
		{
			Begin();
		}

		private bool m_isInstallValid = false;
		private void SetIsPatching()
		{
			SetIsPatching(true, false);
		}
		private void SetIsPatching(bool inProgress, bool validResult)
		{
			if (inProgress)
			{
				m_isInstallValid = false;
				m_btnRetry.IsEnabled = false;
			}
			else
			{
				m_isInstallValid = validResult;
				m_btnRetry.IsEnabled = true;
			}
		}

		private void Begin()
		{
			SetIsPatching();
			display.Items.Clear();
			Log("Contacting patch server");
		//	if (m_patch == null)
				m_downloader.DownloadBytes(m_manifestUri, PatchManifestDownloaded, null);
		//	else
		//		BeginScan();
		}

		private void PatchManifestDownloaded(Uri uri, byte[] raw, string error, object userData)
		{
			bool guessValid = false;
			try
			{
				DirectoryInfo dir = new DirectoryInfo(m_installPath_DayZRP);
				guessValid = dir.Exists;
			}
			catch (Exception) { }
			if (error != null)
			{
				Log("Failed contacting patch server: " + error);
				SetIsPatching(false, guessValid);
				RefreshServerList();
				m_tabs.SelectedItem = m_tabLaunch;
				return;
			}
			Debug.Assert(uri == m_manifestUri);
			XmlDocument doc = new XmlDocument();
			MemoryStream stream = new MemoryStream(raw);
			string exText = null;
			try
			{
				doc.Load(stream);
				m_patch = PatchManifest.Parse(doc, m_baseUrl);
			}
			catch (Exception e) { exText = e.ToString(); }
			if (m_patch == null)
			{
				Log("Failed to parse XML document");
				if (exText != null)
					Log("\t" + exText);
				SetIsPatching(false, guessValid);
				RefreshServerList();
				return;
			}
			else
			{
				Log("Retrieved patch manifest");
				if (!String.IsNullOrEmpty(m_patch.launcherVersion) && m_patch.launcherVersion != LauncherVersion)
				{
					Log("New version of launcher available: " + m_patch.launcherVersion);
					MessageBoxResult r = MessageBox.Show("New version of launcher available: " + m_patch.launcherVersion+"\nWould you like to update now?", "Launcher out of date!", MessageBoxButton.YesNo);
					if (r == MessageBoxResult.Yes)
					{
						StartLauncherDownload();
						return;
					}
				}
			//	else foreach (var a in m_patch.assets)
			//		Log("\tFile: " + a.Key + ", " + StringExtension.ToHexString(a.Value.hash) + ", " + a.Value.uri.ToString());

				if (m_patch.servers != null && m_patch.servers.Count > 0)
				{
					Dictionary<string, GameServer> servers = new Dictionary<string, GameServer>();
					foreach(var server in m_patch.servers)
						servers.Add(server.name, new GameServer(server.host, server.port));
					m_servers = servers;
				}
				RefreshServerList();
			}

			try
			{
				DirectoryInfo dir = new DirectoryInfo(m_installPath_DayZRP);
				if (!dir.Exists)
				{
					dir.Create();
					Log("Installation directory is not valid");
					MessageBoxResult r = MessageBox.Show("DayZRP is not currently installed!\nWould you like to use BitTorrent for the initial installation (faster)?", "Couldn't find existing DayZRP installation", MessageBoxButton.YesNo);
					if (r == MessageBoxResult.Yes)
					{
						ProcessOutput.Run("http://www.dayzrp.com/t-dayzrp-mod-download", "", "", null, false, true);
						SetIsPatching(false, false);
						return;
					}
				}
			}
			catch (Exception)
			{
				Log("Installation directory is not valid");
				SetIsPatching(false, false);
				return;
			}

			BeginScan();
		}

		private FileInfo m_tempNewPatcher;
		private void StartLauncherDownload()
		{
			Debug.Assert(m_tasks == null);
			m_tasks = new PatchTasks
			{
				countPatchTasks = 0,
				totalPatchTasks = 1,
				toDelete = new List<FileInfo>(),
				toDownload = new Dictionary<DownloadLocation, Uri>(),
				numErrors = 0
			};

			m_tempNewPatcher = new FileInfo(Path.GetTempFileName()+"_launcher_update.exe");
			m_tasks.toDownload.Add(new DownloadLocation(m_tempNewPatcher), m_patch.launcherUri);
			
			Progress1.Value = Progress1.Minimum;
			Progress2.Value = Progress2.Minimum;
			Action task = new Action(DoPatchTasks);
			task.BeginInvoke(new AsyncCallback(LauncherDownloadComplete_ThreadSafe), this);
		}
		private void LauncherDownloadComplete_ThreadSafe(IAsyncResult ar)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action<IAsyncResult>)delegate(IAsyncResult a) { this.LauncherDownloadComplete(a); }, ar);
		}
		private void LauncherDownloadComplete(IAsyncResult ar)
		{
			AsyncResult result = (AsyncResult)ar;
			Action caller = (Action)result.AsyncDelegate;
			caller.EndInvoke(ar);
			Debug.Assert(m_tasks != null);

			if (m_tasks.numErrors != 0)
			{
				Log("Error updating launcher");
			}
			else
			{
				string currentExe = System.Reflection.Assembly.GetExecutingAssembly().Location;
				try
				{
					string args = "-move \"" + currentExe + "\"";
					if (ProcessOutput.Run(m_tempNewPatcher.FullName, args, Path.GetDirectoryName(currentExe), null, true, true) == null)
						Log("Trouble starting new launcher version!");
					else
						Application.Current.Shutdown();
				}
				catch (Exception)
				{
					Log("Trouble restarting new launcher version!");
				}
			}
			SetIsPatching(false, false);
			m_tasks = null;
			m_tempNewPatcher = null;
		}

		private void BeginScan()
		{
			Log("Checking local installation");
			m_cache = FileHashDatabase.Load(CacheFile);
			DirectoryScanner.ScanCompleteCallback callback =
					(x, y) => Dispatcher.BeginInvoke(new DirectoryScanner.ScanCompleteCallback(DirectoryScanComplete), DispatcherPriority.Normal, new object[] { x, y });
			m_scanner.BeginScan(m_installPath_DayZRP, callback);
		}

		private void DirectoryScanComplete(List<FileInfo> results, bool error)
		{
			if (error)
			{
				Log("Something went wrong when scanning installation directory");
				m_taskStatus.Content = "Update failed";
				SetIsPatching(false, false);
				return;
			}
			if( results.Count == 0 )
			{
				MessageBoxResult r = MessageBox.Show("DayZRP is not currently installed!\nWould you like to use BitTorrent for the initial installation (faster)?", "Couldn't find existing DayZRP installation", MessageBoxButton.YesNo);
				if (r == MessageBoxResult.Yes)
				{
					ProcessOutput.Run("http://www.dayzrp.com/t-dayzrp-mod-download", "", "", null, false, true);
					SetIsPatching(false, false);
					return;
				}
			}

			if( m_patch.assets.Count == 0 )
			{
				Log("No files listed in patch manifest...");
				m_taskStatus.Content = "Patching failed";
				SetIsPatching(false, true);
				return;
			}

			Debug.Assert(m_tasks == null);
			m_tasks = new PatchTasks
			{
				countPatchTasks = 0,
				totalPatchTasks = results.Count + m_patch.assets.Count,
				toDelete = new List<FileInfo>(),
				toDownload = new Dictionary<DownloadLocation, Uri>(),
				numErrors = 0
			};
			Progress1.Value = Progress1.Minimum;
			Progress2.Value = Progress2.Minimum;
			Action<List<FileInfo>> task = new Action<List<FileInfo>>(this.DoInstallationChecks);
			task.BeginInvoke(results, new AsyncCallback(OnInstallationCheckComplete_ThreadSafe), this);
		}

		private void DoInstallationChecks(List<FileInfo> results)
		{
			Debug.Assert(m_tasks != null);
			HashSet<string> alreadyInspected = new HashSet<string>();
			//for each existing file
			foreach (var file in results)
			{
				IncrementPatchProgressBar_ThreadSafe(false);
				string name = m_cache.GetRelativeName(file);

				if (name == m_cache.GetRelativeName(CacheFile))
					continue;

				alreadyInspected.Add(name);
				DateTime stamp = file.LastWriteTimeUtc;

				PatchManifest.AssetInfo patchFile = m_patch.GetFileInfo(name);
				if (patchFile == null)
				{
					Log_ThreadSafe("\t delete " + name + ", not in patch manifest");
					m_tasks.toDelete.Add(file);
				}
				else
				{
					if (!m_cache.IsFileValid(name, file, patchFile.hash))
					{
						Log_ThreadSafe("\t Invalid: " + name + ", get from " + patchFile.uri.ToString());
						var dl = new DownloadLocation(file, TempDownloadFile(name));
						m_tasks.toDownload.Add(dl, patchFile.uri);
					}
				//	else
				//		Log_ThreadSafe("\t Valid: " + name + ", " + stamp.ToString());
				}
			}
			foreach (var patchFile in m_patch.assets)
			{
				IncrementPatchProgressBar_ThreadSafe(false);
				if(alreadyInspected.Contains(patchFile.Key.ToLowerInvariant()))
					continue;
				Log_ThreadSafe("\t Missing: " + patchFile.Key + ", get from " + patchFile.Value.uri.ToString());
				var dl = new DownloadLocation(m_cache.GetAbsoluteFile(patchFile.Key), TempDownloadFile(patchFile.Key));
				m_tasks.toDownload.Add(dl, patchFile.Value.uri);
			}

			m_cache.Save(CacheFile);
		}
		
		private void OnInstallationCheckComplete_ThreadSafe(IAsyncResult ar)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action<IAsyncResult>)delegate(IAsyncResult a) { this.OnInstallationCheckComplete(a); }, ar);
		}
		private void OnInstallationCheckComplete(IAsyncResult ar)
		{
			AsyncResult result = (AsyncResult)ar;
			Action<List<FileInfo>> caller = (Action<List<FileInfo>>)result.AsyncDelegate;
			caller.EndInvoke(ar);
			Debug.Assert(m_tasks != null);
			if (m_tasks.toDelete.Count > 0 || m_tasks.toDownload.Count > 0)
			{
				Log("Beginning patch process");
				m_tasks.countPatchTasks = 0;
				m_tasks.totalPatchTasks = m_tasks.toDelete.Count + m_tasks.toDownload.Count;
				Progress1.Value = Progress1.Minimum;
				Progress2.Value = Progress2.Minimum;
				Action task = new Action(this.DoPatchTasks);
				task.BeginInvoke(new AsyncCallback(OnPatchingComplete_ThreadSafe), this);
			}
			else
			{
				m_tasks = null;
				Log("Installation is up to date");
				m_taskStatus.Content = "Ready to launch";
				SetIsPatching(false, true);
				m_tabs.SelectedItem = m_tabLaunch;
			}
		}

		private void DoPatchTasks()
		{
			foreach (var file in m_tasks.toDelete)
			{
				file.Delete();
				Log_ThreadSafe("\tDeleted " + file.FullName);
				IncrementPatchProgressBar_ThreadSafe(false);
			}
			int maxConcurrentDownloads = 1;
			List<object> downloadHandles = new List<object>();
			foreach (var nameInfo in m_tasks.toDownload)
			{
				Log_ThreadSafe("\tDownloading " + nameInfo.Key);
				SetTaskProgressBar_ThreadSafe(0, "");
				object decompressionTask = null;
				string downloadTo = nameInfo.Key.destination.FullName;
				if (nameInfo.Key.tempCompressed != null)
				{
					downloadTo = nameInfo.Key.tempCompressed.FullName;
					decompressionTask = new CompressionTask(nameInfo.Key);
				}
				object handle = m_downloader.DownloadFile(nameInfo.Value, downloadTo, FileDownloaded, ProgressUpdate, decompressionTask);
				if (handle == null)
				{
					Log_ThreadSafe("\tFailed to initiate download of " + nameInfo.Value.ToString());
					IncrementPatchProgressBar_ThreadSafe(true);
				}
				else
				{
					downloadHandles.Add(handle);
					while (downloadHandles.Count >= maxConcurrentDownloads)
					{
						foreach (var h in downloadHandles)
						{
							if (m_downloader.IsComplete(h))
							{
								downloadHandles.Remove(h);
								break;
							}
						}
						Thread.Sleep(100);
					}
				}
			}
		}
		private void FileDownloaded(Uri uri, byte[] raw, string error, object decompressionTask)
		{
			SetTaskProgressBar_ThreadSafe(100, "");
			if (error != null)
			{
				if (decompressionTask != null)
				{
					CompressionTask task = decompressionTask as CompressionTask;
					if (task != null)
						task.done = true;
				}
				IncrementPatchProgressBar_ThreadSafe(true);
				Log_ThreadSafe("\tFailed to download " + uri.ToString() + ": " + error);
				return;
			}
			if (decompressionTask != null)
			{
				string file = "?";
				CompressionTask task = null;
				try
				{
					task = (CompressionTask)decompressionTask;
					file = task.dl.destination.Name;
					SetTaskProgressBar_ThreadSafe(100, "Decompressing " + task.dl.destination.Name);
					task.dl.destination.Directory.Create();

					Stream inStream = task.dl.tempCompressed.OpenRead();
					Stream outStream = task.dl.destination.OpenWrite();

					byte[] properties = new byte[5];
					if (inStream.Read(properties, 0, 5) != 5)
						throw (new Exception("input .lzma is too short"));
					LZMA.Decoder decoder = new LZMA.Decoder();
					decoder.SetDecoderProperties(properties);
					long outSize = 0;
					for (int i = 0; i < 8; i++)
					{
						int v = inStream.ReadByte();
						if (v < 0)
							throw (new Exception("Can't Read 1"));
						outSize |= ((long)(byte)v) << (8 * i);
					}
					long compressedSize = inStream.Length - inStream.Position;
					decoder.Code(inStream, outStream, compressedSize, outSize, null);
					task.done = true;
				}
				catch (Exception e)
				{
					if (task != null)
						task.done = true;
					IncrementPatchProgressBar_ThreadSafe(true);
					Log_ThreadSafe("\tFailed during decompression of " + file + ": " + e.ToString());
				}
			}
			IncrementPatchProgressBar_ThreadSafe(false);
		}
		private void ProgressUpdate(long recieved, long total, int percent, DateTime startTime)
		{
			double seconds = (DateTime.Now - startTime).TotalSeconds;
			if (seconds == 0)
			{
				SetTaskProgressBar_ThreadSafe(percent, "");
				return;
			}
			double bps = recieved / seconds;
			string units;
			double speed;
			if (bps < 1024)
			{
				speed = bps;
				units = "Bytes";
			}
			else if (bps < 1024 * 1024)
			{
				speed = bps / 1024;
				units = "KB";
			}
			else
			{
				speed = (bps / 1024) / 1024;
				units = "MB";
			}
			SetTaskProgressBar_ThreadSafe(percent, String.Format("{0:0.0}{1}/s", speed, units));
		}

		private void IncrementPatchProgressBar_ThreadSafe(bool error)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action<bool>)delegate(bool b) { this.IncrementPatchProgressBar(b); }, error);
		}
		private void IncrementPatchProgressBar(bool error)
		{
			if (m_tasks == null)
				return;
			if (error)
				++m_tasks.numErrors;
			++m_tasks.countPatchTasks;
			Progress1.Value = Progress1.Maximum / m_tasks.totalPatchTasks * m_tasks.countPatchTasks;
		}

		delegate void SetTaskProgressDelegate(int percent, string message);
		private void SetTaskProgressBar_ThreadSafe(int percent, string message)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action<int, string>)delegate(int a, string b) { this.SetTaskProgressBar(a, b); }, percent, new object[]{message});
		}
		private void SetTaskProgressBar(int percent, string message)
		{
			if (m_tasks == null)
				return;
			Progress2.Value = Progress1.Maximum * (percent/100.0);
			m_taskStatus.Content = message;
		}
		
		private void OnPatchingComplete_ThreadSafe(IAsyncResult ar)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action<IAsyncResult>)delegate(IAsyncResult a) { this.OnPatchingComplete(a); }, ar);
		}
		private void OnPatchingComplete(IAsyncResult ar)
		{
			AsyncResult result = (AsyncResult)ar;
			Action caller = (Action)result.AsyncDelegate;
			caller.EndInvoke(ar);
			Progress1.Value = Progress1.Maximum;
			Progress2.Value = Progress2.Maximum;
			if (m_tasks.numErrors == 0)
			{
				SetIsPatching(false, true);
				Log("Downloads complete");
				m_taskStatus.Content = "Ready to launch";
				m_tabs.SelectedItem = m_tabLaunch;
			}
			else
			{
				SetIsPatching(false, false);
				m_taskStatus.Content = String.Format("There were {0} errors while attempting to update", m_tasks.numErrors);
			}
			m_tasks = null;
		}

		string ReadRegString(string path, string key)
		{
			RegistryKey[] roots = new RegistryKey[2] { Registry.CurrentUser, Registry.LocalMachine };
			string[] prefixes = new string[2] { "", "Wow6432Node\\" };
			for (int i = 0; i != 2; ++i)
			{
				RegistryKey root = roots[i];
				for (int j = 0; j != 2; ++j)
				{
					try
					{
						RegistryKey subKey = root.OpenSubKey("SOFTWARE\\" + prefixes[j] + path);
						if (subKey != null)
						{
							object value = subKey.GetValue(key);
							if (value != null)
								return value.ToString();
						}
					}
					catch (Exception) { }
				}
			}
			return null;
		}

		private void LaunchGame(bool joinServer)
		{
			if (m_isInstallValid == false)
			{
				MessageBoxResult r = MessageBox.Show("Install is not complete, launch anyway?", "Install not validated", MessageBoxButton.OKCancel);
				if (r == MessageBoxResult.Cancel)
					return;
			}
			if (m_game != null)
				m_game.Kill();
			m_game = null;
			string dir = Path.GetFullPath(m_installPath_Arma2OA);
			string a2path = Path.GetFullPath(m_installPath_Arma2);
			string exe = Path.GetFullPath(Path.Combine(dir, "./Expansion/beta/ARMA2OA.exe"));
			string args = String.Format(
				"\"-mod={0};EXPANSION;ca\" " +
				"\"-beta=Expansion\\beta;Expansion\\beta\\Expansion;\" " +
				"-mod=@DayZRP -nosplash -nopause -skipIntro -world=Chernarus ",
				a2path);
			EventHandler onExit = (EventHandler)delegate(object a, EventArgs b)
			{
				Dispatcher.BeginInvoke((Action)delegate
				{
					OnGameExit();
				});
			};

			if (joinServer)
			{
				int i = 0;
				foreach (var server in m_servers)
				{
					if (i != m_serverListBox.SelectedIndex)
					{
						++i;
						continue;
					}
					args += " -connect=" + server.Value.Settings.Host + " -port=" + server.Value.Settings.RemotePort;
					break;
				}
			}

			if (m_steamTickBox.IsChecked == true)
			{
				string steamExe = ReadRegString("Valve\\Steam", "SteamExe");
				if (steamExe != null)
				{
					exe = steamExe;
					args = "-applaunch 219540 " + args;
				}
				else
					MessageBox.Show("Could not locate Steam installation");
			}

			if (!String.IsNullOrEmpty(m_launchCommands.Text))
				args = args + " " + m_launchCommands.Text;

			m_game = ProcessOutput.Run(exe, args, dir, onExit, false, false);
			if (m_game != null && !m_game.Finished)
			{
				m_taskStatus.Content = "Game is running...";
				m_btnLaunch1.IsEnabled = false;
				m_btnLaunch2.IsEnabled = false;
			}
			else
				m_taskStatus.Content = "Error launching game.";
		}
		private void Join_Click(object sender, RoutedEventArgs e)
		{
			if (m_serverListBox.SelectedIndex < 0)
				return;
			LaunchGame(true);
		}
		private void Launch_Click(object sender, RoutedEventArgs e)
		{
			LaunchGame(false);
		}
		private void OnGameExit()
		{
			m_taskStatus.Content = "Ready to launch";
			m_btnLaunch1.IsEnabled = true;
			m_btnLaunch2.IsEnabled = true;
		}

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			RefreshServerList();
		}
		private void RefreshServerList()
		{
			if (m_btnRefresh.IsEnabled == true)
			{
				m_btnRefresh.IsEnabled = false;
				Action checkServers = (Action)delegate() { CheckServers(m_servers); };
				checkServers.BeginInvoke(null, this);
			}
		}

		private Dictionary<string, GameServer> m_servers = new Dictionary<string, GameServer>();
		private void CheckServers(Dictionary<string, GameServer> servers)
		{
			List<ServerListItem> items = new List<ServerListItem>();

			foreach (var server in servers)
			{
				string name = server.Key;
				var gs = server.Value;
				gs.Settings.ReceiveTimeout = 1000;
				gs.Update();
				bool online = gs.IsOnline;
				int numPlayers = 0;
				int maxPlayers = 50;
				bool locked = false;
				if (gs.ServerInfo != null)
				{
					numPlayers = gs.ServerInfo.NumPlayers;
					maxPlayers = gs.ServerInfo.MaxPlayers;
					name = gs.ServerInfo.HostName;
					locked = gs.ServerInfo.Password;
				}
				string playerList = "";
				if (gs.Players != null)
				{
					foreach (var player in gs.Players)
					{
						if (playerList != "")
							playerList += "\n";
						playerList += player.Name;
					}
				}
				if (playerList == "")
					playerList = "No players online";
				items.Add(new ServerListItem()
					{
						locked = locked,
						online = online,
						Name = name,
						Players = String.Format("{0} / {1}",numPlayers, maxPlayers),
						PlayerList = playerList
					});
			}
			Dispatcher.BeginInvoke((Action)delegate
			{
				m_btnRefresh.IsEnabled = true;
				int idx = m_serverListBox.SelectedIndex;
				m_serverListBox.ItemsSource = items;
				if (idx == -1 && Properties.Settings.Default.lastServerIdx != -1)
					idx = Properties.Settings.Default.lastServerIdx;
				m_serverListBox.SelectedIndex = idx;
			});
		}

		private void ServerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Properties.Settings.Default.lastServerIdx = m_serverListBox.SelectedIndex;
			Properties.Settings.Default.Save();
		}

		void ServerList_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				if (m_serverListBox.SelectedIndex < 0)
					return;
				LaunchGame(true);
			}
		}

		private void GoToDayZRP_Rules_Click(object sender, RoutedEventArgs e)
		{
			ProcessOutput.Run("http://www.dayzrp.com/rules.php", "", "", null, false, true);
		}
		private void GoToDayZRP_Team_Click(object sender, RoutedEventArgs e)
		{
			ProcessOutput.Run("http://www.dayzrp.com/showteam.php", "", "", null, false, true);
		}
		private void GoToDayZRP_Click(object sender, RoutedEventArgs e)
		{
			ProcessOutput.Run("http://www.dayzrp.com/", "", "", null, false, true);
		}

		private void LaunchTeamSpeak_Click(object sender, RoutedEventArgs e)
		{
			ProcessOutput.Run("ts3server://ts.dayzrp.com?port=9987&password=dayzrp", "", "", null, false, true);
		}
		private void LaunchIrc_Click(object sender, RoutedEventArgs e)
		{
			ProcessOutput.Run("http://webchat.quakenet.org/?channels=DayZRP&uio=Mj10cnVlJjQ9dHJ1ZSY5PXRydWUmMTA9dHJ1ZSYxMT0yMTUe9", "", "", null, false, true);
		}
	}
	public class ServerListItem
	{
		public bool online;
		public bool locked;
		public string Status { get { return !online ? "Offline" : locked ? "Locked" : "Online"; } }
		public string ImageSource
		{
			get
			{
				if (!online)
					return "pack://application:,,,/img/red.png";
				else if(locked)
					return "pack://application:,,,/img/blue.png";
				else
					return "pack://application:,,,/img/green.png";
			}
		}
		public string Name { get; set; }
		public string Players { get; set; }
		public string PlayerList { get; set; }
	}
}