echo "Please remove the folder ../Release first, else you might encounter errors"
read -n1 -r -p "Press any key to continue..." key

echo "Preparing (gitignored) output directory"
mkdir ../Release

#//////////

echo "Building Client..."

echo "Building Windows x64..."
dotnet publish ../Client/Client.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime win-x64
mv ../Client/bin/Release/net6.0/win-x64/publish ../Release/PRF_Client_Windows
cp ../LICENSE ../Release/PRF_Client_Windows
cp ../Client/Get_Engines.py ../Release/PRF_Client_Windows
cp ../Client/SID_Temoral_Bridge.py ../Release/PRF_Client_Windows
tar -cf ../Release/PRF_Client_Windows.zip ../Release/PRF_Client_Windows

echo "Building Windows arm..."
dotnet publish ../Client/Client.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime win-arm
mv ../Client/bin/Release/net6.0/win-arm/publish ../Release/PRF_Client_Windows_ARM
cp ../LICENSE ../Release/PRF_Client_Windows_ARM
cp ../Client/Get_Engines.py ../Release/PRF_Client_Windows_ARM
cp ../Client/SID_Temoral_Bridge.py ../Release/PRF_Client_Windows_ARM
zip -r ../Release/PRF_Client_Windows_ARM.zip ../Release/PRF_Client_Windows_ARM

echo "Building Linux x64..."
dotnet publish ../Client/Client.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime linux-x64
mv ../Client/bin/Release/net6.0/linux-x64/publish ../Release/PRF_Client_Linux
cp ../LICENSE ../Release/PRF_Client_Linux
cp ../Client/Get_Engines.py ../Release/PRF_Client_Linux
cp ../Client/SID_Temoral_Bridge.py ../Release/PRF_Client_Linux
zip -r ../Release/PRF_Client_Linux.zip ../Release/PRF_Client_Linux

echo "Building Linux arm..."
dotnet publish ../Client/Client.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime linux-arm
mv ../Client/bin/Release/net6.0/linux-arm/publish ../Release/PRF_Client_Linux_ARM
cp ../LICENSE ../Release/PRF_Client_Linux_ARM
cp ../Client/Get_Engines.py ../Release/PRF_Client_Linux_ARM
cp ../Client/SID_Temoral_Bridge.py ../Release/PRF_Client_Linux_ARM
zip -r ../Release/PRF_Client_Linux_ARM.zip ../Release/PRF_Client_Linux_ARM

echo "Building Mac x64..."
dotnet publish ../Client/Client.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime osx-x64
mv ../Client/bin/Release/net6.0/osx-x64/publish ../Release/PRF_Client_Mac
cp ../LICENSE ../Release/PRF_Client_Mac
cp ../Client/Get_Engines.py ../Release/PRF_Client_Mac
cp ../Client/SID_Temoral_Bridge.py ../Release/PRF_Client_Mac
zip -r ../Release/PRF_Client_Mac.zip ../Release/PRF_Client_Mac

echo "Building Mac arm..."
dotnet publish ../Client/Client.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime osx-arm64
mv ../Client/bin/Release/net6.0/osx-arm64/publish ../Release/PRF_Client_Mac_ARM
cp ../LICENSE ../Release/PRF_Client_Mac_ARM
cp ../Client/Get_Engines.py ../Release/PRF_Client_Mac_ARM
cp ../Client/SID_Temoral_Bridge.py ../Release/PRF_Client_Mac_ARM
zip -r ../Release/PRF_Client_Mac_ARM.zip ../Release/PRF_Client_Mac_ARM

#//////////

echo "Building Master..."

echo "Building Windows x64..."
dotnet publish ../Master/Master.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime win-x64
mv ../Master/bin/Release/net6.0/win-x64/publish ../Release/PRF_Master_Windows
cp ../LICENSE ../Release/PRF_Master_Windows
cp ../Master/BPY.py ../Release/PRF_Master_Windows
cp ../Master/SID_Temoral_Bridge.py ../Release/PRF_Master_Windows
cp ../Master/Get_Version.py ../Release/PRF_Master_Windows
zip -r ../Release/PRF_Master_Windows.zip ../Release/PRF_Master_Windows

echo "Building Windows arm..."   
dotnet publish ../Master/Master.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime win-arm
mv ../Master/bin/Release/net6.0/win-arm/publish ../Release/PRF_Master_Windows_ARM
cp ../LICENSE ../Release/PRF_Master_Windows_ARM
cp ../Master/BPY.py ../Release/PRF_Master_Windows_ARM
cp ../Master/SID_Temoral_Bridge.py ../Release/PRF_Master_Windows_ARM
cp ../Master/Get_Version.py ../Release/PRF_Master_Windows_ARM
zip -r ../Release/PRF_Master_Windows_ARM.zip ../Release/PRF_Master_Windows_ARM

echo "Building Linux x64..."
dotnet publish ../Master/Master.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime linux-x64
mv ../Master/bin/Release/net6.0/linux-x64/publish ../Release/PRF_Master_Linux
cp ../LICENSE ../Release/PRF_Master_Linux
cp ../Master/BPY.py ../Release/PRF_Master_Linux
cp ../Master/SID_Temoral_Bridge.py ../Release/PRF_Master_Linux
cp ../Master/Get_Version.py ../Release/PRF_Master_Linux
zip -r ../Release/PRF_Master_Linux.zip ../Release/PRF_Master_Linux

echo "Building Linux arm..."
dotnet publish ../Master/Master.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime linux-arm
mv ../Master/bin/Release/net6.0/linux-arm/publish ../Release/PRF_Master_Linux_ARM
cp ../LICENSE ../Release/PRF_Master_Linux_ARM
cp ../Master/BPY.py ../Release/PRF_Master_Linux_ARM
cp ../Master/SID_Temoral_Bridge.py ../Release/PRF_Master_Linux_ARM
cp ../Master/Get_Version.py ../Release/PRF_Master_Linux_ARM
zip -r ../Release/PRF_Master_Linux_ARM.zip ../Release/PRF_Master_Linux_ARM

echo "Building Mac x64..."
dotnet publish ../Master/Master.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime osx-x64
mv ../Master/bin/Release/net6.0/osx-x64/publish ../Release/PRF_Master_Mac
cp ../LICENSE ../Release/PRF_Master_Mac
cp ../Master/BPY.py ../Release/PRF_Master_Mac
cp ../Master/SID_Temoral_Bridge.py ../Release/PRF_Master_Mac
cp ../Master/Get_Version.py ../Release/PRF_Master_Mac
zip -r ../Release/PRF_Master_Mac.zip ../Release/PRF_Master_Mac

echo "Building Mac arm..."
dotnet publish ../Master/Master.csproj --configuration Release --self-contained true -p:PublishSingleFile=true --runtime osx-arm64
mv ../Master/bin/Release/net6.0/osx-arm64/publish ../Release/PRF_Master_Mac_ARM
cp ../LICENSE ../Release/PRF_Master_Mac_ARM
cp ../Master/BPY.py ../Release/PRF_Master_Mac_ARM
cp ../Master/SID_Temoral_Bridge.py ../Release/PRF_Master_Mac_ARM
cp ../Master/Get_Version.py ../Release/PRF_Master_Mac_ARM
zip -r ../Release/PRF_Master_Mac_ARM.zip ../Release/PRF_Master_Mac_ARM

echo "Build completed!"
read -n1 -r -p "Press any key to continue..." key