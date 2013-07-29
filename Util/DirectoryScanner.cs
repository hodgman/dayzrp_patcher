using System;
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
	class DirectoryScanner
	{
		public delegate void ScanCompleteCallback(List<FileInfo> results, bool error);
		public void BeginScan(string path, ScanCompleteCallback onComplete)
		{
			AbortScan();
			m_root = path;
			m_onComplete = onComplete;
			m_abort = false;
			m_error = false;
			m_workerThread = new Thread(ThreadMain);
			try
			{
				m_workerThread.Start();
			}
			catch (Exception)
			{
				m_error = true;
				m_abort = true;
				m_onComplete(new List<FileInfo>(), m_error);
				m_workerThread = null;
			}
		}
		public void AbortScan()
		{
			if (m_workerThread != null)
			{
				m_abort = true;
				m_workerThread.Join();
				m_workerThread = null;
			}
		}
		public bool IsScanning()
		{
			if (m_abort)
				return false;
			return m_workerThread.IsAlive;
		}
		#region internal
		private void ThreadMain()
		{
			List<FileInfo> files = CollectFiles(m_root);
			bool aborted = m_abort;
			m_abort = true;
			m_onComplete(files, m_error);
		}

		private string m_root;
		private volatile bool m_abort = false;
		private bool m_error = false;
		private Thread m_workerThread = null;
		ScanCompleteCallback m_onComplete;
		private List<FileInfo> CollectFiles(string path)
		{
			List<FileInfo> files = new List<FileInfo>();
			try
			{
				CollectFiles(new DirectoryInfo(path), files);
			}
			catch
			{
				m_error = true;
			}
			return files;
		}
		private void CollectFiles(DirectoryInfo dir, List<FileInfo> output)
		{
			if (m_abort)
				throw new Exception();
			DirectoryInfo[] subdirs = dir.GetDirectories();
			foreach (var subdir in subdirs)
			{
				CollectFiles(subdir, output);
			}
			FileInfo[] files = dir.GetFiles();
			foreach (var file in files)
			{
				output.Add(file);
			}
		}
		#endregion
	}
}