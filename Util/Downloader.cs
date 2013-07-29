using System;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Net;
using System.ComponentModel;

namespace util
{
	public class Downloader
	{
		public delegate void DownloadCompleteDelegate(Uri uri, byte[] data, string error);
		public delegate void ProgressChangedDelegate(long recieved, long total, int progress, DateTime startTime);
		public void CancelDownload(object userHandle)
		{
			Handle handle = (Handle)userHandle;
			handle.client.CancelAsync();
			Debug.Assert(!m_idleClients.Contains(handle.client));
			m_idleClients.Add(handle.client);
		}
		public bool IsComplete(object userHandle)
		{
			Handle handle = (Handle)userHandle;
			return handle.complete;
		}
		public object DownloadBytes(Uri uri, DownloadCompleteDelegate onComplete)
		{
			WebClient client = GetIdleClient();
			Handle handle = new Handle(uri, client, onComplete, null);
			try
			{
				client.DownloadDataAsync(uri, handle);
			}
			catch (Exception)
			{
				client.CancelAsync();
				m_idleClients.Add(client);
				return null;
			}
			return handle;
		}
		public object DownloadFile(Uri uri, string fileName, DownloadCompleteDelegate onComplete, ProgressChangedDelegate onProgressChanged)
		{
			WebClient client = GetIdleClient();
			Handle handle = new Handle(uri, client, onComplete, onProgressChanged);
			try
			{
				FileInfo file = new FileInfo(fileName);
				if (file.Exists)
					file.Delete();
				client.DownloadFileAsync(uri, fileName, handle);
			}
			catch (Exception)
			{
				client.CancelAsync();
				m_idleClients.Add(client);
				return null;
			}
			return handle;
		}

		#region internal
		private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			Handle handle = (Handle)e.UserState;
			if (handle.onProgressChanged != null)
				handle.onProgressChanged(e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage, handle.startTime);
		}
		private string ErrorMessage(AsyncCompletedEventArgs e)
		{
			if (e.Cancelled)
				return "Cancelled by user";
			if (e.Error != null)
			{
				if (e.Error.InnerException != null)
					return e.Error.InnerException.Message;
				return e.Error.Message;
			}
			return "?";
		}
		private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
		{
			Handle handle = (Handle)e.UserState;
			Debug.Assert(!m_idleClients.Contains(handle.client));
			handle.complete = true;
			m_idleClients.Add(handle.client);
			if (e.Cancelled || e.Error != null)
			{
				handle.onComplete(handle.uri, null, ErrorMessage(e));
			}
			else
				handle.onComplete(handle.uri, null, null);
		}
		private void DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
		{
			Handle handle = (Handle)e.UserState;
			Debug.Assert(!m_idleClients.Contains(handle.client));
			handle.complete = true;
			m_idleClients.Add(handle.client);
			if (e.Cancelled || e.Error != null)
				handle.onComplete(handle.uri, null, ErrorMessage(e));
			else
				handle.onComplete(handle.uri, e.Result, null);
		}

		private WebClient GetIdleClient()
		{
			int count = m_idleClients.Count;
			if (count == 0)
				return NewClient();
			WebClient client = m_idleClients[count - 1];
			m_idleClients.RemoveAt(count - 1);
			Debug.Assert(!client.IsBusy);
			return client;
		}
		private WebClient NewClient()
		{
			WebClient client = new WebClient();
			client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
			client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadFileCompleted);
			client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadDataCompleted);
			return client;
		}

		private class Handle
		{
			public Handle(Uri uri, WebClient client, DownloadCompleteDelegate onComplete, ProgressChangedDelegate onProgressChanged)
			{
				this.startTime = DateTime.Now;
				this.uri = uri;
				this.client = client;
				this.onComplete = onComplete;
				this.onProgressChanged = onProgressChanged;
				this.complete = false;
			}
			public readonly DateTime startTime;
			public readonly Uri uri;
			public readonly WebClient client;
			public readonly DownloadCompleteDelegate onComplete;
			public readonly ProgressChangedDelegate onProgressChanged;
			public bool complete;
		}

		private List<WebClient> m_idleClients = new List<WebClient>();
		#endregion
	}
}