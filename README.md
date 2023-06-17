# Pidgeon Render Farm
<img src="images/PRF_light.png" width="512"/>

Pidgeon Render Farm is an innovative, **peer-to-peer** render farm software that empowers you to harness the computational power of **multiple devices**, such as your MacBook, desktop, and laptop, to render a single Blender project. This software operates on a **local network**, **eliminating the need for an internet connection and third-party servers**.

### Note
You may have to configure your firewall. [See troubleshooting section for more details](https://github.com/PidgeonTools/PidgeonRenderFarm#server-socket-wont-start).

## What makes Pidgeon Render Farm stand out?
- **Customizability**: Pidgeon Render Farm is highly customizable, allowing you to tailor the software to your specific needs.
- **Integration**: It integrates seamlessly with other addons by PidgeonTools, enhancing its functionality.
- **Security**: The peer-to-peer nature of the software ensures your data stays secure and only on your devices.
- **Support**: Free support is available to help you navigate any challenges.
- **Compatibility**: The software supports many operating systems, including modern Windows (7 and above), Linux, and MacOS.
- **Freeware**: Pidgeon Render Farm will be freeware, making it accessible to all.

## Why Pidgeon Render Farm?
By choosing Pidgeon Render Farm, you're choosing a solution that's secure, customizable, and supportive of your local render farm needs.

## Requirements
- **RAM:**        <50 MB + RAM for Blender
- **Storage:**    ~200 kb (for Client and Master each) + storage for projects
- **Network:**    **No internet** connection required, just a local network

### Operating System
Modern **Windows** (7 and above), **Linux** and **MacOS** are supported. Though only the Windows and Linux versions are tested.

-> You help testing by using the render farm and filling out [this form](https://docs.google.com/forms/d/e/1FAIpQLSf77tLntJEPKpRnBiI6ITCFw2YQwGMHTeO-uuuhtMg1rG7fdA/viewform?usp=sf_link), creating an [issue on GitHub](https://github.com/PidgeonTools/PidgeonRenderFarm/issues/new?template=bug_report.md) or by contacting us on [Discord](https://discord.gg/cnFdGQP)!

## Future Plans
- [ ] Support for software other than Blender
- [ ] ❗ Support for **Multiple Blender** versions
- [x] ❗ Rework render engine system
- [ ] Support for **custom Blender builds** (e.g. E-Cycles)
- [x] ❗ Support for **custom render engines** (e.g. Radeon Pro Render)
- [ ] Support for non-animation projects
- [ ] ❗ Master handling **multiple connections**
- [x] Bandwidth saving mode (**Chunks**)
- [x] Automatically detecting render engine
- [ ] 💡 Automatic Blender download
- [x] GUI (**Super Render Farm**)

❗ = important

💡 = just a rough idea

⏱️ = halted

## Installation
### Master
1. **Download**: Start by downloading the latest release of Pidgeon Render Farm for your operating system. (currently only Windows)
2. **Extract**: Once the download is complete, extract the contents of the zip file.
3. **Execute**: If you're on Windows, double-click the Master.exe to execute it.
4. **Logging**: You'll be prompted to choose if you want to enable logging. It's recommended to set this to 'on' as it can help troubleshoot any issues that may arise.
5. **Port Selection**: Next, you'll need to choose which port to use. If you're unsure, you can leave this field empty, and Port 19186 will be used by default.
6. **Blender Installation**: Provide the program with your Blender installation by pasting the path of the Blender executable into the program. If you have Blender installed via Steam, you can find this by right-clicking Blender in the library, then going to manage -> Browse local files.
7. **Data Collection**: Choose if you want to allow data collection. We have no access to it, it will never leave your device, and is for debugging purposes only. You can see a list of the data that will be collected [here](https://github.com/PidgeonTools/PidgeonRenderFarm#do-you-collect-any-data).
8. **Ready**: Now your Master is ready to go!

### Client
1. **Download**: Begin by downloading the latest release of Pidgeon Render Farm for your operating system. (currently only Windows)
2. **Extract**: After the download is complete, extract the contents of the zip file.
3. **Execute**: If you're on Windows, double-click the Client.exe to execute it.
4. **Logging**: You'll be asked to choose if you want to enable logging. It's recommended to set this to 'on' as it can help troubleshoot any issues that may arise.
5. **Master Addition**: Add one Master in the next step. A valid input looks like this: ``127.0.0.1:19186``. Remember to replace it with your Master's IP and Port. You can find out how to get your IP in the FAQ section.
6. **Blender Installation**: Provide the program with your Blender installation by pasting the path of the Blender executable into the program. If you have Blender installed via Steam, you can find this by right-clicking Blender in the library, then going to ``manage`` -> ``Browse local files``.
7. **Rendering Device**: Choose which device you want to use for rendering. You can find a detailed description in the FAQ section.
8. **Hybrid Rendering**: If you decided to use your GPU for rendering, you now have the option to enable hybrid rendering. It's worth trying out, as it only helps for some devices.
9. **Render Engines**: Stand by for a second and choose whether you want to pick allowed render engines on your own, or if you want to just allow all. If you choose to select, you can pick the engines to allow one by one from the list. To finish your selection, use the "That is it, I don't want to allow more engines" option.
10. **Data Collection**: Choose if you want to allow data collection. We have no access to it, it will never leave your device, and is for debugging purposes only. You can see a list of the data that will be collected [here](https://github.com/PidgeonTools/PidgeonRenderFarm#do-you-collect-any-data).
11. **Ready**: Hit 'Start Client' and your Client is ready!

Remember, you may need to configure your firewall settings to allow Pidgeon Render Farm to operate correctly. You can find more details about this in the troubleshooting section of the Pidgeon Render Farm documentation.

## Project Setup
1. **New Project**: In the main menu, choose 'New Project'.
2. **.Blend File**: Enter the path of your .Blend file.
3. **Super Fast Render**: Decide whether you want to use 'Super Fast Render' to optimize the scene before rendering.
4. **Test Frame**: Decide if you want the Master to render a test frame. This test render will be used to calculate the estimated time per frame. If you have no limit on your Client, this step may not be necessary.
5. **Chunk Size**: Pick a chunk size. This must be a number greater than 0. The chunk size determines how many frames are rendered at once. Instead of rendering only one frame and then reporting to the master, the Client will render a set of frames (the chunk) and after all frames are done it will report back and provide the master with the results. We recommend: 15
6. **Project Information**: The master now opens your project to obtain information like the selected render engine and Blender version.
7. **Waiting for Clients**: After this is done, your Master now waits for Clients to connect and start rendering.

Remember, each step in the setup process is crucial to ensure that your project renders correctly. If you encounter any issues, refer to the troubleshooting section of the Pidgeon Render Farm documentation or reach out to their support team.

## Troubleshooting
### Server socket won't start
In most cases this is due to the settings of your **firewall**. You can see if it is the case for you by following the steps below. If that doesn't work visit our [Discord Server](https://discord.gg/cnFdGQP).

#### Windows
Click "🛡️Allow access"

<img src="images/windows-security.png" width="512"/>

or go to ``Control Panel`` -> ``System and Security`` -> ``Windows Defender Firewall`` -> ``Advanced settings`` -> add your custom (TCP)-port to the firewall.

#### Linux
Run the following commands. It will add an **firewall exception**. Be sure to **replace ``<your port>``** with the one you set in the settings!

``firewall-cmd --permanent --add-port=<your port>/tcp``

``firewall-cmd --reload``

#### MacOS
It is the easiest to just dissable the firewall entirely.

## Info
[Do you have questions? Join the Discord!](https://discord.gg/cnFdGQP)

## FAQ
### Why should I turn logging on?
Logging helps in troubleshooting by recording the errors thrown by the compiler. It's recommended to keep it on to help resolve any issues that may arise.

### Where do I find the IP?
When creating a new project, you can find the Master's IP at the top. Alternatively, you can use the command ``ipconfig`` in the command prompt on Windows, ``ip address`` in the terminal on Linux, or go to ``System Preferences`` -> ``Network`` -> ``Select your network`` -> ``Advanced`` -> ``TCP/IP`` on Mac to find your IPv4 Address.

### What is the difference between CRF and CBR?
CRF stands for Constant Quality, which may result in an unpredictable file size. CBR stands for Constant Bitrate, which can lead to somewhat unpredictable quality.

### What does the Chunks feature do/How does it work?
The chunks feature allows the Client to render multiple frames at once. Instead of rendering only one frame and then reporting to the master, the Client will render a set of frames (the chunk) and after all frames are done it will report back and provide the master with the results. It's recommended to use this feature.

### Why would I add multiple Masters in the Client? - Not aviable yet
In case the connection to the main Master fails (because it is offline or you are uploading projects from different machines), the Client will automatically use another connection of the ones you added. It's a nice-to-have feature, but if you don't need it, just add a single connection.

### What are the hardware requirements for the different render devices/APIs?
- **CPU:**	No requirement
- **CUDA:**	Nvidia GPUs with CUDA version >= 3.0 (find the version of yours at https://developer.nvidia.com/cuda-gpus)
- **OptiX:**	Nvidia GPUs with CUDA version >= 5.0 (find the version of yours at https://developer.nvidia.com/cuda-gpus)
- **HIP:**	AMD GPUs with Vega architecture or newer
- **oneAPI:**	Intel Arc A-Series GPU
- **METAL:**	AMD or Intel GPU
- **OPENCL:**	AMD GPU with GCN 2 architecture or newer

### What are the software requirements for the different render devices/APIs?
- **CPU:**	Any Blender version
- **CUDA:**	Any Blender version
- **OptiX:**	Blender 2.81 or newer and driver version 470 or newer
- **HIP:**	Blender 3.0 or newer and Radeon Software 21.12.1 (Windows)/Radeon Software 22.10 or ROCm 5.3 (Linux)
- **oneAPI:**	Blender 3.3 or newer and Intel Graphics Driver 30.0.101.4032 (Windows)/intel-level-zero-gpu package 22.10.24931 (Linux)
- **METAL:**	Blender 3.1 or newer and macOS 13.0 or newer
- **OPENCL:**	Blender 2.93 or older

### Do you collect any data?
**No!** Unless you decide to allow the collection of data in the setup process, but even then the data remains on your system. We **won't have any access** to it! We only have access to the data if you decide to send us your data, which has to be done manually. The data collected contains **anonymous informations** about your system. Full list of data collected: ``OS version``, ``CPU model``, ``GPU model``, ``RAM``

<img src="images/Logo.png" width="512"/>
