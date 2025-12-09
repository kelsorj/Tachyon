@echo off
echo compiling ...
javac -cp bin\xmlrpc-common-3.1.3.jar;bin\xmlrpc-client-3.1.3.jar;bin\xmlrpc-server-3.1.3.jar -d classes HiveClientServerTest.java HiveClientException.java HiveClientInterface.java HiveClient.java HiveServerException.java HiveServerInterface.java HiveServer.java HiveServerCallbackInterface.java JemsImpl.java

if ERRORLEVEL 1 GOTO end
cd classes

jar cfe ..\bin\bionex.jar BioNex.HiveClientServerTest BioNex/HiveClient.class BioNex/HiveClientInterface.class BioNex/HiveClientException.class BioNex/HiveClientServerTest.class BioNex/HiveServerException.class BioNex/HiveServerInterface.class BioNex/HiveServer.class BioNex/HiveServer$1.class BioNex/HiveServerCallbackInterface.class  BioNex/JemsImpl.class

cd ..

:end