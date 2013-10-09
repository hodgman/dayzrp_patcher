Exe's require DotNet framework 3.5, which should be standard on Win7.
For earlier versions of windows, the DL link is:
http://www.microsoft.com/en-au/download/details.aspx?id=21


To set up a patch server:
* Make launcher.dayzrp.com accessible
* Copy DayZRP.exe into this directory (i.e. launcher.dayzrp.com/DayZRP.exe)
* Make a new subdirectory "files" (i.e. launcher.dayzrp.com/files)

* Run the manifester tool.
** See manifester_settings.png
** The source path should be your @DayZRP directory.
** Destination directory will receive a compressed version of the @DayZRP files.
** Server data dir is the name of your subdirectory (e.g. 'files').
** The output file must be called "DayZRP.xml".
** Hit build.

* Copy the resulting DayZRP.xml file to the server (i.e. launcher.dayzrp.com/DayZRP.xml)
* Copy the produced 'destination' files to the server (i.e. launcher.dayzrp.com/files/...)

You should end up with:
http://launcher.dayzrp.com/DayZRP.xml
http://launcher.dayzrp.com/DayZRP.exe
http://llauncher.dayzrp.com/files/... <-- all the stuff from the "destination" directory, e.g:
http://launcher.dayzrp.com/files/mod.cpp.lzma
http://launcher.dayzrp.com/files/mod.paa.lzma
http://launcher.dayzrp.com/files/Addons/acex_sm_c_sound_men.pbo.lzma
http://launcher.dayzrp.com/files/Addons/...


To change the server browser list:
* In DayZRP.xml, there will be a section like this:
  <servers>
    <!--<server name='RP1 : S1' host='81.170.227.227' port='2302'/>-->
  </servers>
* Change it to something like this:
  <servers>
	<server name="RP1 : S1" host="81.170.233.200" port="2302"/>
	<server name="RP1 : S2" host="81.170.233.202" port="2302"/>
  </servers>
* The above two entries are hard-coded into the client. If no servers are found in the DayZRP.xml file, these two hard-coded entries are used.



To test out the launcher:
* If you want to use a different test server than launcher.dayzrp.com, then go to the about tab and type 'devmode'. The option to enter the server URL manually will appear!
* Back up / rename your @DayZRP installation :)
* Put DayZRP.exe somewhere on your client machine.
* Run it. It should download http://launcher.dayzrp.com/DayZRP.xml and get to work.
* When it's complete, assuming there's no errors, it should take you to the Launch tab.



Future work -- 
* Clean up the prototype codebase. Separate patcher logic from GUI logic.
* Allow you to use 7Zip yourself, rather than have manifester do the compression.
* Allow multiple files to be downloaded concurrently.
* Add ping display to server list.
* Add bittorrent support ;-P
* Add server time clock, display server restart time warnings.
* Make a better player list display. Maybe a 'buddy' system to tell you which server your friends are on.