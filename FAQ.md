# ❓Frequently Asked Questions
# Welcome to the FAQ! Here we will answer the most common questions.

## Where can I download Pidgeon Render Farm?
**Gumroad**: https://pidgeontools.gumroad.com/l/PidgeonRenderFarm

**GitHub**: https://github.com/PidgeonTools/PidgeonRenderFarm

## Where can I track the development progress?
https://github.com/orgs/PidgeonTools/projects/5

## Where can I see the development goals (roadmap)?
https://github.com/PidgeonTools/PidgeonRenderFarm/milestones

## What is Pidgeon Render Farm?
Pidgeon Render Farm is an innovative, **peer-to-peer** render farm software that empowers you to harness the computational power of **multiple devices**, such as your MacBook, desktop, and laptop, to render a single Blender project. This software operates on a local network, **eliminating the need for an internet connection and third-party servers**.

## What makes Pidgeon Render Farm stand out?
- **Free and Open Source**: Pidgeon Render Farm is open source, making it accessible to all - you can confirm the security and quality of Pidgeon Render Farm on your own.
- **Customizability**: Pidgeon Render Farm is highly customizable, allowing you to tailor the software to your specific needs.
- **Integration**: It integrates seamlessly with other addons by PidgeonTools, enhancing its functionality.
- **Security**: The peer-to-peer nature of the software ensures your data stays secure and only on your devices.
- **Support**: Free support is available to help you navigate any challenges.
- **Compatibility**: The software supports many operating systems, including modern Windows (7 and above), Linux, and MacOS.

## What are the system requirements?
- **RAM**:	<50 MB RAM + RAM for Blender
- **Storage**: ~60 MB (for Client and Master each) + storage for projects
- **Network**: **No internet** connection required, just a local network

### Operating System
Modern **Windows** (10 and above) and **Linux** are actively supported. MacOS binaries are not aviable, as we want to ensure that you receive a tested product. You can still obtain the binaries by asking on [our Discord server](https://discord.gg/cnFdGQP)

## How can I help the development?
You can [donate on PayPal](https://www.paypal.me/kevinlorengel), [buy Pidgeon Render Farm on Gumroad](https://pidgeontools.gumroad.com/l/PidgeonRenderFarm) and you can help by testing Pidgeon Render Farm and filling out [this form](https://app.formbricks.com/s/cljn7iccc0023qs0h9sxtjpc4), creating an [issue on GitHub](https://github.com/PidgeonTools/PidgeonRenderFarm/issues/new/choose) or by contacting us on [Discord](https://discord.gg/cnFdGQP)!

## Why should I turn logging on?
Logging helps in troubleshooting by recording the errors thrown by the compiler. It's recommended to keep it on to help resolve any issues that may arise.

## Where do I find the IP?
When creating a new project, you can find the Master's IP at the top. Alternatively, you can use the command ``ipconfig`` in the command prompt on Windows, ``ip address`` in the terminal on Linux, or go to ``System Preferences`` -> ``Network`` -> Select your network -> ``Advanced`` -> ``TCP/IP`` on Mac to find your IPv4 Address.

## What is the difference between CRF and CBR?
CRF stands for Constant Quality, which may result in an unpredictable file size. CBR stands for Constant Bitrate, which can lead to somewhat unpredictable quality.

## What does the Batch (formerly Chunks) feature do/How does it work?
The chunks feature allows the Client to render multiple frames at once. Instead of rendering only one frame and then reporting to the master, the Client will render a set of frames (the chunk) and after all frames are done it will report back and provide the master with the results. It's recommended to use this feature.

## Why would I add multiple Masters in the Client?
In case the connection to the main Master fails (because it is offline or you are uploading projects from different machines), the Client will automatically use another connection of the ones you added. It's a nice-to-have feature, but if you don't need it, just add a single connection.

## What are the hardware requirements for the different render devices/APIs?
- **CPU**: No requirement
- **CUDA**: Nvidia GPUs with CUDA version >= 3.0
- **OptiX**: Nvidia GPUs with CUDA version >= 5.0
- **HIP**: AMD GPUs with Vega architecture or newer
- **oneAPI**: Intel Arc A-Series GPU
- **METAL**: AMD or Intel GPU
- **OPENCL**: AMD GPU with GCN 2 architecture or newer

[Find the CUDA version of your Nvidia GPU](https://developer.nvidia.com/cuda-gpus)

## What are the software requirements for the different render devices/APIs?
- **CPU**: Any Blender version
- **CUDA**: Any Blender version
- **OptiX**: Blender 2.81 or newer and driver version 470 or newer
- **HIP**: Blender 3.0 or newer and Radeon Software 21.12.1 (Windows)/Radeon Software 22.10 or ROCm 5.3 (Linux)
- **oneAPI**: Blender 3.3 or newer and Intel Graphics Driver 30.0.101.4032 (Windows)/intel-level-zero-gpu package 22.10.24931 (Linux)
- **METAL**: Blender 3.1 or newer and macOS 13.0 or newer
- **OPENCL**: Blender 2.93 or older

### What is dotnet (.NET)?
".NET is an open source developer platform, created by Microsoft, for building many different types of applications."
It is required in order to execute PRF, but the binaries include .NET, so you don't have to install it on your system seperately.

## Do you collect any data?
**No!** Unless you decide to allow the collection of data in the setup process, but even then the data remains on your system. We **won't have any access to it**! We only have access to the data if you decide to send us your data, which has to be done manually. The data collected contains **anonymous informations** about your system. Full list of data collected: ``PRF version``, ``OS name (e.g. Windows, Fedora)``, ``OS version``, ``System architecture``, ``CPU cores``, ``GPU count``, ``RAM``

## Info
Do you have questions or encounter any problems? [Contact us on Discord!](https://discord.gg/cnFdGQP)