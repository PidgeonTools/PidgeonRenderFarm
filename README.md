# Red-Render-Farm
Red Render Farm is a p2p (peer to peer - no third party) render farm software. Because no third party server is involved, you don't need an internet connection, just a local network. It allows you to use the computation power of multiple machines (e.g. macBook, desktop and laptop) to render on one (Blender) project. For now you can only render animations with RRF.

## Requirements
RAM: ~50 MB

Storage: ~100 kb

Network: No internet connection required, just a local network

Python: >= 3.8 recommended

## Future
- Support for software other than Blender
- Support for Multiple Blender versions
- Support for custom Blender builds (e.g. E-Cycles)
- Support for custom render engines (e.g. Radeon Pro Render)
- Support for non animation projects
- Bandwidth saving mode
- Automatically detecting render engine
- Automatic Blender download
- GUI

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

## See README.md
Here you can see the valid options and explainations for the prompts

### Client
"Which device to use for rendering?:"
- "CPU", "CUDA", "OPTIX", "HIP", "METAL", "OPENCL"
- You can add "+CPU" if you want hybrid rendering

"Maximum amount of threads to use (see README.md)?:"
- Used for post processing (and rendering) in Blender
- Will use all aviable if set to 0

"Maximum amount of RAM to use (see README.md)?:"
- NOT implemented yet
- Maximum RAM to use for rendering

"Allow EEVEE rendering on this client?:"
- "Yes", "True", "Y", "1", "No", "False", "N", "0"

"Allow Cycles rendering on this client?:"
- "Yes", "True", "Y", "1", "No", "False", "N", "0"

"Allow Workbench rendering on this client?:"
- "Yes", "True", "Y", "1", "No", "False", "N", "0"

"Keep the rendered and uploaded frames?:"
- This gives you the option to save storage by deleting the rendered frame directly after uploading it
- "Yes", "True", "Y", "1", "No", "False", "N", "0"

"Keep the project files received from the master? (See README.md):"
- This gives you the option to delete the .blend file after every rendered frame
- Recommended: Yes/True
- "Yes", "True", "Y", "1", "No", "False", "N", "0"

### Master
"What is your FFMPEG directory?:"
- In the decompressed folder (Setup -> Master -> 4) there is a folder called ``bin``. Paste it's full path

"Keep the files received from the clients?:"
- This gives you the option to delete all received frames after generating the video file
- Recommended: Yes/True
- "Yes", "True", "Y", "1", "No", "False", "N", "0"

"Which Render Engine does your project use?:"
- "EEVEE", "Cycles", "Workbench"

"Generate a video file?:"
- This gives you the option to automatically generate a video file using FFMPEG
- "Yes", "True", "Y", "1", "No", "False", "N", "0"

"Video Rate Control:"
- CBR gives you a stable bitrate, while CRF gives you a stable quality
- Recommended: CRF
- "CBR", "CRF"

"Change the video resolution?:"
- This gives you the opton to resize the resolution in the video
- "Yes", "True", "Y", "1", "No", "False", "N", "0"

## Info
[Do you have questions? Join the Discord!](https://discord.gg/cnFdGQP)
