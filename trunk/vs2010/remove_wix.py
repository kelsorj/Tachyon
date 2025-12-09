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
  for line in inputfile:
    if line.find( "\\thirdparty\\wix30") != -1:
      print "found wix30 reference\n"
      # comment out the line referencing wix30
      outputfile.write( "<!--" + line + " -->\n")
    else:
      outputfile.write( line)

  outputfile.close()
  print "copying '", outputfilename, "' to '", csproj, "'"
  copy( outputfilename, csproj)
  print "removing '", outputfilename
  os.remove( outputfilename)

  
