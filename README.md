dayzrp_patcher
==============

IMPORTANT!!!
Whenever you release a new version of the EXE file, be sure to edit App.xaml.cs and change the LauncherVersion property.


Solution files created with MSVC9 (2008).


The Window1 constructor contains the default server list:
			//Default servers to display before the XML file has been downloaded
			m_servers.Add("RP1 : S1", new GameServer("81.170.233.200", 2302));
			m_servers.Add("RP1 : S2", new GameServer("81.170.233.202", 2302));
			
			
The easter eggs are located in Window_TextInput.


Exe's require DotNet framework 3.5, which should be standard on Win7.
For earlier versions of windows, the DL link is:
http://www.microsoft.com/en-au/download/details.aspx?id=21


See the docs folder for usage instructions.
