import threading
import time
import json
import socket
from os import path as p
import os
import shutil
import essentials
import subprocess
#import sys

# subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
# subprocess.call([sys.executable, "-m", "pip", "install", "--upgrade", "pip"])
# subprocess.call([sys.executable, "-m", "pip", "install", "ffmpeg-python"])
# subprocess.call([sys.executable, "-m", "pip", "install", "pillow"])


#---Master related---#
SCRIPT_DIRECTORY: str = p.dirname(p.abspath(__file__)) + "/"
LOGS_DIRECTORY: str = p.join(SCRIPT_DIRECTORY, "logs/")
PROJECT_DIRECTORY: str = SCRIPT_DIRECTORY

PROJECT_EXTENSION: str = "rrfp"

#master_ip = socket.gethostbyname(socket.gethostname())
#settings_file: str = f"master_{master_ip}_settings.json"
settings_file_name = f"master_settings.json"
settings_file = p.join(SCRIPT_DIRECTORY, settings_file_name)

if not p.isdir(LOGS_DIRECTORY):
    os.mkdir(LOGS_DIRECTORY)
log_file_name = time.strftime("mSession_%Y%m%d%H%M%S.log")
log_file = p.join(LOGS_DIRECTORY, log_file_name)

settings_object: dict = {}
#settings_file = f"client_{client_ip}_settings.json"

lock = threading.Lock()
aquire_lock = threading.Lock()
save_lock = threading.Lock()

#---Project Related---#
project_object: dict = {}
frames_left: list = []


def setup():
    new_save_object = {
        "Master IP": "127.0.0.1",
        "Master Port": 9090,
        "Blender Executable": "D:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe",
        "FFMPEG Directory": "D:/Program Files/ffmpeg/bin",
        # "Working Directory": SCRIPT_DIRECTORY,
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

    user_input = input("Blender executable: ")
    while not p.isfile(user_input):
        print("Please select a valid executable")
        user_input = input("Blender executable: ")
    new_save_object["Blender Executable"] = user_input

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
    lock.acquire()
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

    project_object = save_object

    with open(p.join(PROJECT_DIRECTORY + f'{project_object["Project ID"]}.{PROJECT_EXTENSION}'), "w+") as f:
        json.dump(project_object, f, indent=4)
    lock.release()


def load_project(project_full_file: str):
    global PROJECT_DIRECTORY
    global project_object
    global frames_left

    with open(project_full_file, "r") as f:
        project_object = json.load(f)

    PROJECT_DIRECTORY = SCRIPT_DIRECTORY + project_object["Project ID"] + "/"

    # Calculate Missing Frames
    frames_left = []

    for frame in range(project_object["Frames Total"]):
        if not frame + 1 in project_object["Frames Complete"]:
            frames_left.append(frame + 1)


def generate_video():
    if project_object["Generate Video"]:
        import ffmpeg

        os.environ['PATH'] += ';' + settings_object["FFMPEG Directory"]

        input_images = p.join(PROJECT_DIRECTORY + "frame_%04d.png")
        video_render_stream = ffmpeg.input(
            input_images, start_number=project_object["First Frame"])
        video_render_stream = ffmpeg.filter(
            video_render_stream, 'fps', fps=project_object["Video FPS"], round='up')

        if project_object["Resize Video"]:
            video_render_stream = ffmpeg.filter(
                video_render_stream, 'scale', w=project_object["New Video Width"], h=project_object["New Video Height"])

        if project_object["VRC"] == "CBR":
            video_render_stream = ffmpeg.output(video_render_stream, p.join(
                PROJECT_DIRECTORY + f'{project_object["Project ID"]}.mp4'), video_bitrate=project_object["VRC Value"])
        elif project_object["VRC"] == "CRF":
            video_render_stream = ffmpeg.output(video_render_stream, p.join(
                PROJECT_DIRECTORY + f'{project_object["Project ID"]}.mp4'), crf=project_object["VRC Value"])

        ffmpeg.run(video_render_stream)


def master():
    global settings_object
    global PROJECT_DIRECTORY
    global project_object
    global frames_left

    essentials.print_help_message()

    while True:
        command_input = input("Command: ").lower()

        if command_input == "h" or command_input == "help":
            essentials.print_help_message()

        elif command_input == "l" or command_input == "load":
            project_input = input("Copy and paste the path to your project: ")

            while not p.isfile(project_input) and not project_input.endswith(f'.{PROJECT_EXTENSION}'):
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
                    input("Render a test frame? [y/N]: "), False)
            test_render = user_input

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

            PROJECT_DIRECTORY = SCRIPT_DIRECTORY + \
                new_project_object["Project ID"] + "/"
            os.mkdir(PROJECT_DIRECTORY)

            tmp = p.join(PROJECT_DIRECTORY,
                         f'{new_project_object["Project ID"]}.blend')
            shutil.copy(new_project_object[".Blend Full"], tmp)
            new_project_object[".Blend Full"] = tmp

            command: list = []
            # append Blender path
            command.append(settings_object["Blender Executable"])
            # append .blend file
            command.append('-b')
            command.append(new_project_object[".Blend Full"])
            # append BPY.py script
            command.append('-P')
            command.append(p.join(SCRIPT_DIRECTORY, "BPY.py"))
            # append BPY.py arguments
            command.append('--')
            command.append(PROJECT_DIRECTORY)
            if test_render:
                command.append("1")
            else:
                command.append("0")

            command.append("> nul")

            # start blender
            subprocess.run(command)

            with open(p.join(PROJECT_DIRECTORY, "vars.json")) as f:
                vars_string = f.read()
                vars_object = json.loads(vars_string)
                new_project_object["Render Engine"] = vars_object["RE"]
                new_project_object["Render Time"] = vars_object["RT"]
                new_project_object["File Format"] = vars_object["FF"]
                new_project_object["First Frame"] = vars_object["FS"]
                new_project_object["Last Frame"] = vars_object["FE"]

            frame_count = new_project_object["Last Frame"] - \
                (new_project_object["First Frame"] - 1)
            new_project_object["Frames Total"] = frame_count

            current_frame = new_project_object["First Frame"]

            while current_frame <= new_project_object["Last Frame"]:
                frames_left.append(current_frame)
                current_frame += 1

            new_project_object["Frames Complete"] = []

            save_project(new_project_object)

            print("Everything is setup! The rendering process will begin now.")

            server()


def aquire_frame(reqs: dict):
    aquire_lock.acquire()
    global frames_left

    if len(frames_left) <= 0:
        aquire_lock.release()
        return "NAN", -1, -1

    # if project_object["Render Time"] > reqs["RAM Limit"]:
    #    aquire_lock.release()
    #    return "NAN", -1, -1

    if project_object["Render Time"] > reqs["Time Limit"]:
        aquire_lock.release()
        return "NAN", -1, -1

    # To be replaced
    if not reqs["Allow EEVEE"] and project_object["Render Engine"] == "BLENDER_EEVEE":
        aquire_lock.release()
        return "NAN", -1, -1

    if not reqs["Allow Cycles"] and project_object["Render Engine"] == "CYCLES":
        aquire_lock.release()
        return "NAN", -1, -1

    if not reqs["Allow Workbench"] and project_object["Render Engine"] == "BLENDER_WORKBENCH":
        aquire_lock.release()
        return "NAN", -1, -1

    frames_left_tmp = frames_left.copy()
    tmp = frames_left[0]
    round = 0

    while round <= project_object["Chunks"]:

        if (tmp + round) in frames_left_tmp:
            frames_left.remove(frames_left[0])

            if len(frames_left) <= 0:

                tmp1 = (tmp + round) - frames_left_tmp[0]
                aquire_lock.release()
                return "NewR", frames_left_tmp[0], tmp1

        elif not (tmp + round) in frames_left_tmp:
            tmp1 = (tmp + round) - frames_left_tmp[0]
            aquire_lock.release()
            return "NewR", frames_left_tmp[0], tmp1

        if round == project_object["Chunks"]:
            tmp1 = (tmp + round) - frames_left_tmp[0]
            aquire_lock.release()
            return "NewR", frames_left_tmp[0], tmp1

        round += 1
    aquire_lock.release()


def save_frame(frame: int):
    lock.acquire()
    global project_object

    project_object["Frames Complete"].append(frame)
    lock.release()
    # save_project(project_object)


def validate_image(en: str, efn: str):
    from PIL import Image
    global frames_left

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
        os._exit(0)
    return faulty


def validate_images(images: list, zn: str, ff: str = "png"):
    from zipfile import ZipFile

    with ZipFile(p.join(PROJECT_DIRECTORY + zn), 'r') as zip_object:
        zip_object.extractall(PROJECT_DIRECTORY)

    # Image is the image number
    for image in images:
        # generate expected file name
        export_name = "frame_"
        export_name += "0" * (4 - len(str(image)))
        export_name += str(image)
        export_name += "." + ff
        #export_full_name = p.join(settings_object["Working Directory"], export_name)
        export_full_name = p.join(PROJECT_DIRECTORY, export_name)

        faulty = validate_image(export_name, export_full_name)

        if faulty:
            frames_left.append(image)
        else:
            save_frame(image)


def client_handler(client_connected: socket, client_address):
    try:
        global project_object
        global frames_left

        data_from_client = client_connected.recv(1024).decode()
        data_object_from_client = json.loads(data_from_client)

        if data_object_from_client["Message"] == "New":

            (msg, start, chunk) = aquire_frame(data_object_from_client)

            if msg == "NAN":
                data_object_to_client = {"Message": msg}
                data_to_client = json.dumps(data_object_to_client)
                client_connected.send(data_to_client.encode())
                return

            data_object_to_client = {
                "Message": msg,
                "Project ID": project_object["Project ID"],
                "Frame": start,
                "Chunks": chunk,
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
                    #uploadbar = essentials.progressbar(range(data_object_to_client["File Size"]))

                    stream_bytes = tcp_upload.read(1024)
                    while stream_bytes:
                        client_connected.send(stream_bytes)

                        # uploadbar.update(len(stream_bytes))

                        stream_bytes = tcp_upload.read(1024)

                client_connected.close()
            print("upload done")

        elif data_object_from_client["Message"] == "Output":
            valid_output = False
            valid_frames = []
            for f in data_object_from_client["Frames"]:
                # If "Faulty", then add back to list
                if data_object_from_client["Faulty"][str(f)]:
                    frames_left.append(f)
                else:
                    # Else download the output
                    valid_output = True
                    valid_frames.append(f)

            if valid_output:
                # Send response -> dropped -> synced
                client_connected.send("D".encode())

                os.chdir(PROJECT_DIRECTORY)

                file_name = str(data_object_from_client["Frames"][0]) + "_" + str(
                    data_object_from_client["Frames"][-1]) + ".zip"

                with open(str(p.join(PROJECT_DIRECTORY, file_name)), "wb") as tcp_download:
                    #downloadbar = essentials.progressbar(range(data_object_from_client["Output Size"]))

                    stream_bytes = client_connected.recv(1024)
                    while stream_bytes:

                        # downloadbar.update(len(stream_bytes))

                        tcp_download.write(stream_bytes)

                        stream_bytes = client_connected.recv(1024)
                client_connected.close()

                validate_images(valid_frames, file_name,
                                project_object["File Format"])

        elif data_object_from_client["Message"] == "Ping":
            client_connected.send("Pong".encode())
    except Exception as e:
        print("an ERROR occoured, continuing anyway")
        print(e)


def server():
    global settings_object
    global project_object
    global frames_left

    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(
        (settings_object["Master IP"], settings_object["Master Port"]))
    server_socket.listen()

    print("Server started. Waiting for clients...")
    #render_progressbar = essentials.progressbar(project_object["Frames Total"])

    while len(project_object["Frames Complete"]) < project_object["Frames Total"]:
        try:
            (client_connected, client_address) = server_socket.accept()
            print(f"New Connection: {client_address[0]}@{client_address[1]}")

            #threading.Thread(target=client_handler, args=(client_connected, client_address)).start()
            client_handler(client_connected, client_address)
        except Exception as e:
            print("an ERROR occoured, continuing anyway")
            print(e)

    server_socket.close()

    generate_video()

    os._exit(0)


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
