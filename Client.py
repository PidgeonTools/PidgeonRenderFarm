import json
#import shutil
import subprocess
import sys
import os
import time
#from zipfile import ZipFile
import socket
from PIL import Image
import tqdm

#---Client related---#
client_ip = socket.gethostbyname(socket.gethostname())
print(client_ip)
settings_file = f"client_{client_ip}_settings.json"
settings_object: dict = {}

script_directory = os.path.dirname(os.path.abspath(__file__)) + "/"


async def setup():

    save_settings()
    return


async def save_settings(save_object: dict = {}):
    save_object_base = {
        "Master IP": "192.168.178.117",
        "Master Port": 9090,
        "Fallback Master IP": "192.168.178.90",
        "Fallback Master Port": 9090,
        "Blender Executable": "D:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe",
        "Working Directory": script_directory,
        # CPU, CUDA, OPTIX, HIP, METAL, (OPENCL) / CUDA+CPU, OPTIX+CPU, HIP+CPU, METAL+CPU, (OPENCL+CPU)
        "Render Device": "CPU",
        "Thread Limit": 0,
        "RAM Limit": 0,
        "Size Limit": 0,
        "Allow EEVEE": True,
        "Allow Cycles": True,
        "Allow Workbench": True,
        "Job Limit": 0,
        "Time Limit": 0,
        "Keep Output": True,
        "Keep Input": True,
        "Error Hold": 30,
        "Connection Error Hold": 20,
        "Transfer Error Hold": 5,
    }

    save_object_out = save_object_base | save_object

    global settings_object
    settings_object = save_object_out

    json_string = json.dumps(save_object_out)

    with open(settings_file, "w+") as file_to_write:
        file_to_write.write(json_string)


async def load_settings(again: bool = False):
    if os.path.isfile(settings_file):
        global settings_object

        with open(settings_file, "r") as loaded_settings_file:
            loaded_string = loaded_settings_file.read()
            settings_object = json.loads(loaded_string)

        # print(settings_object)

    else:
        setup()


def client():
    global settings_object

    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    while True:
        # region Connect
        while True:
            try:
                print("Trying to connect to the server...")
                #client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                client_socket.connect(
                    (settings_object["Master IP"], settings_object["Master Port"]))

                break
            except Exception as e:
                print(
                    f'Could not connect to the server, waiting {settings_object["Connection Error Hold"]} seconds')
                # print(str(e))
                time.sleep(settings_object["Connection Error Hold"])
        # endregion

        try:
            # region Server Request
            data_object_to_server = {
                "Message": "New",
                "RAM Limit": settings_object["RAM Limit"],
                "Size Limit": settings_object["Size Limit"],
                "Allow EEVEE": settings_object["Allow EEVEE"],
                "Allow Cycles": settings_object["Allow Cycles"],
                "Allow Workbench": settings_object["Allow Workbench"],
            }
            data_to_server = json.dumps(data_object_to_server)
            client_socket.send(data_to_server.encode())
            # endregion

            # region Server Rersponse
            data_from_server = client_socket.recv(1024).decode()
            data_object_from_server = json.loads(data_from_server)
            # Data structure:
            # Message, project id, frame, engine, file size, (average render time, average ram use)

            print(
                f'Project ID: {data_object_from_server["Project ID"]}; Frame: {data_object_from_server["Frame"]}; Render Engine: {data_object_from_server["Render Engine"]}')
            # endregion

            # region Aquire Project
            if os.path.isfile(f'{data_object_from_server["Project ID"]}.blend'):
                data_object_to_server = {"Message": "File", "Needed": False}
                data_to_server = json.dumps(data_object_to_server)
                client_socket.send(data_to_server.encode())
            else:
                data_object_to_server = {"Message": "File", "Needed": True}
                data_to_server = json.dumps(data_object_to_server)
                client_socket.send(data_to_server.encode())

                while True:
                    try:
                        with open(f'{data_object_from_server["Project ID"]}.blend', "wb") as tcp_download:
                            progress_bar = tqdm.tqdm(range(
                                data_object_from_server["File Size"]), f'Downloading {data_object_from_server["Project ID"]}', unit="B", unit_scale=True, unit_divisor=1024)

                            stream_bytes = client_socket.recv(1024)
                            while stream_bytes:
                                tcp_download.write(stream_bytes)
                                progress_bar.update(len(stream_bytes))

                                stream_bytes = client_socket.recv(1024)

                        break
                    except Exception as e:
                        print(
                            f'ERROR while transfering, waiting {settings_object["Transfer Error Hold"]} seconds')
                        # print(e)
                        time.sleep(settings_object["Transfer Error Hold"])

            client_socket.close()
            # endregion

            # region Render
            time_start = time.now()

            if data_object_from_server["Render Engine"] == "Cycles":
                subprocess.run(f'"{settings_object["Blender Executable"]}" -b "{settings_object["Working Directory"]}{data_object_from_server["Project ID"]}.blend" -o "{settings_object["Working Directory"]}frame_####" --cycles-device {settings_object["Render Device"]} -f {data_object_from_server["Frame"]}', shell=True)
            else:
                subprocess.run(
                    f'"{settings_object["Blender Executable"]}" -b "{settings_object["Working Directory"]}{data_object_from_server["Project ID"]}.blend" -o "{settings_object["Working Directory"]}frame_####" -f {data_object_from_server["Frame"]}', shell=True)

            render_time = time.now() - time_start
            # endregion

            # region Verify
            export_name = "frame_"
            export_name += "0" * (4 - len(data_object_from_server["Frame"]))
            export_name += ".png"

            export_full_name = settings_object["Working Directory"] + export_name

            data_object_to_server = {"Message": "Output"}

            try:
                with Image.open(export_full_name) as test_image:
                    test_image.verify()
                    data_object_to_server["Faulty"] = False

                data_object_to_server["Output Size"] = os.path.getsize(
                    export_full_name)
                data_object_to_server["Project Frame"] = export_name
                data_object_to_server["Render Time"] = render_time
            except:
                print("faulty image detected")
                data_object_to_server["Faulty"] = True
                bad = True
                upload = True
            # endregion

            # region Connect
            while True:
                try:
                    print("Trying to connect to the server...")
                    #client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                    client_socket.connect(
                        (settings_object["Master IP"], settings_object["Master Port"]))

                    break
                except Exception as e:
                    print(
                        f'Could not connect to the server, waiting {settings_object["Connection Error Hold"]} seconds')
                    # print(str(e))
                    time.sleep(settings_object["Connection Error Hold"])
            # endregion

            # region Upload Output
            data_to_server = json.dumps(data_object_to_server)
            client_socket.send(data_to_server.encode())

            # Receive something (e.g. "ok")

            if not data_object_to_server["Faulty"]:
                while True:
                    try:
                        with open(export_full_name, "rb") as tcp_upload:
                            progress_bar = tqdm.tqdm(range(
                                data_object_to_server["Output Size"]), f'Uploading {export_name}', unit="B", unit_scale=True, unit_divisor=1024)

                            stream_bytes = tcp_upload.read(stream_bytes)
                            while stream_bytes:
                                stream_bytes = client_socket.send(stream_bytes)
                                progress_bar.update(len(stream_bytes))
                                stream_bytes = tcp_upload.read(1024)

                        break
                    except Exception as e:
                        print(
                            f'ERROR while transfering, waiting {settings_object["Transfer Error Hold"]} seconds')
                        # print(e)
                        time.sleep(settings_object["Transfer Error Hold"])

            client_socket.close()
            # endregion
        except Exception as e:
            print(
                f'An ERROR occoured, waiting {settings_object["Error Hold"]} seconds')
            # print(str(e))
            time.sleep(settings_object["Error Hold"])


if __name__ == "__main__":
    subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
    subprocess.call([sys.executable, "-m", "pip",
                     "install", "--upgrade", "pip"])
    subprocess.call([sys.executable, "-m", "pip", "install", "pillow"])
    subprocess.call([sys.executable, "-m", "pip", "install", "tqdm"])

    load_settings()

    client()
