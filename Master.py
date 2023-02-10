import time
import json
import ffmpeg
from PIL import Image
import socket
from os import path as p
import os
import essentials
import subprocess
import sys

# subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
# subprocess.call([sys.executable, "-m", "pip", "install", "--upgrade", "pip"])
# subprocess.call([sys.executable, "-m", "pip", "install", "ffmpeg-python"])
# subprocess.call([sys.executable, "-m", "pip", "install", "pillow"])

#import shutil
#from zipfile import ZipFile

#---Master related---#
#master_ip = socket.gethostbyname(socket.gethostname())
#settings_file: str = f"master_{master_ip}_settings.json"
settings_file: str = f"master_settings.json"
log_file = time.strftime("mSession_%Y%m%d%H%M%S.log")

settings_object: dict = {}
project_extension: str = "rrfp"

SCRIPT_DIRECTORY: str = p.dirname(p.abspath(__file__)) + "/"

#---Project Related---#
project_object: dict = {}
frames_left: list = []


def setup():
    new_save_object = {
        "Master IP": "127.0.0.1",
        "Master Port": 9090,
        "Blender Executable": "D:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe",
        "FFMPEG Directory": "D:/Program Files/ffmpeg/bin",
        "Working Directory": SCRIPT_DIRECTORY,
        "Worker Limit": 0,
        "Keep Output": True,
        "Project ID Length": 8,
    }

    new_save_object["Master IP"] = socket.gethostbyname(socket.gethostname())

    user_input = input("Which Port to use?: ")
    while not essentials.is_port(user_input):
        print("Please input a whole number between 1 and 65536")
        user_input = input("Which Port to use?: ")
    new_save_object["Master Port"] = int(user_input)

    user_input = input("What is your FFMPEG directory?: ")
    while not p.isdir(user_input):
        print("Please select a valid directory (see README.md)")
        user_input = input("What is your FFMPEG directory?: ")
    new_save_object["FFMPEG Directory"] = user_input

    user_input = input("Which directory to use as working directory?: ")
    while not p.isdir(user_input):
        print("Please select a valid directory")
        user_input = input("Which directory to use as working directory?: ")
    new_save_object["Working Directory"] = user_input

    user_input = input("Maximum amount of clients?: ")
    while not user_input.isdigit():
        print("Please input a whole number")
        user_input = input("Maximum amount of clients?: ")
    new_save_object["Worker Limit"] = abs(int(user_input))

    user_input = None
    while user_input == None:
        user_input = essentials.parse_bool(
            input("Keep the files received from the clients? [y/N]: "), True)
    new_save_object["Keep Output"] = user_input

    user_input = input("Project ID length?: ")
    while True:
        if user_input.isdigit():
            if int(user_input) >= 1:
                break

        print("Please input a whole number")
        user_input = input("Project ID length?: ")
    new_save_object["Project ID Length"] = abs(int(user_input))

    save_settings(new_save_object)


def save_settings(save_object: dict):
    global settings_object
    settings_object = save_object

    with open(settings_file, "w+") as f:
        json.dump(settings_object, f, indent=4)


def load_settings(again: bool = False):
    try:
        global settings_object

        with open(settings_file, "r") as loaded_settings_file:
            settings_object = json.load(loaded_settings_file)
    except Exception as e:
        setup()


def save_project(save_object: dict = {}):
    save_object_base = {
        "Project ID": "NAN",
        ".Blend Full": "NAN",
        "Render Engine": "NAN",
        "File Format": "PNG",
        "Generate Video": True,
        "Video FPS": 30,
        "VRC": "CBR",  # CBR, CRF
        "VRC Value": 5,
        "Resize Video": True,
        "New Video Width": 3840,
        "New Video Height": 2160,
        "Chunks": 1,
        "Render Time": 0,
        "First Frame": 1,
        "Last Frame": 250,
        "Frames Total": 250,
        "Frames Complete": [],
    }

    global project_object

    project_object = save_object_base | project_object | save_object

    with open(f'{project_object["Project ID"]}.{project_extension}', "w+") as f:
        json.dump(project_object, f, indent=4)


def load_project(project_full_file: str):
    global project_object
    global frames_left

    with open(project_full_file, "r") as f:
        project_object = json.load(f)

    # Calculate Missing Frames
    frames_left = []

    for frame in range(project_object["Frames Total"]):
        if not frame + 1 in project_object["Frames Complete"]:
            frames_left.append(frame + 1)

    print(frames_left)


def master():
    global settings_object
    global project_object
    global frames_left

    essentials.print_help_message()

    while True:
        command_input = input("Command: ").lower()

        if command_input == "h" or command_input == "help":
            essentials.print_help_message()

        elif command_input == "l" or command_input == "load":
            project_input = input("Copy and paste the path to your project: ")

            while not p.isfile(project_input) and not project_input.endswith(f'.{project_extension}'):
                print("Please select an exsisting and compatible file")
                project_input = input(
                    "Copy and paste the path to your project: ")

            load_project(project_input)
            server()

        elif command_input == "n" or command_input == "new":
            new_project_object = {}
            new_project_object["Project ID"] = essentials.generate_project_id(
                settings_object["Project ID Length"])

            user_input = input("Copy and paste the path to your .blend: ")
            while not p.isfile(user_input) and not user_input.endswith(".blend"):
                print("Please select an exsisting and compatible file")
                user_input = input("Copy and paste the path to your .blend: ")
            new_project_object[".Blend Full"] = user_input

            user_input = None
            while user_input == None:
                user_input = essentials.parse_bool(
                    input("Generate a video file? [y/N]: "), False)
            new_project_object["Generate Video"] = user_input

            if new_project_object["Generate Video"]:
                user_input = input("Video FPS: ")
                while not user_input.isdigit():
                    print("Please input a whole number")
                    user_input = input("Video FPS: ")
                new_project_object["Video FPS"] = abs(int(user_input))

                user_input = input("Video Rate Control: ")
                while not user_input.upper() in ["CBR", "CRF"]:
                    print("Please input an valid option (see README.md)")
                    user_input = input("Video Rate Control: ")
                new_project_object["VRC"] = user_input.upper()

                user_input = input("Video Rate Control Value: ")
                while not user_input.isdigit():
                    print("Please input a whole number")
                    user_input = input("Video Rate Control Value: ")
                new_project_object["VRC Value"] = abs(int(user_input))

                user_input = None
                while user_input == None:
                    user_input = essentials.parse_bool(
                        input("Change the video resolution? [y/N]: "), False)
                new_project_object["Resize Video"] = user_input

                if new_project_object["Resize Video"]:
                    user_input = input("New video width: ")
                    while not user_input.isdigit():
                        print("Please input a whole number")
                        user_input = input("New video width: ")
                    new_project_object["New Video Width"] = abs(
                        int(user_input))

                    user_input = input("New video heigth: ")
                    while not user_input.isdigit():
                        print("Please input a whole number")
                        user_input = input("New video heigth: ")
                    new_project_object["New Video Height"] = abs(
                        int(user_input))

            user_input = input(
                "Chunks (0 for auto. 1 for 1 chunk = 1 frame; EXPERIMENTAL!): ")
            while not user_input.isdigit():
                print("Please input a whole number")
                user_input = input("New video heigth: ")
            new_project_object["Chunks"] = abs(int(user_input))

            print("The project setup has been completed! The script will now compute all the other required data on it's own.")

            print("Computing Required Data")
            combar = essentials.progressbar(10, 0)

            subprocess.call([settings_object["Blender Executable"], "-b", "-P",
                             "BPY.py", "--", f'"{settings_object["Working Directory"]}"', "1"])
            combar.update(1)

            with open(p.join(settings_object["Working Directory"], "vars.json")) as f:
                vars_string = f.read()
                vars_object = json.loads(vars_string)
                new_project_object["Render Engine"] = vars_object["RE"]
                new_project_object["Render Time"] = vars_object["RT"]
                new_project_object["File Format"] = vars_object["FF"]
                new_project_object["First Frame"] = vars_object["FS"]
                new_project_object["Last Frame"] = vars_object["FE"]
            combar.update(5)

            frame_count = new_project_object["Last Frame"] - \
                (new_project_object["First Frame"] - 1)
            new_project_object["Frames Total"] = frame_count
            combar.update(1)

            current_frame = new_project_object["First Frame"]

            while current_frame <= new_project_object["Last Frame"]:
                frames_left.append(current_frame)
                current_frame += 1

            combar.update(1)

            new_project_object["Frames Complete"] = []
            combar.update(1)

            save_project(new_project_object)
            combar.update(1)

            print("Everything is setup! The rendering process will begin now.")

            server()


def server():
    global settings_object
    global project_object
    global frames_left

    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(
        (settings_object["Master IP"], settings_object["Master Port"]))
    server_socket.listen()

    print("Server started. Waiting for clients...")
    #render_progressbar = tqdm(range(project_object["Frames Total"]), unit="Frame", unit_divisor=1)
    render_progressbar = essentials.progressbar(
        range(project_object["Frames Total"]))

    while len(project_object["Frames Complete"]) < project_object["Frames Total"]:
        try:
            (client_connected, client_address) = server_socket.accept()
            print(f"New Connection: {client_address[0]}@{client_address[1]}")

            data_from_client = client_connected.recv(1024).decode()
            data_object_from_client = json.loads(data_from_client)

            if data_object_from_client["Message"] == "New":
                # Do some processing...

                data_object_to_client = {
                    "Message": "NewR",
                    "Project ID": project_object["Project ID"],
                    "Frame": frames_left[0],
                    "Render Engine": project_object["Render Engine"],
                    "File Format": project_object["File Format"],
                    "File Size": p.getsize(project_object[".Blend Full"]),
                }
                data_to_client = json.dumps(data_object_to_client)
                client_connected.send(data_to_client.encode())

                # Blend File
                data_from_client = client_connected.recv(1024).decode()
                data_object_from_client = json.loads(data_from_client)

                if data_object_from_client["Needed"]:
                    with open(project_object[".Blend Full"], "rb") as tcp_upload:
                        uploadbar = essentials.progressbar(
                            range(data_object_to_client["File Size"]))

                        stream_bytes = tcp_upload.read(1024)
                        while stream_bytes:
                            client_connected.send(stream_bytes)

                            uploadbar.update(len(stream_bytes))

                            stream_bytes = tcp_upload.read(1024)

                        client_connected.shutdown()
                print("upload done")
                frames_left.remove(frames_left[0])

            elif data_object_from_client["Message"] == "Output":
                if data_object_from_client["Faulty"]:
                    frames_left.append(data_object_from_client["Frame"])

                else:
                    client_connected.send("Drop".encode())

                    with open(p.join(settings_object["Working Directory"] + data_object_from_client["Project Frame"]), "wb") as tcp_download:
                        downloadbar = essentials.progressbar(
                            range(data_object_from_client["Output Size"]))

                        stream_bytes = client_connected.recv(1024)
                        while stream_bytes:
                            tcp_download.write(stream_bytes)
                            downloadbar.update(len(stream_bytes))

                            stream_bytes = client_connected.recv(1024)

                    try:
                        with Image.open(p.join(settings_object["Working Directory"], data_object_from_client["Project Frame"])) as test_image:
                            test_image.verify()

                            project_object["Frames Complete"].append(
                                data_object_from_client["Frame"])
                            save_project()

                            render_progressbar.update(1)
                    except:
                        print("Faulty image detected")
                        frames_left.append(data_object_from_client["Frame"])

        except Exception as e:
            print("an ERROR occoured, continuing anyway")
            print(e)

    # server_socket.shutdown()
    server_socket.close()

    if project_object["Generate Video"]:
        os.environ['PATH'] += ';' + settings_object["FFMPEG Directory"]

        input_images = p.join(
            settings_object["Working Directory"] + "frame_%04d.png")
        video_render_stream = ffmpeg.input(
            input_images, start_number=project_object["First Frame"])
        video_render_stream = ffmpeg.filter(
            video_render_stream, 'fps', fps=project_object["Video FPS"], round='up')

        if project_object["Resize Video"]:
            video_render_stream = ffmpeg.filter(
                video_render_stream, 'scale', w=project_object["New Video Width"], h=project_object["New Video Height"])

        if project_object["VRC"] == "CBR":
            video_render_stream = ffmpeg.output(
                video_render_stream, f'{project_object["Project ID"]}.mp4', video_bitrate=project_object["VRC Value"])
        elif project_object["VRC"] == "CRF":
            video_render_stream = ffmpeg.output(
                video_render_stream, f'{project_object["Project ID"]}.mp4', crf=project_object["VRC Value"])

        ffmpeg.run(video_render_stream)

        sys.exit()


def get_frames(path):
    import struct

    with open(path, "rb") as blendfile:
        head = blendfile.read(7)

        if head[0:2] == b'\x1f\x8b':  # gzip magic
            import gzip
            blendfile.seek(0)
            blendfile = gzip.open(blendfile, "rb")
            head = blendfile.read(7)

        if head != b'BLENDER':
            print("not a blend file:", path)
            blendfile.close()
            return []

        is_64_bit = (blendfile.read(1) == b'-')

        # true for PPC, false for X86
        is_big_endian = (blendfile.read(1) == b'V')

        # Now read the bhead chunk!!!
        blendfile.read(3)  # skip the version

        scenes = []

        sizeof_bhead = 24 if is_64_bit else 20

        while blendfile.read(4) == b'REND':
            sizeof_bhead_left = sizeof_bhead - 4

            struct.unpack('>i' if is_big_endian else '<i',
                          blendfile.read(4))[0]
            sizeof_bhead_left -= 4

            # We don't care about the rest of the bhead struct
            blendfile.read(sizeof_bhead_left)

            # Now we want the scene name, start and end frame. this is 32bites long
            start_frame, end_frame = struct.unpack(
                '>2i' if is_big_endian else '<2i', blendfile.read(8))

            scenes.append(start_frame)
            scenes.append(end_frame)

    return scenes


if __name__ == "__main__":
    load_settings()
    master()

    # print(sys.argv)
    # try:
    #     arg = sys.argv[1]
    #     master(arg)
    # except:
    #     master("No")
    # load_settings(False)
