import ipaddress
from tqdm import tqdm
from PIL import Image
import socket
import datetime
import time
import os
import json
import subprocess
import sys

subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
subprocess.call([sys.executable, "-m", "pip", "install", "--upgrade", "pip"])
subprocess.call([sys.executable, "-m", "pip", "install", "pillow"])
subprocess.call([sys.executable, "-m", "pip", "install", "tqdm"])

#import shutil
#from zipfile import ZipFile

#---Client related---#
client_ip = socket.gethostbyname(socket.gethostname())
current_date = datetime.datetime.now()
#settings_file = f"client_{client_ip}_settings.json"
settings_file = f"client_settings.json"
log_file = f"session_{current_date.year}{current_date.month}{current_date.day}{current_date.hour}{current_date.minute}{current_date.second}.log"
settings_object: dict = {}

script_directory = os.path.dirname(os.path.abspath(__file__)) + "/"

#---Setup related---#
valid_settings: dict = {
    "Render Device": ["CPU", "CUDA", "OPTIX", "HIP", "METAL", "OPENCL", "CUDA+CPU", "OPTIX+CPU", "HIP+CPU", "METAL+CPU", "OPENCL+CPU"],
}


def setup():
    new_save_object = {}

    user_input = input("What is the IP address of the master?: ")
    while not validate_ip(user_input):
        print("Please select a valid ip address (xxx.xxx.xxx.xxx")
        user_input = input("What is the IP address of the master?: ")
    new_save_object["Master IP"] = user_input

    user_input = input("What is the port of the master?: ")
    while True:
        if user_input.isdigit():
            if int(user_input) >= 1 and int(user_input) <= 65535:
                break

        print("Please input a whole number between 1 and 65536")
        user_input = input("What is the port of the master?: ")
    new_save_object["Master Port"] = int(user_input)

    user_input = input("Where is your Blender executable stored?: ")
    while not os.path.isfile(user_input):
        print("Please select a valid executable")
        user_input = input("Where is your Blender executable stored?: ")
    new_save_object["Blender Executable"] = user_input

    user_input = input("Which directory to use as working directory?: ")
    while not os.path.isdir(user_input):
        print("Please select a valid directory")
        user_input = input("Which directory to use as working directory?: ")
    new_save_object["Working Directory"] = user_input

    user_input = input("Which device to use for rendering?: ")
    while not user_input.upper() in valid_settings["Render Device"]:
        print("Please select an valid option (see README.md)")
        user_input = input("Which Render Engine does your project use?: ")
    new_save_object["Render Device"] = user_input.upper

    user_input = input("Maximum amount of threads to use (see README.md)?: ")
    while not user_input.isdigit():
        print("Please input a whole number")
        user_input = input(
            "Maximum amount of threads to use (see README.md)?: ")
    new_save_object["Thread Limit"] = abs(int(user_input))

    # experimental

    user_input = input("Maximum amount of RAM to use (see README.md)?: ")
    while not user_input.isdigit():
        print("Please input a whole number")
        user_input = input("Maximum amount of RAM to use (see README.md)?: ")
    new_save_object["RAM Limit"] = abs(int(user_input))

    user_input = input("Maximum project dowload size?: ")
    while not user_input.isdigit():
        print("Please input a whole number")
        user_input = input("Maximum project dowload size?: ")
    new_save_object["Size Limit"] = abs(int(user_input))

    user_input = input("Maximum time of rendering per frame?: ")
    while not user_input.isdigit():
        print("Please input a whole number")
        user_input = input("Maximum time of rendering per frame?: ")
    new_save_object["Job Time Limit"] = abs(int(user_input))

    user_input = input("Allow EEVEE rendering on this client?: ")
    while user_input.capitalize() != "True" and user_input.capitalize() != "Yes" and user_input.capitalize() != "False" and user_input.capitalize() != "No":
        print("Please select an valid option (see README.md)")
        user_input = input("Allow EEVEE rendering on this client?: ")
    new_save_object["Allow EEVEE"] = input_to_bool(user_input)

    user_input = input("Allow Cycles rendering on this client?: ")
    while user_input.capitalize() != "True" and user_input.capitalize() != "Yes" and user_input.capitalize() != "False" and user_input.capitalize() != "No":
        print("Please select an valid option (see README.md)")
        user_input = input("Allow Cycles rendering on this client?: ")
    new_save_object["Allow Cycles"] = input_to_bool(user_input)

    user_input = input("Allow Workbench rendering on this client?: ")
    while user_input.capitalize() != "True" and user_input.capitalize() != "Yes" and user_input.capitalize() != "False" and user_input.capitalize() != "No":
        print("Please select an valid option (see README.md)")
        user_input = input("Allow Workbench rendering on this client?: ")
    new_save_object["Allow Workbench"] = input_to_bool(user_input)

    user_input = input("Maximum frames to render?: ")
    while not user_input.isdigit():
        print("Please input a whole number")
        user_input = input("Maximum frames to render?: ")
    new_save_object["Job Limit"] = abs(int(user_input))

    user_input = input("Maximum time of rendering?: ")
    while not user_input.isdigit():
        print("Please input a whole number")
        user_input = input("Maximum time of rendering?: ")
    new_save_object["Time Limit"] = abs(int(user_input))

    user_input = input("Keep the rendered and uploaded frames?: ")
    while user_input.capitalize() != "True" and user_input.capitalize() != "Yes" and user_input.capitalize() != "False" and user_input.capitalize() != "No":
        print("Please select an valid option (see README.md)")
        user_input = input("Keep the rendered and uploaded frames?: ")
    new_save_object["Keep Output"] = input_to_bool(user_input)

    user_input = input(
        "Keep the project files received from the master? (See README.md): ")
    while user_input.capitalize() != "True" and user_input.capitalize() != "Yes" and user_input.capitalize() != "False" and user_input.capitalize() != "No":
        print("Please select an valid option (see README.md)")
        user_input = input(
            "Keep the project files received from the master? (See README.md): ")
    new_save_object["Keep Input"] = input_to_bool(user_input)

    save_settings(new_save_object)


def save_settings(save_object: dict = {}):
    save_object_base = {
        "Master IP": "192.168.178.117",
        "Master Port": 9090,
        "Blender Executable": "D:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe",
        "Working Directory": script_directory,
        # CPU, CUDA, OPTIX, HIP, METAL, (OPENCL) / CUDA+CPU, OPTIX+CPU, HIP+CPU, METAL+CPU, (OPENCL+CPU)
        "Render Device": "CPU",
        "Thread Limit": 0,
        "RAM Limit": 0,
        "Size Limit": 0,
        "Job Time Limit": 0,
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


def load_settings(again: bool = False):
    if os.path.isfile(settings_file):
        global settings_object

        with open(settings_file, "r") as loaded_settings_file:
            loaded_string = loaded_settings_file.read()
            settings_object = json.loads(loaded_string)

        # print(settings_object)

    else:
        setup()

# region Functions


def input_to_bool(inp: str):
    if inp.capitalize() == "True" or inp.capitalize() == "Yes":
        return True
    elif inp.capitalize() == "False" or inp.capitalize() == "No":
        return False


def validate_ip(address: str = "127.0.0.1"):
    try:
        ipaddress.ip_address(address)
        return True
    except:
        return False


def write_to_log(text: str):
    with open(os.path.join(settings_object["Working Directory"], log_file), "a") as writeFile:
        writeFile.write(text + "\n")


def show_progress_bar(base, part):
    tmp = base - part
    bar = "["
    bar += "█" * part
    bar += " " * tmp
    bar += "]"

    print(bar)
# endregion


def client():
    global settings_object

    #client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    while True:
        # region Connect
        while True:
            try:
                print("Trying to connect to the server...")
                client_socket = socket.socket(
                    socket.AF_INET, socket.SOCK_STREAM)
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
            print(os.path.abspath(
                f'{data_object_from_server["Project ID"]}.blend'))
            if os.path.isfile(f'{data_object_from_server["Project ID"]}.blend') and os.path.getsize(f'{data_object_from_server["Project ID"]}.blend') == data_object_from_server["File Size"]:
                data_object_to_server = {"Message": "File", "Needed": False}
                data_to_server = json.dumps(data_object_to_server)
                client_socket.send(data_to_server.encode())
            else:
                data_object_to_server = {"Message": "File", "Needed": True}
                data_to_server = json.dumps(data_object_to_server)
                client_socket.send(data_to_server.encode())

                with open(f'{data_object_from_server["Project ID"]}.blend', "wb") as tcp_download:
                    progress_bar = tqdm(range(
                        data_object_from_server["File Size"]), f'Downloading {data_object_from_server["Project ID"]}', unit="B", unit_scale=True, unit_divisor=1024)

                    stream_bytes = client_socket.recv(1024)
                    while stream_bytes:
                        tcp_download.write(stream_bytes)
                        progress_bar.update(len(stream_bytes))

                        stream_bytes = client_socket.recv(1024)

            client_socket.close()
            # endregion

            # region Render
            time_start = time.time()

            if data_object_from_server["Render Engine"] == "Cycles":
                subprocess.run(
                    f'"{settings_object["Blender Executable"]}" -b "{data_object_from_server["Project ID"]}.blend" -o "{settings_object["Working Directory"]}frame_####" --cycles-device {settings_object["Render Device"]} -f {data_object_from_server["Frame"]}', shell=True)
            else:
                subprocess.run(
                    f'"{settings_object["Blender Executable"]}" -b "{data_object_from_server["Project ID"]}.blend" -o "{settings_object["Working Directory"]}frame_####" -f {data_object_from_server["Frame"]}', shell=True)

            render_time = time.time() - time_start
            # endregion

            # region Verify
            export_name = "frame_"
            export_name += "0" * \
                (4 - len(str(data_object_from_server["Frame"])))
            export_name += str(data_object_from_server["Frame"])
            export_name += ".png"

            print(export_name)

            # os.path.abspath(export_name)
            export_full_name = settings_object["Working Directory"] + export_name

            data_object_to_server = {"Message": "Output"}

            try:
                with Image.open(export_full_name) as test_image:
                    test_image.verify()
                    data_object_to_server["Faulty"] = False

                data_object_to_server["Output Size"] = os.path.getsize(
                    export_full_name)
                data_object_to_server["Frame"] = data_object_from_server["Frame"]
                data_object_to_server["Project Frame"] = export_name
                data_object_to_server["Render Time"] = render_time
            except Exception as e:
                print("faulty image detected")
                print(str(e))
                data_object_to_server["Faulty"] = True
                data_object_to_server["Frame"] = data_object_from_server["Frame"]
            # endregion

            # region Connect
            while True:
                try:
                    print("Trying to connect to the server...")
                    client_socket = socket.socket(
                        socket.AF_INET, socket.SOCK_STREAM)
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

            client_socket.recv(1024).decode()

            if not data_object_to_server["Faulty"]:
                with open(export_full_name, "rb") as tcp_upload:
                    progress_bar = tqdm(range(
                        data_object_to_server["Output Size"]), f'Uploading {export_name}', unit="B", unit_scale=True, unit_divisor=1024)

                    stream_bytes = tcp_upload.read(1024)
                    while stream_bytes:
                        stream_bytes = client_socket.send(stream_bytes)
                        progress_bar.update(len(str(stream_bytes)))
                        stream_bytes = tcp_upload.read(1024)

                progress_bar.clear()

            client_socket.close()
            # endregion
        except Exception as e:
            print(
                f'An ERROR occoured, waiting {settings_object["Error Hold"]} seconds')
            print(str(e))
            time.sleep(settings_object["Error Hold"])


if __name__ == "__main__":
    load_settings()
    client()
