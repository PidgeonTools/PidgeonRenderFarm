from genericpath import isfile
import json
#import shutil
import subprocess
import sys
import os
import time
#from zipfile import ZipFile
import socket
#import ftplib
#import urllib.request
import re
from PIL import Image
import tqdm

#---Client related---#
client_ip = socket.gethostbyname(socket.gethostname())
print(client_ip)
settings_file = f"client_{client_ip}_settings.json"
settings_object: dict = {}

#script_dir = os.path.dirname(os.path.abspath(__file__))

connected = False


async def save_settings(save_object: dict = {}):
    if save_object == {}:
        save_object = {
            "Master IP": "192.168.178.117",
            "Master Port": 9090,
            "Fallback Master IP": "192.168.178.90",
            "Fallback Master Port": 9090,
            "Blender Executable": "D:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe",
            "Working Directory": ".",
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
            "Error Hold": 30,
            "Connection Error Hold": 20,
            "Transfer Error Hold": 5,
        }

    global settings_object
    settings_object = save_object

    json_string = json.dumps(save_object)

    with open(settings_file, "w+") as file_to_write:
        file_to_write.write(json_string)


async def load_settings(again: bool = False):
    if os.path.isfile(settings_file):
        global settings_object

        with open(settings_file, "r") as loaded_settings_file:
            loaded_string = loaded_settings_file.read()
            settings_object = json.loads(loaded_string)

        print(settings_object)

    else:
        save_settings()


def client():  # first_boot:str = "Yes"):
    # if first_boot == "Yes":
    #     subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
    #     subprocess.call([sys.executable, "-m", "pip", "install", "--upgrade", "pip"])

    # os.mkdir("job")

    global settings_object
    global connected

    load_settings()

    while True:
        while not connected:
            try:
                print("Trying to connect to the server...")
                client_socket = socket.socket(
                    socket.AF_INET, socket.SOCK_STREAM)
                client_socket.connect(
                    (settings_object["Master IP"], settings_object["Master Port"]))
                connected = True
            except Exception as e:
                print(
                    f'Could not connect to the server, waiting {settings_object["Connection Error Hold"]} seconds')
                # print(str(e))
                time.sleep(settings_object["Connection Error Hold"])

        try:
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

            data_from_server = client_socket.recv(1024).decode()
            data_object_from_server = json.loads(data_from_server)
            # Data structure:
            # Message, project id, frame, engine, file size, (average render time, average ram use)

            print(
                f'Project ID: {data_object_from_server["Project ID"]}; Frame: {data_object_from_server["Frame"]}; Render Engine: {data_object_from_server["Render Engine"]}')

            if os.path.isfile(f'{data_object_from_server["Project ID"]}.blend'):
                data_object_to_server = {"Message": "File", "Needed": False}
                data_to_server = json.dumps(data_object_to_server)
                client_socket.send(data_to_server.encode())
            else:
                data_object_to_server = {"Message": "File", "Needed": True}
                data_to_server = json.dumps(data_object_to_server)
                client_socket.send(data_to_server.encode())

                with open(f'{data_object_from_server["Project ID"]}.blend', "wb") as tcp_download:
                    progress_bar = tqdm.tqdm(range(
                        data_object_from_server["File Size"]), f'Receiving {data_object_from_server["Project ID"]}', unit="B", unit_scale=True, unit_divisor=1024)

                    streamBytes = client_socket.recv(1024)
                    while streamBytes:
                        tcp_download.write(streamBytes)
                        progress_bar.update(len(streamBytes))

                        streamBytes = client_socket.recv(1024)

            client_socket.close()
            connected = False

            if data_object_from_server["Render Engine"] == "Cycles":
                subprocess.run(f'"{settings_object["Blender Executable"]}" -b "{settings_object["Working Directory"]}{data_object_from_server["Project ID"]}.blend" -o "{settings_object["Working Directory"]}frame_####" --cycles-device {settings_object["Render Device"]} -f {data_object_from_server["Frame"]}', shell=True)
            else:
                subprocess.run(
                    f'"{settings_object["Blender Executable"]}" -b "{settings_object["Working Directory"]}{data_object_from_server["Project ID"]}.blend" -o "{settings_object["Working Directory"]}frame_####" -f {data_object_from_server["Frame"]}', shell=True)

            export_name = "frame_"
            export_name += "0" * (4 - len(data_object_from_server["Frame"]))
            export_name += ".png"

            try:
                test_image = Image.open(
                    settings_object["Working Directory"] + export_name)
                test_image.verify()
                test_image.close()
            except:
                print("faulty image detected")
                bad = True
                upload = True

            # open

            if data_from_server.startswith("here"):
                split = data_from_server.split('|')

                print(
                    f"Job-Request! {master_ip}; Project: {split[1]}; Frame: {split[2]}")

                if not os.path.isfile(working_dir + split[1]):
                    print("downloading project")
                    urllib.request.urlretrieve(
                        f'ftp://{split[7]}:{split[8]}@{split[4]}:{split[5]}/{split[1]}', working_dir + split[1])

                if os.path.isfile(split[1]):
                    done = False
                    upload = False
                    bad = False

                    print("Starting blender render")
                    if split[3] == "eevee":
                        subprocess.run(
                            f'"{blender_path}" -b "{working_dir + split[1]}" -o "{working_dir}frame_####" -f {split[2]}', shell=True)

                    elif split[3] == "cycles":
                        if gpu != "NONE":
                            if hybrid:
                                subprocess.run(
                                    f'"{blender_path}" -b "{working_dir + split[1]}" -o "{working_dir}frame_####" --cycles-device {gpu} -f {split[2]}', shell=True)
                            else:
                                subprocess.run(
                                    f'"{blender_path}" -b "{working_dir + split[1]}" -o "{working_dir}frame_####" --cyles-device {gpu}+CPU -t {maxThreads} -f {split[2]}', shell=True)
                        else:
                            subprocess.run(
                                f'"{blender_path}" -b "{working_dir + split[1]}" -o "{working_dir}frame_####" --cyles-device CPU -t {maxThreads} -f {split[2]}', shell=True)

                    print("verifying...")
                    export_name = ""
                    digits = len(split[2])

                    if digits == 1:
                        export_name = "frame_" + "000" + split[2] + ".png"
                    elif digits == 2:
                        export_name = "frame_" + "00" + split[2] + ".png"
                    elif digits == 3:
                        export_name = "frame_" + "0" + split[2] + ".png"
                    elif digits == 4:
                        export_name = "frame_" + split[2] + ".png"

                    try:
                        im = Image.open(working_dir + export_name)
                        im.verify()
                        im.close()
                    except:
                        print("faulty image detected")
                        bad = True
                        upload = True

                    while not upload:
                        try:
                            print("trying to upload...")
                            session = ftplib.FTP()
                            session.connect(split[4], int(split[5]))
                            session.login(split[7], split[8])
                            file = open(working_dir + export_name, 'rb')
                            session.storbinary(f"STOR {export_name}", file)
                            file.close()
                            session.quit()
                            print("file uploaded")
                            upload = True
                        except:
                            print("uploading has failed, waiting 30 seconds")
                            time.sleep(30)

                    while not connected:
                        try:
                            print("trying to connect to master...")
                            client_socket = socket.socket(
                                socket.AF_INET, socket.SOCK_STREAM)
                            client_socket.connect((master_ip, masterPORT))
                            connected = True
                        except Exception as e:
                            print(
                                "could not connect to the server, waiting 60 seconds")
                            print(str(e))
                            time.sleep(60)

                    while not done:
                        try:
                            print("trying to report...")

                            if bad:
                                data = "error"
                                print(data)
                                client_socket.send(data.encode())
                                done = True

                            else:
                                data = f"done|{split[2]}|{export_name}"
                                print(data)
                                client_socket.send(data.encode())
                                done = True

                        except Exception as e:
                            print("ERROR while transfering, waiting 60 seconds")
                            print(e)
                            time.sleep(60)

                        client_socket.close()
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

    client()

    # print(sys.argv)
    # try:
    #     arg = sys.argv[1]
    #     client(arg)
    # except:
    #     client()
