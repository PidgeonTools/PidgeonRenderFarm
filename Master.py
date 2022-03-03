from doctest import master
import re
import shutil
import subprocess
import sys
import os
import time
from tracemalloc import start
from zipfile import ZipFile
import socket
from PIL import Image
import ftplib
import urllib.request
#import ffmpeg

master_ip = socket.gethostbyname(socket.gethostname())
ftp_url = ""
ftp_port = 1337
ftp_user = ""
ftp_password = ""

working_path = ""

#global active_project
#active_project = False

#---Project related---#
path = None
blend_name = None

project_settings = ["zip", "mp4", "fps",
                    "rate control", "value", "upscale", "res"]

start_end_frame = None
start_frame = None
end_frame = None
frame_count = None
frames_left = []
frames_received = []

print("good")


def master(first_boot):
    if first_boot == "Yes":
        subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
        subprocess.call([sys.executable, "-m", "pip",
                         "install", "--upgrade", "pip"])
        subprocess.call([sys.executable, "-m", "pip",
                         "install", "ffmpeg-python"])

    jobs_on_the_go = True

    while True:
        command = input("Command (h for help): ").lower()

        if command == "h" or command == "help":
            print(f"n - New Project \na - manage account \ncs - specify clients")

        elif command == "n" or command == "new":
            global path
            path = input("Project: ")

            while not os.path.isfile(path):
                print("that is not a file")
                path = input("Project: ")

            global blend_name
            blend_name = os.path.split(path)[1]

            global project_settings
            '''while project_settings[0] != "y" or project_settings[0] != "n":
                    project_settings[0] = input("Extract Frames? y/n: ").lower()

            if project_settings[0] == "y" or project_settings[0] == "n":'''
            '''while project_settings[1] != "y" or project_settings[1] != "n":
                project_settings[1] = input("Generate MP4? y/n: ").lower()

            if project_settings[1] == "y" or project_settings[1] == "n":
                while not project_settings[2].isdigit():
                    project_settings[2] = input("Video FPS? Whole Number: ")

                while project_settings[3] != "crf" or project_settings[2] != "cbr":
                    project_settings[3] = input("Rate Control Type? cbr/crf: ").lower()

                while not project_settings[4].isdigit():
                    project_settings[4] = input("Value?: ")'''

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

            print("Copying project to dataserver...")
            global ftp_url
            global ftp_port
            global ftp_user
            global ftp_password

            session = ftplib.FTP()
            session.connect(ftp_url, ftp_port)
            session.login(ftp_user, ftp_password)
            file = open(path, 'rb')
            session.storbinary(f"STOR {blend_name}", file)
            file.close()
            session.quit()
            print("file uploaded")

            if jobs_on_the_go:
                job()

            #global active_project
            #active_project = True


def reset():
    global active_project
    active_project = False

    global path
    path = None  # = "C:/Users/alexj/Desktop/Red Render Farm/v2/Scene.blend"
    global blend_name
    blend_name = None

    global project_settings
    project_settings = ["zip", "mp4", "fps",
                        "rate control", "value", "upscale", "res"]

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


def NewProject():
    return


def job():
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(('127.0.0.1', 9090))
    server_socket.listen()

    global frames_left
    global frames_received
    global blend_name

    global ftp_url
    global ftp_port
    global ftp_user
    global ftp_password

    # 0 = clients working
    # 1 = master working
    state = 0

    print("Waiting for clients...")

    while len(frames_received) < frame_count:
        try:
            (client_connected, client_address) = server_socket.accept()
            print(
                f"Accepted a connection request from {client_address[0]}:{client_address[1]}")

            data_from_client = client_connected.recv(1024).decode()
            print(client_address[0] + ": " + data_from_client)

            if data_from_client.startswith("new"):
                print(
                    f"Job-Request! {client_address}; Project: {blend_name}; Frame: {frames_left[0]}")
                client_connected.send(
                    f"here|{blend_name}|{frames_left[0]}".encode())
                frames_left.remove(frames_left[0])

                # server_socket.close()

            elif data_from_client.startswith("done"):
                split = data_from_client.split('|')

                print(
                    f"Receive! {client_address}; Project: {blend_name}; Frame: {frames_left[0]}")

                # download from ftp
                urllib.request.urlretrieve(
                    f'ftp://{ftp_user}:{ftp_password}@{ftp_url}:{ftp_port}/{split[2]}', working_path + split[2])

                print("verifying...")
                im = Image.open(working_path + split[2])
                try:
                    im.verify()
                    frames_received.append(int(split[1]))

                    print("deleting file")
                    session = ftplib.FTP()
                    session.connect(ftp_url, ftp_port)
                    session.login(ftp_user, ftp_password)
                    file = open(path, 'rb')
                    session.delete(split[2])
                    file.close()
                    session.quit()
                    print("deleted")
                except:
                    print("image faulty")
                    frames_left.append(int(split[1]))
                im.close()

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

        except:
            print("an ERROR occoured, continuing anyway")

    server_socket.shutdown()
    # server_socket.close()

    print("deleting project")
    session = ftplib.FTP()
    session.connect(ftp_url, ftp_port)
    session.login(ftp_user, ftp_password)
    file = open(path, 'rb')
    session.delete(blend_name)
    file.close()
    session.quit()
    print("deleted")

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
    print(sys.argv)
    try:
        arg = sys.argv[1]
        master(arg)
    except:
        master("No")
