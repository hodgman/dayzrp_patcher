﻿using System;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;

namespace util
{
	public class ProcessOutput
	{
		private System.Diagnostics.Process proc;
		public readonly List<string> stdout = new List<string>();
		public readonly List<string> stderr = new List<string>();

		public bool Finished { get { return proc.HasExited; } }
		public void Wait()
		{
			if (!Finished)
				proc.WaitForExit();
		}
		public void Kill()
		{
			if (!Finished)
			{
				try { proc.Kill(); }
				catch (Exception) { }
			}
		}

		public static ProcessOutput Run(string executable, string args, string dir, EventHandler onExit, bool runAsAdmin, bool shellExec)
		{
			System.Diagnostics.Process proc = new System.Diagnostics.Process();
			ProcessOutput output = new ProcessOutput { proc = proc };
			if (onExit != null)
				proc.Exited += onExit;
			if (runAsAdmin)
			{
				proc.StartInfo.UseShellExecute = true;
				proc.StartInfo.Verb = "runas";
			}
			else if (shellExec)
			{
				proc.StartInfo.UseShellExecute = true;
			}
			else
			{
				proc.StartInfo.UseShellExecute = false;
				proc.StartInfo.RedirectStandardError = true;
				proc.StartInfo.RedirectStandardOutput = true;
				proc.OutputDataReceived += new DataReceivedEventHandler(output.OnOutput);
				proc.ErrorDataReceived += new DataReceivedEventHandler(output.OnError);
			}
			proc.StartInfo.FileName = executable;
			proc.StartInfo.Arguments = args;
			proc.StartInfo.CreateNoWindow = true;
			proc.StartInfo.WorkingDirectory = dir;
			proc.EnableRaisingEvents = true;
			proc.Start();
			if (!runAsAdmin && !shellExec)
			{
				proc.BeginOutputReadLine();
				proc.BeginErrorReadLine();
			}
			return output;
		}

		public static ProcessOutput RunSafe(string executable, string args, string dir, EventHandler onExit, bool runAsAdmin, bool shellExec)
		{
			try
			{
				return Run(executable, args, dir, onExit, runAsAdmin, shellExec);
			}
			catch (Exception)
			{
				return null;
			}
		}
		private void OnOutput(object proc, DataReceivedEventArgs data)
		{
			if (!string.IsNullOrEmpty(data.Data))
				stdout.Add(data.Data);
		}
		private void OnError(object proc, DataReceivedEventArgs data)
		{
			if (!string.IsNullOrEmpty(data.Data))
				stderr.Add(data.Data);
		}
	}
}