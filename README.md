# Pidgeon Render Farm
Pidgeon Render Farm is a p2p (peer to peer - no third party) render farm software. Because no third party server is involved, you don't need an internet connection, just a local network. It allows you to use the computation power of multiple machines (e.g. macBook, desktop and laptop) to render on one (Blender) project. For now you can only render animations with RRF.

### Note
You may have to configure your firewall. [See troubleshooting section for more details](#server-socket-wont-start).

## Requirements
RAM: ~50 MB + RAM for Blender

Storage: ~100 kb + storage for projects

Network: No internet connection required, just a local network

Python: >= 3 required

Python: >= 3.8 recommended

### Operating System
Modern Windows (7 and above), Linux and MacOS are supported. Though only the Windows and Linux versions are tested.

-> You help testing by using the render farm and creating an [issue on GitHub](https://github.com/PidgeonTools/PidgeonRenderFarm/issues/new?template=bug_report.md) or by contacting us on [Discord](https://discord.gg/cnFdGQP)!

## Future Plans
- [ ] Support for software other than Blender
- [ ] ❗ Support for Multiple Blender versions
- [ ] Rework render engine system
- [ ] Support for custom Blender builds (e.g. E-Cycles)
- [ ] Support for custom render engines (e.g. Radeon Pro Render)
- [ ] Support for non animation projects
- [ ] ⏱️ Master handling multiple connections
- [x] Bandwidth saving mode (Chunks)
- [x] Automatically detecting render engine
- [ ] 💡 Automatic Blender download
- [x] GUI (SuperRenderFarm)

❗ = important

💡 = just a rough idea

⏱️ = halted

## Setup
### Client
1. Install ``pillow`` using ``pip install pillow``

2. Run ``Client.py`` and answer the questions

### Master
1. Install ``pillow`` using ``pip install pillow``

2. Install ``ffmpeg-python`` using ``pip install ffmpeg-python``

3. Download FFMPEG from https://ffmpeg.org/download.html

4. Decompress the downloaded file

5. Run ``Master.py`` and answer the questions

## Troubleshooting
### Server socket won't start
#### Windows
Click "🛡️Allow access"

![Alt text](/images/windows-security.png)

or go to "Control Panel" -> "System and Security" -> "Windows Defender Firewall" -> "Advanced settings" -> add your custom (TCP)-port to the firewall.

#### Linux
Run the following commands. It will add an firewall exception.

``firewall-cmd --permanent --add-port=<your port>/tcp``

``firewall-cmd --reload``

#### MacOS
It is the easiest to just dissable the firewall entirely.

## Changelog
- Added the "Chunks" feature

## Info
[Do you have questions? Join the Discord!](https://discord.gg/cnFdGQP)

## FAQ
### Why are the PIP packages not automatically installed?
Because of legal reasons.

### Why do I need to install FFMPEG?
You need FFMPEG to automatically generate a video. This is an optional feature. If you don't want to use this feature, you don't have to install FFMPEG. It is recommended to install it anyway.

### Why do I need to install Pillow?
You need Pillow to check the rendered output. This is not optional yet.

### What is the FFMPEG directory?
The directory is the subdirectory "bin" of the file you downloaded and decompressed in step 3 and 4 of the master setup. E.g. "c:/Program Files/FFMPEG/bin"

### Do you collect any data?
No!