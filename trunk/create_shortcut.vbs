' Create Shortcut to latest Release Binary

Set oShell = CreateObject("WScript.Shell")
sDesktop = oShell.SpecialFolders("Desktop")
sReleaseDate = WScript.Arguments.Item(0)
sReleasePath = WScript.Arguments.Item(1)

Set oShortCut = oShell.CreateShortcut( sReleasePath & "\..\Bumblebee GUI.lnk")
oShortCut.TargetPath = sReleasePath & "\bin\BumblebeeAlphaGUI.exe"
oShortCut.WorkingDirectory = sReleasePath & "\bin"
oShortCut.Save

Set oShortCut = oShell.CreateShortcut( sDesktop & "\Bumblebee GUI.lnk")
oShortCut.TargetPath = sReleasePath & "\bin\BumblebeeAlphaGUI.exe"
oShortCut.WorkingDirectory = sReleasePath & "\bin"
oShortCut.Save

MsgBox "BumblebeeAlphaGUI shortcut is created/updated.", vbInformation + vbSystemModal, "Create shortcut"

