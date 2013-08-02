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

namespace Patcher
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		public Window1()
		{
			InitializeComponent();
			m_timer = new System.Timers.Timer();
			m_timer.Elapsed += (ElapsedEventHandler)delegate(object a, ElapsedEventArgs b)
			{
				CheckServers();
				m_timer.Interval = 5000;
				m_timer.Enabled = true;
			};
			m_timer.AutoReset = false;
			m_timer.Enabled = true;


			m_a2Box.Text = ReadRegString("Bohemia Interactive Studio\\ArmA 2", "MAIN");
			m_a2oaBox.Text = ReadRegString("Bohemia Interactive Studio\\ArmA 2 OA", "MAIN");
		}

		private string LauncherVersion { get { return App.LauncherVersion; } }

		private System.Timers.Timer m_timer;
		private DirectoryScanner m_scanner = new DirectoryScanner();
		private Downloader m_downloader = new Downloader();

		ProcessOutput m_game;

		private string m_installPath;
		private string m_baseUrl;
		private Uri m_manifestUri;
		private PatchManifest m_patch;
		private FileHashDatabase m_cache;

		class PatchTasks
		{
			public int countPatchTasks;
			public int totalPatchTasks;
			public List<FileInfo> toDelete;
			public Dictionary<FileInfo, Uri> toDownload;
			public int numErrors;
		}
		private PatchTasks m_tasks;

		FileInfo CacheFile
		{
			get { return new FileInfo(Path.Combine(m_installPath, "Launcher.Cache")); }
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

		private void Begin_Click(object sender, RoutedEventArgs e)
		{
			m_baseUrl = m_urlBox.Text;
			m_installPath = m_installPathBox.Text;
			m_manifestUri = new Uri(Path.Combine(m_baseUrl, "current.xml"));
			m_patch = null;
			Begin();
		}

		private void Begin()
		{
			//!	m_btnLaunch.IsEnabled = false;
			m_btnLaunch.IsEnabled = true;
			m_btnRetry.IsEnabled = false;
			display.Items.Clear();
			Log("Contacting patch server");
			if (m_patch == null)
				m_downloader.DownloadBytes(m_manifestUri, PatchManifestDownloaded);
			else
				BeginScan();
		}

		private void PatchManifestDownloaded(Uri uri, byte[] raw, string error)
		{
			if (error != null)
			{
				Log("Failed contacting patch server: " + error);
				m_btnRetry.IsEnabled = true;
				return;
			}
			Debug.Assert(uri == m_manifestUri);
			XmlDocument doc = new XmlDocument();
			MemoryStream stream = new MemoryStream(raw);
			try
			{
				doc.Load(stream);
				m_patch = PatchManifest.Parse(doc, m_baseUrl);
			}
			catch (Exception) { }
			if (m_patch == null)
			{
				Log("Failed to parse XML document");
				m_btnRetry.IsEnabled = true;
				return;
			}
			else
			{
				Log("Retrieved manifest for version: " + m_patch.version);
				if (!String.IsNullOrEmpty(m_patch.launcherVersion) && m_patch.launcherVersion != LauncherVersion)
				{
					Log("New version of launcher available: " + m_patch.launcherVersion);
					StartLauncherDownload();
					return;
				}
				else foreach (var a in m_patch.assets)
					Log("\tFile: " + a.Key + ", " + StringExtension.ToHexString(a.Value.hash) + ", " + a.Value.uri.ToString());
			}

			DirectoryInfo dir = null;
			try
			{
				dir = new DirectoryInfo(m_installPath);
				if (!dir.Exists )
					dir.Create();
			}
			catch (Exception)
			{
				Log("Installation directory is not valid");
				m_btnRetry.IsEnabled = true;
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
				toDownload = new Dictionary<FileInfo, Uri>(),
				numErrors = 0
			};

			m_tempNewPatcher = new FileInfo(Path.GetTempFileName()+"_launcher_update.exe");
			m_tasks.toDownload.Add(m_tempNewPatcher, m_patch.launcherUri);
			
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
					if (ProcessOutput.Run(m_tempNewPatcher.FullName, args, Path.GetDirectoryName(currentExe), null, true) == null)
						Log("Trouble starting new launcher version!");
					else
						Application.Current.Shutdown();
				}
				catch (Exception)
				{
					Log("Trouble restarting new launcher version!");
				}
			}
			m_btnRetry.IsEnabled = true;
			m_tasks = null;
			m_tempNewPatcher = null;
		}

		private void BeginScan()
		{
			Log("Checking local installation");
			m_cache = FileHashDatabase.Load(CacheFile);
			DirectoryScanner.ScanCompleteCallback callback =
					(x, y) => Dispatcher.BeginInvoke(new DirectoryScanner.ScanCompleteCallback(DirectoryScanComplete), DispatcherPriority.Normal, new object[] { x, y });
			m_scanner.BeginScan(m_installPath, callback);
		}

		private void DirectoryScanComplete(List<FileInfo> results, bool error)
		{
			if (error)
			{
				Log("Something went wrong when scanning installation directory");
				m_btnRetry.IsEnabled = true;
				return;
			}

			Debug.Assert(m_tasks == null);
			m_tasks = new PatchTasks
			{
				countPatchTasks = 0,
				totalPatchTasks = results.Count + m_patch.assets.Count,
				toDelete = new List<FileInfo>(),
				toDownload = new Dictionary<FileInfo, Uri>(),
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
					if (m_cache.IsFileValid(name, file, patchFile.hash))
					{
						Log_ThreadSafe("\t Valid: " + name + ", " + stamp.ToString());
					}
					else
					{
						Log_ThreadSafe("\t Invalid: " + name + ", get from " + patchFile.uri.ToString());
						m_tasks.toDownload.Add(file, patchFile.uri);
					}
				}
			}
			foreach (var patchFile in m_patch.assets)
			{
				IncrementPatchProgressBar_ThreadSafe(false);
				if(alreadyInspected.Contains(patchFile.Key.ToLowerInvariant()))
					continue;
				Log_ThreadSafe("\t Missing: " + patchFile.Key + ", get from " + patchFile.Value.uri.ToString());
				m_tasks.toDownload.Add(m_cache.GetAbsoluteFile(patchFile.Key), patchFile.Value.uri);
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
				m_btnLaunch.IsEnabled = true;
			}
		}

		private void DoPatchTasks()
		{
			foreach (var file in m_tasks.toDelete)
			{
				file.Delete();
				Log_ThreadSafe("Deleted " + file.FullName);
				IncrementPatchProgressBar_ThreadSafe(false);
			}
			int maxConcurrentDownloads = 1;
			List<object> downloadHandles = new List<object>();
			foreach (var nameInfo in m_tasks.toDownload)
			{
				Log_ThreadSafe("Downloading " + nameInfo.Key);
				SetTaskProgressBar_ThreadSafe(0, "");
				object handle = m_downloader.DownloadFile(nameInfo.Value, nameInfo.Key.FullName, FileDownloaded, ProgressUpdate);
				if (handle == null)
				{
					Log_ThreadSafe("Failed to initiate download of " + nameInfo.Value.ToString());
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
		private void FileDownloaded(Uri uri, byte[] raw, string error)
		{
			SetTaskProgressBar_ThreadSafe(100, "");
			IncrementPatchProgressBar_ThreadSafe(error != null);
			if (error != null)
			{
				Log_ThreadSafe("Failed to download "+uri.ToString()+": "+error);
				return;
			}
		}
		private void ProgressUpdate(long recieved, long total, int percent, DateTime startTime)
		{
			int seconds = (DateTime.Now - startTime).Seconds;
			double kbps = (recieved / 1024.0) / seconds;
			SetTaskProgressBar_ThreadSafe(percent, String.Format("{0}KiB/s", kbps));
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
				m_btnLaunch.IsEnabled = true;
				m_taskStatus.Content = "Ready to launch";
			}
			else
			{
				m_btnRetry.IsEnabled = true;
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

		private void Launch_Click(object sender, RoutedEventArgs e)
		{
			if( m_game != null )
				m_game.Kill();
			m_game = null;
			string dir = Path.GetFullPath(m_a2oaBox.Text);
			string a2path = Path.GetFullPath(m_a2Box.Text);
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

			string steamExe = ReadRegString("Valve\\Steam", "SteamExe");
			if (steamExe != null)
			{
				exe = steamExe;
				args = "-applaunch 219540 " + args;
			}

			m_game = ProcessOutput.Run(exe, args, dir, onExit, false);
			if (m_game != null && !m_game.Finished)
			{
				m_taskStatus.Content = "Game is running...";
				m_btnLaunch.IsEnabled = false;
			}
			else
				m_taskStatus.Content = "Error launching game.";
		}
		private void OnGameExit()
		{
			m_taskStatus.Content = "Ready to launch";
			m_btnLaunch.IsEnabled = true;
		}


		private GameServer gs = new GameServer("81.170.229.148", 2302);
		private void CheckServers()
		{
			gs.Update();
			bool online = gs.IsOnline;
			int numPlayers = 0;
			int maxPlayers = 50;
			string name = "S1";
			if (gs.ServerInfo != null )
			{
				numPlayers = gs.ServerInfo.NumPlayers;
				maxPlayers = gs.ServerInfo.MaxPlayers;
				name = gs.ServerInfo.HostName;
			}
			Dispatcher.BeginInvoke((Action)delegate
			{
				m_serverStatus.Text = String.Format("{0} {1} {2}/{3}", name, online ? "Online" : "Offline", numPlayers, maxPlayers);
			});
		}
	}
}
