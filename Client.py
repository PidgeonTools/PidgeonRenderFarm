import socket
import time
import json
from zipfile import ZipFile
from os import path as p
import essentials as e
import subprocess
#import sys

# subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
# subprocess.call([sys.executable, "-m", "pip", "install", "--upgrade", "pip"])
# subprocess.call([sys.executable, "-m", "pip", "install", "pillow"])

#import os
#import shutil

#---Client related---#
#client_ip = socket.gethostbyname(socket.gethostname())
#settings_file = f"client_{client_ip}_settings.json"
settings_file = f"client_settings.json"
log_file = time.strftime("cSession_%Y%m%d%H%M%S.log")

settings_object: dict = {}

SCRIPT_DIRECTORY: str = p.dirname(p.abspath(__file__)) + "/"


def setup():
    valid_settings: dict = {
        "Render Device": ["CPU", "CUDA", "OPTIX", "HIP", "METAL", "OPENCL", "CUDA+CPU", "OPTIX+CPU", "HIP+CPU", "METAL+CPU", "OPENCL+CPU"],
    }

    # new save_object with default values / example values
    new_save_object = {
        "Master IP": "192.168.178.117",
        "Master Port": 9090,
        "Blender Executable": "D:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe",
        # "Working Directory": SCRIPT_DIRECTORY,
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

    user_input = input("What is the IP address of the master?: ")
    while not e.validate_ip(user_input):
        print("Please select a valid ip address (xxx.xxx.xxx.xxx")
        user_input = input("What is the IP address of the master?: ")
    new_save_object["Master IP"] = user_input

    user_input = input("What is the port of the master?: ")
    while not e.is_port(user_input):
        print("Please input a whole number between 1 and 65536")
        user_input = input("What is the port of the master?: ")
    new_save_object["Master Port"] = int(user_input)

    user_input = input("Where is your Blender executable stored?: ")
    while not p.isfile(user_input):
        print("Please select a valid executable")
        user_input = input("Where is your Blender executable stored?: ")
    new_save_object["Blender Executable"] = user_input

    # user_input = input("Which directory to use as working directory?: ")
    # while not p.isdir(user_input):
    #     print("Please select a valid directory")
    #     user_input = input("Which directory to use as working directory?: ")
    # new_save_object["Working Directory"] = user_input

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

    user_input = None
    while user_input == None:
        user_input = e.parse_bool(
            input("Allow EEVEE rendering on this client? [y/N]: "), True)
    new_save_object["Allow EEVEE"] = user_input

    user_input = None
    while user_input == None:
        user_input = e.parse_bool(
            input("Allow Cycles rendering on this client? [y/N]: "), True)
    new_save_object["Allow Cycles"] = user_input

    user_input = None
    while user_input == None:
        user_input = e.parse_bool(
            input("Allow Workbench rendering on this client? [y/N]: "), True)
    new_save_object["Allow Workbench"] = user_input

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

    user_input = None
    while user_input == None:
        user_input = e.parse_bool(
            input("Keep the rendered and uploaded frames? [y/N]: "), True)
    new_save_object["Keep Output"] = user_input

    user_input = None
    while user_input == None:
        user_input = e.parse_bool(input(
            "Keep the project files received from the master? (See README.md) [y/N]: "), True)
    new_save_object["Keep Input"] = user_input

    save_settings(new_save_object)


def save_settings(save_object: dict):
    global settings_object
    settings_object = save_object

    with open(settings_file, "w+") as f:
        json.dump(settings_object, f, indent=4)


def load_settings():
    try:
        global settings_object

        with open(settings_file, "r") as loaded_settings_file:
            settings_object = json.load(loaded_settings_file)
    except Exception as e:
        setup()


def connect():
    while True:
        try:
            print("Trying to connect to the server...")
            global settings_object

            cs = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            cs.connect((settings_object["Master IP"],
                        settings_object["Master Port"]))

            break
        except Exception as e:
            print(
                f'Could not connect to the server, waiting {settings_object["Connection Error Hold"]} seconds')
            # print(str(e))
            time.sleep(settings_object["Connection Error Hold"])
    return cs


def validate_image(en: str, efn: str):
    from PIL import Image

    faulty = False

    try:
        # verify output using PIL
        with Image.open(efn) as test_image:
            test_image.verify()

            faulty = False

        #dots["Output Size"] = p.getsize(efn)
    except Exception as e:
        print("faulty image detected: " + en)
        # print(str(e))
        faulty = True
    return faulty


def validate_images(images: list, ff: str = "png"):
    dots = {"Message": "Output"}
    dots["Faulty"] = {}

    zip_name = str(images[0]) + "-" + str(images[-1]) + ".zip"
    with ZipFile(zip_name, 'w') as zip_object:
        # Image is the image number
        for image in images:
            # generate expected file name
            export_name = "frame_"
            export_name += "0" * (4 - len(str(image)))
            export_name += str(image)
            export_name += "." + ff
            #export_full_name = p.join(settings_object["Working Directory"], export_name)
            export_full_name = p.join(SCRIPT_DIRECTORY, export_name)

            dots["Faulty"][str(image)] = validate_image(
                export_name, export_full_name)

            if dots["Faulty"][str(image)] == False:
                zip_object.write(export_full_name)
    return dots, zip_name


def client():
    global settings_object

    while True:
        client_socket = connect()

        try:
            # region Server Request
            data_object_to_server = {
                "Message": "New",
                "RAM Limit": settings_object["RAM Limit"],
                "Size Limit": settings_object["Size Limit"],
                # Outdated
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
            # Message, Project ID, Frame, Render Engine, File Size, (average render time, average ram use)

            print(f'Project ID: {data_object_from_server["Project ID"]}')
            print(f'Frame: {data_object_from_server["Frame"]}')
            print(f'Render Engine: {data_object_from_server["Render Engine"]}')
            # endregion

            # region Aquire Project
            #print(p.abspath(f'{data_object_from_server["Project ID"]}.blend'))

            # Make sure we have the correct file by checking if we have a file and if it has the same size as on the server side
            if p.isfile(f'{data_object_from_server["Project ID"]}.blend') and p.getsize(f'{data_object_from_server["Project ID"]}.blend') == data_object_from_server["File Size"]:
                # If we have it, tell the server there is no need
                data_object_to_server = {"Message": "File", "Needed": False}
                data_to_server = json.dumps(data_object_to_server)
                client_socket.send(data_to_server.encode())
            else:
                # Else tell the server we still need it
                data_object_to_server = {"Message": "File", "Needed": True}
                data_to_server = json.dumps(data_object_to_server)
                client_socket.send(data_to_server.encode())

                # Create a new file
                with open(f'{data_object_from_server["Project ID"]}.blend', "wb") as tcp_download:
                    downloadbar = e.progressbar(
                        range(data_object_from_server["File Size"]))

                    # Download file over TCP
                    # Receive data and write to file until there is no more data
                    stream_bytes = client_socket.recv(1024)
                    while stream_bytes:
                        tcp_download.write(stream_bytes)

                        downloadbar.update(len(stream_bytes))

                        stream_bytes = client_socket.recv(1024)

            client_socket.close()
            # endregion

            # region Render

            # create list
            command: list = []
            # append Blender path
            command.append(settings_object["Blender Executable"])
            # append .blend file
            command.append('-b')
            command.append(f'{data_object_from_server["Project ID"]}.blend')
            # append output directory and name
            command.append('-o')
            # command.append(p.join(settings_object["Working Directory"], "frame_####"))
            command.append(p.join(SCRIPT_DIRECTORY, "frame_####"))
            # append output file format
            command.append('-F')
            command.append(data_object_from_server["File Format"])
            # append start frame
            command.append('-s')
            command.append(data_object_from_server["Frame"])
            # append end frame
            command.append('-e')
            command.append(
                data_object_from_server["Frame"] + (data_object_from_server["Chunks"] - 1))
            # if cycles, then set the render device
            if data_object_from_server["Render Engine"] == "Cycles":
                command.append('--cycles-device')
                command.append(settings_object["Render Device"])

            # start blender
            subprocess.run(command)

            # endregion

            # region Verify and Compress
            image_list = []

            for im in range(data_object_from_server["Frame"], (data_object_from_server["Frame"] + data_object_from_server["Chunks"] - 1)):
                image_list.append(im)

            data_object_to_server, zip_name = validate_images(
                image_list, data_object_from_server["File Format"])
            data_object_to_server["Frames"] = image_list
            data_object_to_server["File"] = zip_name

            zip_full_name = p.join(SCRIPT_DIRECTORY, zip_name)
            data_object_to_server["Size"] = p.getsize(zip_full_name)
            # endregion

            client_socket = connect()

            data_to_server = json.dumps(data_object_to_server)
            client_socket.send(data_to_server.encode())

            # drop stream from master -> synced
            client_socket.recv(1024).decode()

            # if output verified, send it to the server
            tmp = True
            for f in data_object_to_server["Frames"]:
                if not data_object_to_server["Faulty"][str(f)]:
                    tmp = False
                    break

            if not tmp:
                with open(zip_full_name, "rb") as tcp_upload:
                    uploadbar = e.progressbar(
                        range(data_object_to_server["Output Size"]))

                    stream_bytes = tcp_upload.read(1024)
                    while stream_bytes:
                        stream_bytes = client_socket.send(stream_bytes)

                        uploadbar.update(len(stream_bytes))

                        stream_bytes = tcp_upload.read(1024)

            client_socket.close()

        except Exception as e:
            print(
                f'An ERROR occoured, waiting {settings_object["Error Hold"]} seconds')
            # print(str(e))
            time.sleep(settings_object["Error Hold"])


if __name__ == "__main__":
    load_settings()
    client()
