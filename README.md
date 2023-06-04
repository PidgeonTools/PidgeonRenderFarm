# Pidgeon Render Farm
<img src="images/Logo.png" width="512"/>

Pidgeon Render Farm is a **P2P** (peer to peer - no third party) render farm software. Because **no third party** server is involved, you don't need an **internet connection**, just a local network. It allows you to use the computation power of multiple machines (e.g. macBook, desktop and laptop) to render on one (Blender) project. For now you can only render animations with RRF.

### Note
You may have to configure your firewall. [See troubleshooting section for more details](https://github.com/PidgeonTools/PidgeonRenderFarm#server-socket-wont-start).

## Requirements
**RAM:**        ~50 MB + RAM for Blender

**Storage:**    ~100 kb + storage for projects

**Network:**    **No internet** connection required, just a local network

### Operating System
Modern **Windows** (7 and above), **Linux** and **MacOS** are supported. Though only the Windows and Linux versions are tested.

-> You help testing by using the render farm and filling out [this form](https://docs.google.com/forms/d/e/1FAIpQLSf77tLntJEPKpRnBiI6ITCFw2YQwGMHTeO-uuuhtMg1rG7fdA/viewform?usp=sf_link), creating an [issue on GitHub](https://github.com/PidgeonTools/PidgeonRenderFarm/issues/new?template=bug_report.md) or by contacting us on [Discord](https://discord.gg/cnFdGQP)!

## Future Plans
- [ ] Support for software other than Blender
- [ ] ❗ Support for **Multiple Blender** versions
- [x] ❗ Rework render engine system
- [ ] Support for **custom Blender builds** (e.g. E-Cycles)
- [ ] ❗ Support for **custom render engines** (e.g. Radeon Pro Render)
- [ ] Support for non-animation projects
- [ ] ❗ Master handling **multiple connections**
- [x] Bandwidth saving mode (**Chunks**)
- [x] Automatically detecting render engine
- [ ] 💡 Automatic Blender download
- [x] GUI (**Super Render Farm**)

❗ = important

💡 = just a rough idea

⏱️ = halted

## Setup
### Client
1. 

### Master
1. 

## Troubleshooting
### Server socket won't start
In most cases this is due to the settings of your **firewall**. You can see if it is the case for you by following the steps below. If that doesn't work visit our [Discord Server](https://discord.gg/cnFdGQP).

#### Windows
Click "🛡️Allow access"

<img src="images/windows-security.png" width="512"/>

or go to "Control Panel" -> "System and Security" -> "Windows Defender Firewall" -> "Advanced settings" -> add your custom (TCP)-port to the firewall.

#### Linux
Run the following commands. It will add an **firewall exception**. Be sure to **replace ``<your port>``** with the one you set in the settings!

``firewall-cmd --permanent --add-port=<your port>/tcp``

``firewall-cmd --reload``

#### MacOS
It is the easiest to just dissable the firewall entirely.

## Info
[Do you have questions? Join the Discord!](https://discord.gg/cnFdGQP)

## FAQ
### Why do I need to install FFMPEG?
You need FFMPEG to automatically generate a video. This is an **optional feature**. If you don't want to use this feature, you don't have to install FFMPEG. It is **recommended** to install it anyway.

### What is the FFMPEG directory?
The directory is the **subdirectory "bin"** of the file you downloaded and decompressed in step 3 and 4 of the master setup. E.g. "c:/Program Files/FFMPEG/bin"

### What is the difference between CRF and CBR?
**CRF:**	Constant **Quality** (unpredictable file size)
**CBR:**	Constant **Bitrate** (somewhat unpredictable quality)

### What does the Chunks feature do/How does it work?

### Why would I add multiple Masters in the Client?
In case the **connection to the main Master fails** (because it is offline or you are uploading projects from different machines), the Client will **automatically** use another connection of the ones you added. In conclusion it is a nice to have, but if you don't need it just add a single connection.

### What are the hardware requirements for the different render devices/APIs?
**CPU:**	No requirement
**CUDA:**	Nvidia GPUs with CUDA version >= 3.0 (find the version of yours at https://developer.nvidia.com/cuda-gpus)
**OptiX:**	Nvidia GPUs with CUDA version >= 5.0 (find the version of yours at https://developer.nvidia.com/cuda-gpus)
**HIP:**	AMD GPUs with Vega architecture or newer
**oneAPI:**	Intel Arc A-Series GPU
**METAL:**	AMD or Intel GPU
**OPENCL:**	AMD GPU with GCN 2 architecture or newer

### What are the software requirements for the different render devices/APIs?
**CPU:**	Any Blender version
**CUDA:**	Any Blender version
**OptiX:**	Blender 2.81 or newer and driver version 470 or newer
**HIP:**	Blender 3.0 or newer and Radeon Software 21.12.1 (Windows)/Radeon Software 22.10 or ROCm 5.3 (Linux)
**oneAPI:**	Blender 3.3 or newer and Intel Graphics Driver 30.0.101.4032 (Windows)/intel-level-zero-gpu package 22.10.24931 (Linux)
**METAL:**	Blender 3.1 or newer and macOS 13.0 or newer
**OPENCL:**	Blender 2.93 or older

### Do you collect any data?
**No!** Unless you decide to allow the collection of data in the setup process, but even then the data remains on your system. We **won't have any access** to it! We only have access to the data if you decide to send us your data, which has to be done manually. The data collected contains **anonymous informations** about your system (e.g. os version, CPU model, installed RAM, GPU model).
