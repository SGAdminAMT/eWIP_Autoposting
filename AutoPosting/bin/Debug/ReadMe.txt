1. If want to change between test database and live database, please modify all following parts:
	
	-The test database is using Filter_toreformat.sql, 
	 while the live database is using Filter.sql,
	 modify the file name "At line 166 in the code";

	-comment and uncomment at line (258,259) and (271,272), this part is for checking;

	-modify sessionToken information at line 320 to connect with AMTLive system;

2. For setting the "AutoPosting.exe" run automatically, need to focus on following parts:

	-Win+R and type "taskschd.msc", 
	 press "create task", check the "Run with high privileges" checkbox,
	 under "trigger", choose "At log on",check "Repeat task every" and set 15 minutes,
	 (cuz create file may need authority) 
	 under "Action", give full path of ".exe";

	-!!!!In "Action", need to give full path of the folder which the ".exe" file locates,
	 otherwise the system running the code seems not create file in that folder;