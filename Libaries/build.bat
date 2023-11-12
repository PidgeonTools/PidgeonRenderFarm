@echo off
echo Please remove the folder "../Release" first, else you might encounter errors
pause

echo Preparing (gitignored) output directory
mkdir ..\Release

:: \\\\\\\\\\

echo Building Client...

echo Building Windows x64...
dotnet publish ..\Client\Client.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime win-x64
move ..\Client\bin\Release\net6.0\win-x64\publish ..\Release\PRF_Client_Windows
copy ..\LICENSE ..\Release\PRF_Client_Windows
copy ..\Client\Get_Engines.py ..\Release\PRF_Client_Windows
copy ..\Client\SID_Temoral_Bridge.py ..\Release\PRF_Client_Windows
tar -cf ..\Release\PRF_Client_Windows.zip ..\Release\PRF_Client_Windows

echo Building Windows arm...
dotnet publish ..\Client\Client.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime win-arm
move ..\Client\bin\Release\net6.0\win-arm\publish ..\Release\PRF_Client_Windows_ARM
copy ..\LICENSE ..\Release\PRF_Client_Windows_ARM
copy ..\Client\Get_Engines.py ..\Release\PRF_Client_Windows_ARM
copy ..\Client\SID_Temoral_Bridge.py ..\Release\PRF_Client_Windows_ARM
tar -cf ..\Release\PRF_Client_Windows_ARM.zip ..\Release\PRF_Client_Windows_ARM

echo Building Linux x64...
dotnet publish ..\Client\Client.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime linux-x64
move ..\Client\bin\Release\net6.0\linux-x64\publish ..\Release\PRF_Client_Linux
copy ..\LICENSE ..\Release\PRF_Client_Linux
copy ..\Client\Get_Engines.py ..\Release\PRF_Client_Linux
copy ..\Client\SID_Temoral_Bridge.py ..\Release\PRF_Client_Linux
tar -cf ..\Release\PRF_Client_Linux.zip ..\Release\PRF_Client_Linux

echo Building Linux arm...
dotnet publish ..\Client\Client.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime linux-arm
move ..\Client\bin\Release\net6.0\linux-arm\publish ..\Release\PRF_Client_Linux_ARM
copy ..\LICENSE ..\Release\PRF_Client_Linux_ARM
copy ..\Client\Get_Engines.py ..\Release\PRF_Client_Linux_ARM
copy ..\Client\SID_Temoral_Bridge.py ..\Release\PRF_Client_Linux_ARM
tar -cf ..\Release\PRF_Client_Linux_ARM.zip ..\Release\PRF_Client_Linux_ARM

echo Building Mac x64...
dotnet publish ..\Client\Client.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime osx-x64
move ..\Client\bin\Release\net6.0\osx-x64\publish ..\Release\PRF_Client_Mac
copy ..\LICENSE ..\Release\PRF_Client_Mac
copy ..\Client\Get_Engines.py ..\Release\PRF_Client_Mac
copy ..\Client\SID_Temoral_Bridge.py ..\Release\PRF_Client_Mac
tar -cf ..\Release\PRF_Client_Mac.zip ..\Release\PRF_Client_Mac

echo Building Mac arm...
dotnet publish ..\Client\Client.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime osx-arm64
move ..\Client\bin\Release\net6.0\osx-arm64\publish ..\Release\PRF_Client_Mac_ARM
copy ..\LICENSE ..\Release\PRF_Client_Mac_ARM
copy ..\Client\Get_Engines.py ..\Release\PRF_Client_Mac_ARM
copy ..\Client\SID_Temoral_Bridge.py ..\Release\PRF_Client_Mac_ARM
tar -cf ..\Release\PRF_Client_Mac_ARM.zip ..\Release\PRF_Client_Mac_ARM

:: \\\\\\\\\\

echo Building Master...

echo Building Windows x64...
dotnet publish ..\Master\Master.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime win-x64
move ..\Master\bin\Release\net6.0\win-x64\publish ..\Release\PRF_Master_Windows
copy ..\LICENSE ..\Release\PRF_Master_Windows
copy ..\Master\BPY.py ..\Release\PRF_Master_Windows
copy ..\Master\SID_Temoral_Bridge.py ..\Release\PRF_Master_Windows
copy ..\Master\Get_Version.py ..\Release\PRF_Master_Windows
tar -cf ..\Release\PRF_Master_Windows.zip ..\Release\PRF_Master_Windows

echo Building Windows arm...   
dotnet publish ..\Master\Master.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime win-arm
move ..\Master\bin\Release\net6.0\win-arm\publish ..\Release\PRF_Master_Windows_ARM
copy ..\LICENSE ..\Release\PRF_Master_Windows_ARM
copy ..\Master\BPY.py ..\Release\PRF_Master_Windows_ARM
copy ..\Master\SID_Temoral_Bridge.py ..\Release\PRF_Master_Windows_ARM
copy ..\Master\Get_Version.py ..\Release\PRF_Master_Windows_ARM
tar -cf ..\Release\PRF_Master_Windows_ARM.zip ..\Release\PRF_Master_Windows_ARM

echo Building Linux x64...
dotnet publish ..\Master\Master.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime linux-x64
move ..\Master\bin\Release\net6.0\linux-x64\publish ..\Release\PRF_Master_Linux
copy ..\LICENSE ..\Release\PRF_Master_Linux
copy ..\Master\BPY.py ..\Release\PRF_Master_Linux
copy ..\Master\SID_Temoral_Bridge.py ..\Release\PRF_Master_Linux
copy ..\Master\Get_Version.py ..\Release\PRF_Master_Linux
tar -cf ..\Release\PRF_Master_Linux.zip ..\Release\PRF_Master_Linux

echo Building Linux arm...
dotnet publish ..\Master\Master.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime linux-arm
move ..\Master\bin\Release\net6.0\linux-arm\publish ..\Release\PRF_Master_Linux_ARM
copy ..\LICENSE ..\Release\PRF_Master_Linux_ARM
copy ..\Master\BPY.py ..\Release\PRF_Master_Linux_ARM
copy ..\Master\SID_Temoral_Bridge.py ..\Release\PRF_Master_Linux_ARM
copy ..\Master\Get_Version.py ..\Release\PRF_Master_Linux_ARM
tar -cf ..\Release\PRF_Master_Linux_ARM.zip ..\Release\PRF_Master_Linux_ARM

echo Building Mac x64...
dotnet publish ..\Master\Master.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime osx-x64
move ..\Master\bin\Release\net6.0\osx-x64\publish ..\Release\PRF_Master_Mac
copy ..\LICENSE ..\Release\PRF_Master_Mac
copy ..\Master\BPY.py ..\Release\PRF_Master_Mac
copy ..\Master\SID_Temoral_Bridge.py ..\Release\PRF_Master_Mac
copy ..\Master\Get_Version.py ..\Release\PRF_Master_Mac
tar -cf ..\Release\PRF_Master_Mac.zip ..\Release\PRF_Master_Mac

echo Building Mac arm...
dotnet publish ..\Master\Master.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime osx-arm64
move ..\Master\bin\Release\net6.0\osx-arm64\publish ..\Release\PRF_Master_Mac_ARM
copy ..\LICENSE ..\Release\PRF_Master_Mac_ARM
copy ..\Master\BPY.py ..\Release\PRF_Master_Mac_ARM
copy ..\Master\SID_Temoral_Bridge.py ..\Release\PRF_Master_Mac_ARM
copy ..\Master\Get_Version.py ..\Release\PRF_Master_Mac_ARM
tar -cf ..\Release\PRF_Master_Mac_ARM.zip ..\Release\PRF_Master_Mac_ARM

echo Build completed!
pause