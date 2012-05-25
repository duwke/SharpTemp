c# access to http://code.google.com/p/tc4-shield/ 

Current features -
Graph amabient, T0 probe, and T1 probe
Alarm for T0 and T1 (email)
Webserver - json of ambient, t0, t1, rate0, rate1, alarm1, alarm2
JQuery Mobile plotting

TODO - 
Add Mobile alarm management (notifications?)
On close - export to CSV
Installer

Known issues -
On close, the COM port sticks and you have to force close.  
I read there was some .Net COM bug, but there must be a workaround.
T1 alarm sends the T0 temp.

This project is currently in source code only, so you must have visual studio 2010 to get it running.  In addition, you must have a tc4-shield, programmed with the latest "bourbon" arduino code from 
http://code.google.com/p/tc4-shield/downloads/list
TC4-StdLib-REL-300.zip & aBourbon-REL-300.zip

Once you have the tc4 attached, you must set the app.config with the following settings - 
gmail_email, gmail_password, com_port
com_port is required.  If you want alarms to work, the gmail settings are required.

On start, it should send you an email letting you know that it's begun, and sending you the mobile link.  You can use this to view the graph and alarms on your phone.


