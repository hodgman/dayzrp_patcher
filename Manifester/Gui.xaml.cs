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
		}
		private void projectPathBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			Properties.Settings.Default.projectPath = projectPathBox.Text;
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

		private void Browse_Click(object sender, RoutedEventArgs e)
		{
			m_folderBrowser.SelectedPath = projectPathBox.Text;
			m_folderBrowser.Description = "Select the project path";
			Forms.DialogResult result = m_folderBrowser.ShowDialog();
			if (result == Forms.DialogResult.OK)
			{
				projectPathBox.Text = m_folderBrowser.SelectedPath;
			}
		}

		private void button1_Click(object sender, RoutedEventArgs e)
		{
			DirectoryScanner.ScanCompleteCallback callback =
					(x, y) => Dispatcher.BeginInvoke(new DirectoryScanner.ScanCompleteCallback(DirectoryScanComplete), DispatcherPriority.Normal, new object[] { x, y });
			m_scanner.BeginScan(projectPathBox.Text, callback);
		}
		private void DirectoryScanComplete(List<FileInfo> results, bool error)
		{
			if (error)
			{
				MessageBox.Show("Something went wrong");
				return;
			}

			var patcher = ProcessOutput.Run(patcherExeBox.Text, "-version", "", null, false, false);
			string patcherVersion = "[Fix me by hand]";
			if (patcher != null)
			{
				patcher.Wait();
				patcherVersion = patcher.stdout.Aggregate((i, j) => i + "\n" + j);
			}

			XmlDocument doc = new XmlDocument();
			XmlElement rootNode = doc.CreateElement("patch");
			XmlAttribute dataDir = doc.CreateAttribute("data");
			XmlAttribute launcherVersion = doc.CreateAttribute("launcherVersion");
			XmlAttribute launcherUrl = doc.CreateAttribute("launcherUrl");
			launcherVersion.Value = patcherVersion;
			launcherUrl.Value = Path.GetFileName(patcherExeBox.Text);
			dataDir.Value = dataDirBox.Text;
			rootNode.Attributes.Append(dataDir);
			rootNode.Attributes.Append(launcherUrl);
			rootNode.Attributes.Append(launcherVersion);
			doc.AppendChild(rootNode);
			foreach (var file in results)
			{
				XmlElement fileNode = doc.CreateElement("file");
				XmlAttribute path = doc.CreateAttribute("path");
				XmlAttribute hash = doc.CreateAttribute("hash");
				string nameValue = GetRelativeName(file);
				string hashValue;
				using (Stream stream = file.OpenRead())
				{
					hashValue = StringExtension.ToHexString(m_md5.ComputeHash(stream));
					stream.Close();
				}
				path.Value = nameValue;
				hash.Value = hashValue;
				fileNode.Attributes.Append(path);
				fileNode.Attributes.Append(hash);
				rootNode.AppendChild(fileNode);
			}
			doc.Save(outputBox.Text);
		}
		private string GetRelativeName(FileInfo file)
		{
			Uri root = new Uri(projectPathBox.Text + "/");
			Uri uri = new Uri(file.FullName);
			uri = root.MakeRelativeUri(uri);
			return uri.ToString().ToLowerInvariant();
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
	}
}
