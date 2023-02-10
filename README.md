# Pidgeon Render Farm
Pidgeon Render Farm is a p2p (peer to peer - no third party) render farm software. Because no third party server is involved, you don't need an internet connection, just a local network. It allows you to use the computation power of multiple machines (e.g. macBook, desktop and laptop) to render on one (Blender) project. For now you can only render animations with RRF.

## Requirements
RAM: ~50 MB

Storage: ~100 kb

Network: No internet connection required, just a local network

Python: >= 3.8 recommended

## Future Plans
- [ ] Support for software other than Blender
- [ ] Support for Multiple Blender versions
- [ ] Support for custom Blender builds (e.g. E-Cycles)
- [ ] Support for custom render engines (e.g. Radeon Pro Render)
- [ ] Support for non animation projects
- [ ] Bandwidth saving mode (Chunks)
- [x] Automatically detecting render engine
- [ ] Automatic Blender download
- [x] GUI (SuperRenderFarm)

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

## Changelog

## Info
[Do you have questions? Join the Discord!](https://discord.gg/cnFdGQP)
