using System;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Security.Cryptography;

namespace util
{
	public class FileHashDatabase
	{
		public static FileHashDatabase Load(FileInfo file)
		{
			FileHashDatabase result = new FileHashDatabase();
			FileStream stream = null;
			try
			{
				stream = file.OpenRead();
				result.m_files = stream.Deserialize<Dictionary<string, HashStamp>>();
			}
			catch (Exception)
			{
				result.m_files = new Dictionary<string, HashStamp>();
			}
			finally
			{
				if (stream != null)
					stream.Close();
			}
			result.m_root = new Uri(file.Directory.FullName + "/");
			return result;
		}
		public void Save(FileInfo file)
		{
			FileStream stream = null;
			try
			{
				if (file.Exists)
					file.Delete();
				stream = file.OpenWrite();
				stream.Serialize(m_files);
				stream.Flush();
			}
			catch (Exception) { }
			finally
			{
				if (stream != null)
					stream.Close();
			}
		}

		public string GetRelativeName(FileInfo file)
		{
			Uri uri = new Uri(file.FullName);
			uri = m_root.MakeRelativeUri(uri);
			return uri.ToString().ToLowerInvariant();
		}
		public FileInfo GetAbsoluteFile(string name)
		{
			Uri uri;
			try
			{
				if (Uri.TryCreate(m_root, name, out uri))
					return new FileInfo(uri.LocalPath);
			}
			catch (System.Exception ex)
			{
				ex.ToString();
			}
			return new FileInfo(Path.Combine(m_root.LocalPath, name));
		}

		public bool IsFileValid(string relativeName, FileInfo file, byte[] expectedHash)
		{
			HashStamp info;
			file.Refresh();
			if (!m_files.TryGetValue(relativeName, out info) || info.stamp != file.LastWriteTimeUtc)
			{//file not yet hashed/cached, or hash out of date
				info = new HashStamp();
				info.hash = ComputeHash(file);
				info.stamp = file.LastWriteTimeUtc;
				//	m_files.Add(relativeName, info);
				m_files[relativeName] = info;
			}
			return ArrayCompare(info.hash, expectedHash);
		}
		#region internal
		private static bool ArrayCompare(byte[] a, byte[] b)
		{
			if (a.Length != b.Length)
				return false;
			if (a.Equals(b))
				return true;
			for (int i = 0; i != a.Length; ++i)
				if (a[i] != b[i])
					return false;
			return true;
		}

		private byte[] ComputeHash(FileInfo file)
		{
			Stream stream = null;
			try
			{
				stream = file.OpenRead();
				return m_md5.ComputeHash(stream);
			}
			catch (Exception) { }
			finally
			{
				if (stream != null)
					stream.Close();
			}
			return new byte[0];
		}

		[Serializable]
		struct HashStamp
		{
			public byte[] hash;
			public DateTime stamp;
		}
		private MD5 m_md5 = MD5.Create();
		private Uri m_root;
		private Dictionary<string, HashStamp> m_files;
		#endregion
	}
}