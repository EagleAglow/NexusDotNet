Attribute VB_Name = "Module1"
Option Explicit

#If VBA7 Then
    Public Declare PtrSafe Sub Sleep Lib "kernel32" (ByVal dwMilliseconds As LongPtr) 'For 64 Bit Systems
#Else
    Public Declare Sub Sleep Lib "kernel32" (ByVal dwMilliseconds As Long) 'For 32 Bit Systems
#End If

Global NexusRunning As Boolean
Global strPipeName As String
Global strPipeNameIn As String
Global strPipeNameOut As String
Dim strServerURL As String
Dim strShellPath As String

Public Sub StartChat()
Call StartNexus
UserFormChat.Show
End Sub

Public Sub StopNexus()
Dim intFileNum As Integer
intFileNum = FreeFile
Open strPipeNameOut For Output Access Write As intFileNum
Print #intFileNum, "|terminate|nexus|"
Close #intFileNum
End Sub

Public Sub StartNexus()
Dim strCmd As String
Dim i As Integer, wait As Integer
Dim t As Date
Dim RetVal As Variant

strPipeName = "nexus"
strPipeNameIn = "\\.\pipe\" & strPipeName & "In"
strPipeNameOut = "\\.\pipe\" & strPipeName & "Out"

'strServerURL = "http://192.168.1.12:3000"  ' local testing
strServerURL = "https://Socketio-Simple-Chat.brightbird.repl.co"

NexusRunning = False
strShellPath = "C:\NXSNXS\Nexus.exe"
strCmd = strShellPath & " /pipe=" & strPipeName & " /server=" & strServerURL
RetVal = Shell(strCmd, vbMinimizedNoFocus)
' wait (up to 5 seconds) until the task actually starts and gets a task id
wait = 5
t = Now()
For i = 1 To wait
  If RetVal <> 0 Then
    NexusRunning = True
    Exit For    ' proceed
  Else
    If (DateDiff("s", t, Now()) > wait) Then
      MsgBox "Shell failed (wait =  " & wait & " seconds)"
    End If
  End If
Next

End Sub

Sub TestWrite()
Call WriteToPipe("chat_message", "Hello World!")
End Sub

Sub TestRead()
Debug.Print ReadFromPipe
End Sub

Sub WriteToPipe(tag As String, content As String)
Dim info As String
Dim intFileNum As Integer
intFileNum = FreeFile
Open strPipeNameOut For Output Access Write As intFileNum
info = "|" & tag & "|" & content & "|"
Print #intFileNum, info
Close #intFileNum
End Sub

Function ReadFromPipe() As String
Dim info As String
Dim myFileNum As Integer
myFileNum = FreeFile
Open strPipeNameIn For Input As myFileNum
If Not (EOF(myFileNum)) Then
  Input #myFileNum, info
Else
  info = "|||"
End If
Close myFileNum
ReadFromPipe = info
End Function


