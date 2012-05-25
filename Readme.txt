c# access to http://code.google.com/p/tc4-shield/ 

Current features -
Graph amabient, T0 probe, and T1 probe
Alarm for T0 and T1 (email)
Webserver - json of ambient, t0, t1, rate0, rate1, alarm1, alarm2

TODO - 
Add JQuery Mobile plotting
Add Mobile alarm management (notifications?)
On close - export to CSV
Installer

Known issues -
On close, the COM port sticks and you have to force close.  
I read there was some .Net COM bug, but there must be a workaround.
T1 alarm sends the T0 temp.

