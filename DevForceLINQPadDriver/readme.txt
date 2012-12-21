BUILDING THIS PROJECT
=====================
Before building this project, add a reference to the LINQPad executable.
You must use LINQPad 4.x.


RUNNING THIS PROJECT
====================
There are two ways to deploy to your local machine. Either:
   - Zip up DevForceLINQPadDriver.dll and header.xml; rename it to DevForceLINQPadDriver.lpx; go to LINQPad
      and click 'Add Connection', 'View More Drivers', 'Browse...' and select the .lpx file
OR:
   - Edit the DevDeploy.bat file and call DevDeploy from the project post-build event.  When developing and testing 
   this is by far the best approach.


WHAT'S IN THIS PROJECT
======================
This project contains the DevForce LINQPad driver, which is based on the UniversalStaticDriver supplied with LINQPad.


VERSION HISTORY
===============

 1.0.0.0 - First release, January 2012
 1.0.1.0 - Added Code First support (requires DevForce 6.1.5 or higher).  1/18/2012
 1.0.2.0 - Stored procs; complex types; show SQL; IdeaBlade "dynamic LINQ" support; ignore static properties; minor perf improvments;
 1.0.3.0 - Turn off LINQPad assembly shadowing in order to work with LINQPad 4.38.* beta.
 1.0.4.0 - Removed old EM ctor-related code and login.  


MORE INFORMATION
================
LINQPad: http://www.linqpad.net/
Writing a LINQPad Data Context Driver:  http://www.linqpad.net/DataContextPlugin.pdf