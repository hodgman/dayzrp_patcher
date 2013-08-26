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
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using System.Linq;
using System.Linq.Expressions;
using util;
using SevenZip;
using LZMA = SevenZip.Compression.LZMA;

namespace Manifester
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		public Window1()
		{
			InitializeComponent();

			if (!String.IsNullOrEmpty(Properties.Settings.Default.output))
				outputBox.Text = Properties.Settings.Default.output;
			if (!String.IsNullOrEmpty(Properties.Settings.Default.dataDir))
				dataDirBox.Text = Properties.Settings.Default.dataDir;
			if (!String.IsNullOrEmpty(Properties.Settings.Default.projectPath))
				projectPathBox.Text = Properties.Settings.Default.projectPath;
			if (!String.IsNullOrEmpty(Properties.Settings.Default.patcherExe))
				patcherExeBox.Text = Properties.Settings.Default.patcherExe;
			if (!String.IsNullOrEmpty(Properties.Settings.Default.destPath))
				destPathBox.Text = Properties.Settings.Default.destPath;
		}
		private void projectPathBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			Properties.Settings.Default.projectPath = projectPathBox.Text;
			Properties.Settings.Default.Save();
		}
		private void destPathBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			Properties.Settings.Default.destPath = destPathBox.Text;
			Properties.Settings.Default.Save();
		}
		private void dataDirBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			Properties.Settings.Default.dataDir = dataDirBox.Text;
			Properties.Settings.Default.Save();
		}
		private void patcherExeBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			Properties.Settings.Default.patcherExe = patcherExeBox.Text;
			Properties.Settings.Default.Save();
		}
		private void outputBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			Properties.Settings.Default.output = outputBox.Text;
			Properties.Settings.Default.Save();
		}

		private Forms.OpenFileDialog m_fileBrowser = new Forms.OpenFileDialog();
		private Forms.FolderBrowserDialog m_folderBrowser = new Forms.FolderBrowserDialog();
		private DirectoryScanner m_scanner = new DirectoryScanner();
		private MD5 m_md5 = MD5.Create();

		private void BrowseSource_Click(object sender, RoutedEventArgs e)
		{
			m_folderBrowser.SelectedPath = projectPathBox.Text;
			m_folderBrowser.Description = "Select the source path";
			Forms.DialogResult result = m_folderBrowser.ShowDialog();
			if (result == Forms.DialogResult.OK)
			{
				projectPathBox.Text = m_folderBrowser.SelectedPath;
			}
		}
		private void BrowseDest_Click(object sender, RoutedEventArgs e)
		{
			m_folderBrowser.SelectedPath = projectPathBox.Text;
			m_folderBrowser.Description = "Select the destination path";
			Forms.DialogResult result = m_folderBrowser.ShowDialog();
			if (result == Forms.DialogResult.OK)
			{
				destPathBox.Text = m_folderBrowser.SelectedPath;
			}
		}
		private void BrowseLauncher_Click(object sender, RoutedEventArgs e)
		{
			m_fileBrowser.InitialDirectory = Path.GetDirectoryName(patcherExeBox.Text);
			m_fileBrowser.FileName = patcherExeBox.Text;
			m_fileBrowser.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
			m_fileBrowser.RestoreDirectory = true;
			if (m_fileBrowser.ShowDialog() == Forms.DialogResult.OK)
				patcherExeBox.Text = m_fileBrowser.FileName;
		}

		private void button1_Click(object sender, RoutedEventArgs e)
		{
			builtButton.IsEnabled = false;
			statusText.Text = "Scanning " + projectPathBox.Text;
			DirectoryScanner.ScanCompleteCallback callback =
					(x, y) => Dispatcher.BeginInvoke(new DirectoryScanner.ScanCompleteCallback(DirectoryScanComplete), DispatcherPriority.Normal, new object[] { x, y });
			m_scanner.BeginScan(projectPathBox.Text, callback);
		}
		private void DirectoryScanComplete(List<FileInfo> results, bool error)
		{
			if (error)
			{
				builtButton.IsEnabled = true;
				MessageBox.Show("Something went wrong");
				return;
			}
			var task = new Action<List<FileInfo>, string[]>(CreateManifest);
			task.BeginInvoke(results, new string[] { dataDirBox.Text, patcherExeBox.Text, destPathBox.Text, outputBox.Text, projectPathBox.Text }, null, this);
		}

		private void SetStatus(string text, double progress)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action<string, double>)delegate(string t, double p)
			{
				this.statusText.Text = t;
				this.progressBar.Value = p * this.progressBar.Maximum;
			}, text, progress);
		}

		private void CreateManifest(List<FileInfo> results, string[] args)
		{
			try
			{
				CreateManifest_(results, args);
			}
			catch (System.Exception ex)
			{
				SetStatus("Something went wrong: "+ex.ToString(), 1);
			}
		}
		private void CreateManifest_(List<FileInfo> results, string[] args)
		{
			string dataDir = args[0];
			string patcherExe = args[1];
			string destPath = args[2];
			string outputFilename = args[3];
			string projectPath = args[4];
			SetStatus("Getting patcher version", 0);
			ProcessOutput patcher = null;
			try
			{
				patcher = ProcessOutput.Run(patcherExe, "-version", "", null, false, false);
			}
			catch (System.Exception e) { }
			string patcherVersion = "[Fix me by hand]";
			if (patcher != null)
			{
				patcher.Wait();
				patcherVersion = patcher.stdout.Aggregate((i, j) => i + "\n" + j);
			}
			
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

			SetStatus("Starting XML file...", 0);

			XmlDocument doc = new XmlDocument();
			XmlElement rootNode = doc.CreateElement("dayzrp");
			XmlElement serversNode = doc.CreateElement("servers");
			XmlElement patchNode = doc.CreateElement("patch");
			XmlAttribute data = doc.CreateAttribute("data");
			XmlAttribute launcherVersion = doc.CreateAttribute("launcherVersion");
			XmlAttribute launcherUrl = doc.CreateAttribute("launcherUrl");
			launcherVersion.Value = patcherVersion;
			launcherUrl.Value = Path.GetFileName(patcherExe);
			data.Value = dataDir;
			patchNode.Attributes.Append(data);
			patchNode.Attributes.Append(launcherUrl);
			patchNode.Attributes.Append(launcherVersion);

			XmlComment comment = doc.CreateComment("<server name='RP1 : S1' host='81.170.227.227' port='2302'/>");
			serversNode.AppendChild(comment);

			rootNode.AppendChild(serversNode);
			rootNode.AppendChild(patchNode);
			doc.AppendChild(rootNode);

			int progress = 0;
			double progressCount = (double)results.Count * 2;
			foreach (var file in results)
			{
				XmlElement fileNode = doc.CreateElement("file");
				XmlAttribute path = doc.CreateAttribute("path");
				XmlAttribute hash = doc.CreateAttribute("hash");
				string nameValue = GetRelativeName(file, projectPath);
				string hashValue;
				SetStatus("Hashing " + nameValue, progress++ / progressCount);
				using (Stream stream = file.OpenRead())
				{
					hashValue = StringExtension.ToHexString(m_md5.ComputeHash(stream));
					stream.Close();
				}
				path.Value = nameValue;
				hash.Value = hashValue;
				fileNode.Attributes.Append(path);
				fileNode.Attributes.Append(hash);
				patchNode.AppendChild(fileNode);

				SetStatus("Compressing " + nameValue, progress++ / progressCount);
				using (Stream inStream = file.OpenRead())
				{
					string outFile = Path.Combine(destPath, nameValue);
					EnsurePathExists(outFile);
					Stream outStream = new FileStream(outFile, FileMode.Create, FileAccess.Write);

					encoder.Code(inStream, outStream, -1, -1, null);
				}
			}
			SetStatus("Saving XML...", 1);
			doc.Save(outputFilename);
			SetStatus("Done!", 1);


			Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)delegate()
			{
				builtButton.IsEnabled = true;
			});
		}
		private void EnsurePathExists(string path)
		{
			try
			{
				FileInfo file = new FileInfo(path);
				file.Directory.Create();
			}
			catch (System.Exception ex)
			{
			}
		}
		private string GetRelativeName(FileInfo file, string projectPath)
		{
			Uri root = new Uri(projectPath + "/");
			Uri uri = new Uri(file.FullName);
			uri = root.MakeRelativeUri(uri);
			return uri.ToString().ToLowerInvariant();
		}
	}
}
