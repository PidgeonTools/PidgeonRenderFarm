# ⚠️Troubleshooting
# Welcome to the quick start guide! Here we will show you solutions for the most common problems.

## Windows: Pidgeon Render Farm won't start (until 0.1.0-beta)
Since PRF is built using C#, which uses the dotnet-SDK, you will need to install the .NET 6.0 Runtime. You can download it from [here](https://dotnet.microsoft.com/download/dotnet/6.0). To learn more please visit the [FAQ](FAQ.md#what-is-dotnet-net).

## Server socket won't start
In most cases this is due to the settings of your **firewall**. You can see if it is the case for you by following the steps below. If that doesn't work visit our [Discord Server](https://discord.gg/cnFdGQP).

### Windows
Click "🛡️Allow access"

<img src="images/windows-security.png" width="512"/>

or go to ``Control Panel`` -> ``System and Security`` -> ``Windows Defender Firewall`` -> ``Advanced settings`` -> add your custom (TCP)-port to the firewall.

### Linux
Run the following commands. It will add an **firewall exception**. Be sure to **replace ``<your port>``** with the one you set in the settings!

``firewall-cmd --permanent --add-port=<your port>/tcp``

``firewall-cmd --reload``

### MacOS
It is the easiest to just disable the firewall entirely.

## Info
Do you have questions or encounter any problems? [Contact us on Discord!](https://discord.gg/cnFdGQP)