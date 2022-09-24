# NEXUS
This is a C# "dot net" programming exercise (in Visual Studio 2022) to implement a chat system with a VBA "UserForm" in a PowerPoint
(or other Microsoft Office) file.

The underlying framework is internet server/client communication with Socket.IO (https://socket.io/), with server code for
JavaScript (Node.js). To set up a server, use the code in node.js.code.txt, or refer to the file "replit.txt" to set up a server on replit.com.

The Nexus executable is a Windows console program that connects to the server, and opens "Named Pipes" that can be read/written by 
VBA as generic files.  Socket.IO uses a tag/content structure that Nexus converts to/from a "|tag|content|" string.  Presently,
the server/client message tags in use are only "chat_message" and "control_message".

The VBA subroutine "StartChat" uses the "Shell" command to start the Nexus application, and then opens the "ChatForm" userform.
Since VBA code needs a predictable location to "Shell" the Nexus appplication, a batch command is included in the project to 
copy the necessary files to that location.

With VS2022, the steps to test the project from GitHub would be: "Clone a repository", "Browse a repository", and for "Open from GitHub",
put in: EagleAglow/NexusDotNet, then "Clone". Open: VB_FITS.sln and then build it...

Right-click on the Solution root, and pick "Open folder in file explorer", then double click "CopyToC.bat" - This copies the
application files to C:\NXSNXS.

You can create a new macro-enabled Office file, open the VBA editor, create a code module and a userform, and import the files
"UserFormChat.frm" and "Module1.bas".)

Alternately, double-click "test.pptm" to open the example PowerPoint file.  !!Internet Caution!! You should inspect the VBA code before
allowing macros to run!!
