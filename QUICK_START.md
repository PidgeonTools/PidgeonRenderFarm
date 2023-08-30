# 🚀Quick start guide
# Welcome to the quick start guide! Here we will show you how you can quickly get Pidgeon Render Farm set up and running.

# Installation
## Download options
**Gumroad**: https://pidgeontools.gumroad.com/l/PidgeonRenderFarm

**GitHub**: https://github.com/PidgeonTools/PidgeonRenderFarm

## Master
- **Download**: Start by downloading the latest release of Pidgeon Render Farm for your operating system. (currently only Windows)
- **Extract**: Once the download is complete, extract the contents of the zip file.
- **Execute**: If you're on Windows, double-click the Master.exe to execute it.
- **Logging**: You'll be prompted to choose if you want to enable logging. It's recommended to set this to 'on' as it can help troubleshoot any issues that may arise.
- **Port Selection**: Next, you'll need to choose which port to use. If you're unsure, you can leave this field empty, and Port 19186 will be used by default.
- **Blender Installation**: Provide the program with your Blender installation by pasting the path of the Blender executable into the program. If you have Blender installed via Steam, you can find this by right-clicking Blender in the library, then going to manage -> Browse local files.
- **Data Collection**: Choose if you want to allow data collection. We have no access to it, it will never leave your device, and is for debugging purposes only. You can find a list of the data that will be collected [here](FAQ.md#do-you-collect-any-data).
- **Ready**: Now your Master is ready to go!

## Client
- **Download**: Begin by downloading the latest release of Pidgeon Render Farm for your operating system. (currently only Windows)
- **Extract**: After the download is complete, extract the contents of the zip file.
- **Execute**: If you're on Windows, double-click the Client.exe to execute it.
- **Logging**: You'll be asked to choose if you want to enable logging. It's recommended to set this to 'on' as it can help troubleshoot any issues that may arise.
- **Master Addition**: Add one Master in the next step. A valid input looks like this: 127.0.0.1:19186. Remember to replace it with your Master's IP and Port. You can find out how to get your IP in the FAQ section.
- **Blender Installation**: Provide the program with your Blender installation by pasting the path of the Blender executable into the program. If you have Blender installed via Steam, you can find this by right-clicking Blender in the library, then going to manage -> Browse local files.
- **Rendering Device**: Choose which device you want to use for rendering. You can find a detailed description in the FAQ section.
- **Hybrid Rendering**: If you decided to use your GPU for rendering, you now have the option to enable hybrid rendering. It's worth trying out, as it only helps for some devices.
- **Render Engines**: Stand by for a second and choose whether you want to pick allowed render engines on your own, or if you want to just allow all. If you choose to select, you can pick the engines to allow one by one from the list. To finish your selection, use the "That is it, I don't want to allow more engines" option.
- **Data Collection**: Choose if you want to allow data collection. We have no access to it, it will never leave your device, and is for debugging purposes only. You can find a list of the data that will be collected [here](FAQ.md#do-you-collect-any-data).
- **Ready**: Hit 'Start Client' and your Client is ready!

Remember, you may need to configure your firewall settings to allow Pidgeon Render Farm to operate correctly. You can find more details about this in the [troubleshooting section](TROUBLESHOOTING.md#server-socket-wont-start) of the Pidgeon Render Farm documentation.

## Project Setup
- **New Project**: In the main menu, choose 'New Project'.
- **.Blend File**: Enter the path of your .Blend file.
- **Super Fast Render**: Decide whether you want to use 'Super Fast Render' to optimize the scene before rendering.
- **Test Frame**: Decide if you want the Master to render a test frame. This test render will be used to calculate the estimated time per frame. If you have no limit on your Client, this step may not be necessary.
- **Batch Size**: Pick a batch size. This must be a number greater than 0. The batch size determines how many frames are rendered at once. Instead of rendering only one frame and then reporting to the master, the Client will render a set of frames (the batch) and after all frames are done it will report back and provide the master with the results.We recommend: 15
- **Project Information**: The master now opens your project to obtain information like the selected render engine and Blender version.
- **Waiting for Clients**: After this is done, your Master now waits for Clients to connect and start rendering.

Remember, each step in the setup process is crucial to ensure that your project renders correctly. If you encounter any issues, refer to the troubleshooting section of the Pidgeon Render Farm documentation or reach out to their support team.

## Info
Do you have questions or encounter any problems? [Contact us on Discord!](https://discord.gg/cnFdGQP)