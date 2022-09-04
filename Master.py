#import shutil
import subprocess
import sys
import os
import socket
from PIL import Image
from click import command
import ffmpeg
import json
import string
import random

#---Project related---#
path = None
blend_name = None
project_name = None

project_settings = ["save", "engine", "mp4", "fps",
                    "rate control", "value", "upscale", "width", "height"]

start_end_frame = None
start_frame = None
end_frame = None
frame_count = None
frames_left = []
frames_received = []

#---Master related---#
master_ip = socket.gethostbyname(socket.gethostname())
settings_file = f"master_{master_ip}_settings.json"
settings_object: dict = {}

script_directory = os.path.dirname(os.path.abspath(__file__)) + "/"

#---Project Related---#
valid_project_settings = {
    "Render Engine": ["eevee, cycles, workbench"],
    "VRC": ["CBR", "CRF"],
}

project_object: dict = {}


async def setup():

    save_settings()
    return


async def save_settings(save_object: dict = {}):
    save_object_base = {
        "Master IP": "192.168.178.117",
        "Master Port": 9090,
        "FFMPEG Directory": "D:/Program Files/ffmpeg/bin",
        "Working Directory": script_directory,
        "Size Limit": 0,
        "Worker Limit": 0,
        "Keep Input": True,
        "Project ID Length": 8,
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


def save_project(save_object: dict = {}):
    save_object_base = {
        "Project ID": "NAN",
        ".Blend Full": "NAN",
        "Render Engine": "Cycles",  # EEVEE, Cycles, Workbench
        "Generate Video": True,
        "Video FPS": 30,
        "VRC": "CBR",  # CBR, CRF
        "VRC Value": 5,
        "Resize Video": True,
        "New Video Width": 3840,
        "New Video Height": 2160,
        "First Frame": 1,
        "Last Frame": 250,
        "Frames Total": 250,
        "Frames Complete": {},
    }

    save_object_out = save_object_base | save_object

    global settings_object
    settings_object = save_object_out

    json_string = json.dumps(save_object_out)

    with open(settings_file, "w+") as file_to_write:
        file_to_write.write(json_string)


async def load_project(project_full_file: str):
    global project_object

    with open(project_full_file, "r") as loaded_settings_file:
        loaded_string = loaded_settings_file.read()
        project_object = json.loads(loaded_string)

    # print(settings_object)

    # Calculate Missing Frames
    # Start the Server


def help_message():
    print("##################################################")
    print("N    -   New project")
    print("L    -   Load project")
    print("S    -   Re-Run setup")
    print("H    -   Show this")
    print("##################################################")


def generate_project_id(length: int = 8):
    # choose from all lowercase letter
    letters = string.ascii_letters + string.digits
    result_str = ''.join(random.choice(letters) for i in range(length))
    #print("Random string of length", length, "is:", result_str)

    return result_str


def input_to_bool(inp: str):
    if inp.capitalize() == "True" or inp.capitalize() == "Yes":
        return True
    elif inp.capitalize() == "False" or inp.capitalize() == "No":
        return False


def master():
    global settings_object
    global project_object

    help_message()

    while True:
        command_input = input("Command: ").lower()

        if command_input == "h" or command_input == "help":
            help_message()

        elif command_input == "l" or command_input == "load":
            project_input = input("Copy and paste the path to your project: ")

            while not os.path.isfile(project_input):
                print("Please select an exsisting and compatible file")
                project_input = input(
                    "Copy and paste the path to your project: ")

            load_project(project_input)

        elif command_input == "n" or command_input == "new":
            newproject_object = {}
            newproject_object["Project ID"] = generate_project_id(
                project_settings["Project ID Length"])

            user_input = input("Copy and paste the path to your .blend: ")

            while not os.path.isfile(user_input) and not user_input.endswith(".blend"):
                print("Please select an exsisting and compatible file")
                user_input = input("Copy and paste the path to your .blend: ")
            newproject_object[".Blend Full"] = user_input

            user_input = input("Which Render Engine does your project use?: ")
            while not valid_project_settings["Render Engine"].contains(user_input.lower()):
                print("Please select an valid option ('EEVEE', 'Cycles', 'Workbench')")
                user_input = input(
                    "Which Render Engine does your project use?: ")
            newproject_object["Render Engine"] = user_input

            user_input = input("Generate a video file?: ")
            while user_input.capitalize() != "True" or user_input.capitalize() != "Yes" or user_input.capitalize() != "False" or user_input.capitalize() != "No":
                print("Please select an valid option ('True', 'Yes', 'False', 'No')")
                user_input = input("Generate a video file?: ")
            newproject_object["Generate Video"] = input_to_bool(user_input)

            if newproject_object["Generate Video"]:
                user_input = input("Video FPS: ")
                while not user_input.isdigit():
                    print("Please input a whole number")
                    user_input = input("Video FPS: ")
                newproject_object["Video FPS"] = abs(int(user_input))

                user_input = input("Video Rate Control: ")
                while not valid_project_settings["VRC"].contains(user_input.upper()):
                    print("Please input an valid option ('CBR', 'CRF')")
                    user_input = input("Video Rate Control: ")
                newproject_object["VRC"] = user_input.upper()

                user_input = input("Video Rate Control Value: ")
                while not user_input.isdigit():
                    print("Please input a whole number")
                    user_input = input("Video Rate Control Value: ")
                newproject_object["VRC Value"] = abs(int(user_input))

                user_input = input("Change the video resolution?: ")
                while user_input.capitalize() != "True" or user_input.capitalize() != "Yes" or user_input.capitalize() != "False" or user_input.capitalize() != "No":
                    print(
                        "Please select an valid option ('True', 'Yes', 'False', 'No')")
                    user_input = input("Change the video resolution?: ")
                newproject_object["Resize Video"] = input_to_bool(user_input)

                if newproject_object["Resize Video"]:
                    user_input = input("New video width: ")
                    while not user_input.isdigit():
                        print("Please input a whole number")
                        user_input = input("New video width: ")
                    newproject_object["New Video Width"] = abs(int(user_input))

                    user_input = input("New video heigth: ")
                    while not user_input.isdigit():
                        print("Please input a whole number")
                        user_input = input("New video heigth: ")
                    newproject_object["New Video Height"] = abs(
                        int(user_input))

            print("The project setup has been completed! The script will now compute all the other required data on it's own.")

            print("Calculating Frames...")
            start_end_frame = get_frames(path)
            start_frame = start_end_frame[0]
            end_frame = start_end_frame[1]
            frame_count = end_frame - (start_frame - 1)
            current_frame = start_frame

    while True:
        command = input("Command (h for help): ").lower()

        if command == "h" or command == "help":
            print(f"n - New Project \nl - Open old Project")

        elif command == "l" or command == "load":
            project_path = input("Project: ")

            while not os.path.isfile(project_path):
                print("that is not a file")
                project_path = input("Project: ")

            load_project(project_path)

        elif command == "n" or command == "new":
            path_input = input("Project: ")

            while not os.path.isfile(path_input) and not path_input.endswith(".blend"):
                print("that is not a file")
                path_input = input("Project: ")

            global project_settings
            while project_settings[0] != "y" and project_settings[0] != "n":
                project_settings[0] = input("Save poroject? y/n: ").lower()
                print(project_settings)

            while project_settings[1] != "eevee" and project_settings[1] != "cycles":
                project_settings[1] = input(
                    "Render engine? eevee/cycles: ").lower()

            while project_settings[2] != "y" and project_settings[2] != "n":
                project_settings[2] = input("Generate MP4? y/n: ").lower()

            if project_settings[2] == "y":
                while not project_settings[3].isdigit():
                    project_settings[3] = input("Video FPS? Whole Number: ")

                while project_settings[4] != "crf" and project_settings[4] != "cbr":
                    project_settings[4] = input(
                        "Rate Control Type? cbr/crf: ").lower()

                while not project_settings[5].isdigit():
                    project_settings[5] = input("Value?: ")

                while project_settings[6] != "y" and project_settings[6] != "n":
                    project_settings[6] = input(
                        "Upscale output? y/n: ").lower()

                if project_settings[6] == "y":
                    while not project_settings[7].isdigit():
                        project_settings[7] = input("Output width: ")

                    while not project_settings[8].isdigit():
                        project_settings[8] = input("Output height: ")

            print("Calculating Frames...")
            global start_end_frame
            start_end_frame = get_frames(path)
            print(start_end_frame)
            global start_frame
            start_frame = start_end_frame[0]
            global end_frame
            end_frame = start_end_frame[1]
            global frame_count
            frame_count = end_frame - (start_frame - 1)
            current_frame = start_frame

            global frames_left
            while current_frame <= end_frame:
                frames_left.append(current_frame)
                current_frame += 1

            print("creating project...")
            save_project()

            print("Copying project to dataserver...")
            if ftp_local:
                return  # shutil.copy(src, dst)
            else:
                global ftp_stuff

                session = ftplib.FTP()
                session.connect(ftp_stuff[0], ftp_stuff[1])
                print(ftp_stuff[3])
                print(ftp_stuff[4])
                session.login(ftp_stuff[3], ftp_stuff[4])
                file = open(path, 'rb')
                session.storbinary(f"STOR {blend_name}", file)
                file.close()
                session.quit()
                print("file uploaded")

            if jobs_on_the_go:
                job()

            #global active_project
            #active_project = True

        elif command == "l" or command == "load":
            return


def reset():
    global active_project
    active_project = False

    global path
    path = None  # = "C:/Users/alexj/Desktop/Red Render Farm/v2/Scene.blend"
    global blend_name
    blend_name = None

    global project_settings
    project_settings = ["save", "engine", "mp4", "fps",
                        "rate control", "value", "upscale", "width", "height"]

    global start_end_frame
    start_end_frame = None
    global start_frame
    start_frame = None
    global end_frame
    end_frame = None
    global frame_count
    frame_count = None
    global frames_left
    frames_left = []
    global frames_received
    frames_received = []

    #global active_project
    #active_project = False

    master("No")


def job():
    global master_ip, master_port
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind((master_ip, master_port))
    server_socket.listen()

    global ftp_stuff

    global frames_left
    global frames_received
    global blend_name

    global project_settings

    # 0 = clients working
    # 1 = master working
    # state = 0

    print("Waiting for clients...")

    while len(frames_received) < frame_count:
        try:
            (client_connected, client_address) = server_socket.accept()
            print(
                f"Accepted a connection request from {client_address[0]}:{client_address[1]}")

            data_from_client = client_connected.recv(1024).decode()
            print(client_address[0] + ": " + data_from_client)

            if data_from_client.startswith("new"):
                split = data_from_client.split('|')
                print(
                    f"Job-Request! {client_address}; Project: {blend_name}; Frame: {frames_left[0]}")
                client_connected.send(
                    f"here|{blend_name}|{frames_left[0]}|{project_settings[1]}|{ftp_stuff[0]}|{ftp_stuff[1]}|{ftp_stuff[2]}|{ftp_stuff[3]}|{ftp_stuff[4]}".encode())
                frames_left.remove(frames_left[0])

                # server_socket.close()

            elif data_from_client.startswith("done"):
                print("in done")
                split = data_from_client.split('|')

                print(
                    f"Receive! {client_address}; Project: {blend_name}; Frame: {split[1]}")
                print("after print")

                # download from ftp
                urllib.request.urlretrieve(
                    f'ftp://{ftp_stuff[3]}:{ftp_stuff[4]}@{ftp_stuff[0]}:{ftp_stuff[1]}/{split[2]}', working_dir + split[2])

                print("verifying...")
                im = Image.open(working_dir + split[2])
                try:
                    im.verify()
                    frames_received.append(int(split[1]))

                    save_project()
                except:
                    print("image faulty")
                    frames_left.append(int(split[1]))
                im.close()

                if delete_after_done:
                    try:
                        print("deleting file")
                        session = ftplib.FTP()
                        session.connect(ftp_stuff[0], ftp_stuff[1])
                        session.login(ftp_stuff[3], ftp_stuff[4])
                        session.delete(split[2])
                        session.quit()
                        print("deleted")
                    except:
                        print("couldn't delete file")

                try:
                    if split[3] == "new":
                        client_connected.send(
                            f"{blend_name}|{frames_left[0]}".encode())
                        frames_left.remove(frames_left[0])
                except:
                    print("no new job requested")

                # server_socket.close()

            elif data_from_client.startswith("error"):
                print("Error reported!")
                frames_left.append(int(data_from_client.split('|')[1]))
                # server_socket.close()

        except Exception as e:
            print("an ERROR occoured, continuing anyway")
            print(e)

    server_socket.shutdown()
    # server_socket.close()

    if delete_after_done:
        print("deleting project")
        session = ftplib.FTP()
        session.connect(ftp_stuff[0], ftp_stuff[1])
        session.login(ftp_stuff[3], ftp_stuff[4])
        session.delete(blend_name)
        session.quit()
        print("deleted")

    if project_settings[2] == "y":
        sys_path = ffmpeg_dir
        os.environ['PATH'] += ';' + sys_path

        if os.path.isfile("render.mp4"):
            os.remove("render.mp4")

        loc = os.path.join(working_dir + "frame_%04d.png")
        stream = ffmpeg.input(loc, start_number=start_end_frame[0])
        stream = ffmpeg.filter(stream, 'fps', fps=float(
            project_settings[3]), round='up')

        if project_settings[6] == "y":
            stream = ffmpeg.filter(stream, 'scale', int(
                project_settings[7]), int(project_settings[8]))

        if project_settings[4] == "crf":
            stream = ffmpeg.output(stream, "render.mp4",
                                   crf=int(project_settings[5]))
        elif project_settings[4] == "cbr":
            stream = ffmpeg.output(stream, "render.mp4",
                                   video_bitrate=int(project_settings[5]))
        ffmpeg.run(stream)

    reset()


def get_frames(path):
    import struct

    blendfile = open(path, "rb")

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

        struct.unpack('>i' if is_big_endian else '<i', blendfile.read(4))[0]
        sizeof_bhead_left -= 4

        # We don't care about the rest of the bhead struct
        blendfile.read(sizeof_bhead_left)

        # Now we want the scene name, start and end frame. this is 32bites long
        start_frame, end_frame = struct.unpack(
            '>2i' if is_big_endian else '<2i', blendfile.read(8))

        scenes.append(start_frame)
        scenes.append(end_frame)

    blendfile.close()

    return scenes


if __name__ == "__main__":
    subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
    subprocess.call([sys.executable, "-m", "pip",
                     "install", "--upgrade", "pip"])
    subprocess.call([sys.executable, "-m", "pip", "install", "ffmpeg-python"])
    subprocess.call([sys.executable, "-m", "pip", "install", "pillow"])

    master()

    # print(sys.argv)
    # try:
    #     arg = sys.argv[1]
    #     Master(arg)
    # except:
    #     Master("No")
    # load_settings(False)
