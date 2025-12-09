' Create Shortcut to latest Synapsis Release Binary

Set oShell = CreateObject("WScript.Shell")
sDesktop = oShell.SpecialFolders("Desktop")
sReleaseDate = WScript.Arguments.Item(0)
sReleasePath = WScript.Arguments.Item(1)

Set oShortCut = oShell.CreateShortcut( sDesktop & "\Synapsis.lnk")
oShortCut.TargetPath = sReleasePath & "\bin\SynapsisPrototype.exe"
oShortCut.WorkingDirectory = sReleasePath & "\bin"
oShortCut.Save

MsgBox "Synapsis Desktop Shortcut is created/updated.", vbInformation + vbSystemModal, "Create shortcut"

