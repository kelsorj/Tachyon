# adds MSBuild / WiX target for Deployment configuration to csproj files
from shutil import copy
import os

dirfile = open( "allcsproj.txt")
for csproj in dirfile:
  csproj = csproj.rstrip( '\n')
  print "opening file ", csproj
  inputfile = open( csproj)
  outputfilename = csproj + ".out"
  print "writing file ", outputfilename
  outputfile = open( outputfilename, "w")
  #print "looping over lines in file"
  found_two_line_comment = False
  for line in inputfile:
    if found_two_line_comment:
      outputfile.write( line)
      outputfile.write( "  -->\n")
      found_two_line_comment = False
    elif line.find( "<!-- To modify your") != -1:
      #print "found comment\n"
      outputfile.write( line)
      found_two_line_comment = True
    elif line.find( "<Target Name=\"BeforeBuild\"") != -1:
      #print "found BeforeBuild\n"
      outputfile.write( "  <Target Name=\"BeforeBuild\" Condition=\" '$(Configuration)' == 'Deployment' \">\n")
      outputfile.write( "    <FileUpdate Files=\"Properties\\AssemblyInfo.cs\" Regex=\"(\\d+)\\.(\\d+)\\.(\\d+)\\.(\\d+)\" ReplacementText=\"$1.$2.$3.$(build_vcs_number_1)\" />\n")
    elif line.find( "<Target Name=\"AfterBuild\">") != -1:
      #print "found AfterBuild\n"
      outputfile.write( "  <!--\n")
      outputfile.write( line)
    elif line.find( "<Import Project=\"$(MSBuildToolsPath)") != -1:
      outputfile.write( line)
      outputfile.write( "  <Import Project=\"$(MSBuildExtensionPath)\\MSBuildCommunityTasks\\MSBuild.Community.Tasks.Targets\" />\n")
    else:
      outputfile.write( line)

  outputfile.close()
  print "copying '", outputfilename, "' to '", csproj, "'"
  copy( outputfilename, csproj)
  print "removing '", outputfilename
  os.remove( outputfilename)

  
