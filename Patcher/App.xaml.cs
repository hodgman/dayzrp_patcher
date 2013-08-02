using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Security.Principal;
using System.Security.Permissions;

namespace Patcher
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static string LauncherVersion { get { return "0.1"; } }
		protected override void OnStartup(StartupEventArgs e)
		{
			if (e.Args.Length > 0 && e.Args[0].ToLower() == "-version")
			{
				System.Console.Out.WriteLine(LauncherVersion);
				Application.Current.Shutdown();
			}
			else if (e.Args.Length > 1 && e.Args[0].ToLower() == "-move")
			{
				while (!Move(e.Args[1]))
				{
					string currentExe = System.Reflection.Assembly.GetExecutingAssembly().Location;
					MessageBoxResult r = System.Windows.MessageBox.Show("Failed to move " + currentExe + " to " + e.Args[1] + "\nRetry?", "Uh oh", MessageBoxButton.OKCancel);
					if (r == MessageBoxResult.Cancel)
						break;
				}
			}
		}
		private bool Move(string targetPath)
		{
			string currentExe = System.Reflection.Assembly.GetExecutingAssembly().Location;
			DateTime startTime = DateTime.Now;
			TimeSpan timeout = new TimeSpan(0, 0, 10);
			while (true)
			{
				DateTime now = DateTime.Now;
				if (now - startTime > timeout)
				{
					MessageBox.Show("Failed to move " + currentExe + " to " + targetPath);
					return false;
				}
				try
				{
					if (File.Exists(targetPath))
						File.Delete(targetPath);
					File.Copy(currentExe, targetPath, true);
				}
				catch (Exception)
				{
					continue;
				}
				NativeMethods.MoveFileEx(currentExe, null, MoveFileFlags.DelayUntilReboot);
				return true;
			}
		}
	}
	[Flags]
	internal enum MoveFileFlags
	{
		None = 0,
		ReplaceExisting = 1,
		CopyAllowed = 2,
		DelayUntilReboot = 4,
		WriteThrough = 8,
		CreateHardlink = 16,
		FailIfNotTrackable = 32,
	}

	internal static class NativeMethods
	{
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool MoveFileEx(
			string lpExistingFileName,
			string lpNewFileName,
			MoveFileFlags dwFlags);
	}
}
