VERSION 5.00
Begin {C62A69F0-16DC-11CE-9E98-00AA00574A4F} UserFormChat 
   Caption         =   "Chat"
   ClientHeight    =   3600
   ClientLeft      =   120
   ClientTop       =   465
   ClientWidth     =   8505
   OleObjectBlob   =   "UserFormChat.frx":0000
   StartUpPosition =   1  'CenterOwner
End
Attribute VB_Name = "UserFormChat"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit

Private Sub UserForm_Activate()
Dim info As String
' wait a moment
Sleep (1000)  ' millisec
Do While True
  DoEvents
  Sleep (100)  ' millisec
  If NexusRunning Then   ' Nexus has started, check for messages
    info = ReadFromPipe
    If (info <> "|||") Then
      If Left(info, 14) = "|chat_message|" Then
        info = (Mid(info, 15))
        info = Left(info, Len(info) - 1)
        Me.ListBox1.AddItem (info)
      End If
    End If
  End If
Loop
End Sub

Private Sub UserForm_Terminate()
NexusRunning = False  ' block action in chat form loop
Call StopNexus
End Sub

Private Sub btnSend_Click()
If NexusRunning Then   ' Nexus has started, handle button
  If Len(Trim(Me.TextBox1.Text)) > 0 Then
    Call WriteToPipe("chat_message", Trim(Me.TextBox1.Text))
    Me.TextBox1.Text = ""
  End If
End If
End Sub



