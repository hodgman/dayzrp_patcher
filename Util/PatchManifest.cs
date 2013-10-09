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
	public class PatchManifest
	{
		public string mode;
		public Uri launcherUri;
		public string launcherVersion;
		public string dataDir;
		public class ServerInfo
		{
			public string name, host;
			public int port;
		}
		public List<ServerInfo> servers = null;
		public class AssetInfo
		{
			public Uri uri;
			public byte[] hash;
		}
		public Dictionary<string, AssetInfo> assets = new Dictionary<string, AssetInfo>();
		public AssetInfo GetFileInfo(string name)
		{
			AssetInfo asset;
			assets.TryGetValue(name.ToLowerInvariant(), out asset);
			return asset;
		}
		public static PatchManifest Parse(XmlDocument doc, string baseUrl)
		{
			Uri baseUri = new Uri(baseUrl);
			PatchManifest patchInfo = new PatchManifest();
			XmlNode xmlPatch = doc.SelectSingleNode("patch|dayzrp/patch");
			patchInfo.launcherVersion = xmlPatch.Attribute("launcherVersion", "");
			string launcherUrl = xmlPatch.Attribute("launcherUrl", "");
			patchInfo.mode = xmlPatch.Attribute("mode", "");
			if (!Uri.TryCreate(baseUri, launcherUrl, out patchInfo.launcherUri))
				return null;
			patchInfo.dataDir = xmlPatch.Attribute("data", "");
			XmlNodeList xmlFiles = xmlPatch.SelectNodes("file");
			foreach (XmlNode xmlFile in xmlFiles)
			{
				string path = xmlFile.Attribute("path", "");
				string hash = xmlFile.Attribute("hash", "");
				string defaultUrl = Path.Combine(baseUrl, patchInfo.dataDir + "/" + path);
				string url = xmlFile.Attribute("url", defaultUrl);
				if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(hash) || String.IsNullOrEmpty(url))
					return null;
				Uri uri;
				if (!Uri.TryCreate(baseUri, url, out uri))
					return null;
				PatchManifest.AssetInfo asset = new PatchManifest.AssetInfo
				{
					uri = uri,
					hash = hash.HexToBytes()
				};
				patchInfo.assets.Add(path.ToLowerInvariant(), asset);
			}
			XmlNode xmlServers = doc.SelectSingleNode("servers|dayzrp/servers");
			if (xmlServers != null)
			{
				patchInfo.servers = new List<ServerInfo>();
				XmlNodeList xmlServerList = xmlServers.SelectNodes("server");
				foreach (XmlNode xmlServer in xmlServerList)
				{
					string host = xmlServer.Attribute("host", "");
					if(String.IsNullOrEmpty(host))
						continue;
					string name = xmlServer.Attribute("name", host);
					string port = xmlServer.Attribute("port", "2302");
					int intPort = 2302;
					int.TryParse(port, out intPort);
					patchInfo.servers.Add(new ServerInfo
						{
							name = name,
							host = host,
							port = intPort
						});
				}
			}
			return patchInfo;
		}
	}
}