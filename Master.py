#import shutil
import subprocess
import sys
import os
import socket
from PIL import Image
import ftplib
import urllib.request
import ffmpeg

#---Master related---#
master_ip = socket.gethostbyname(socket.gethostname())
settings_file = f"master_{master_ip}_settings.txt"

indicator = False
'''ftp_url = None       #0
ftp_port = None      #1
ftp_dir = None       #2
ftp_user = None      #3
ftp_password = None  #4'''

ftp_stuff = []  # None, None, None, None, None, None]
ftp_local = False
delete_after_done = False
master_port = None
working_dir = None
script_dir = os.path.dirname(os.path.abspath(__file__))
print(script_dir)
ffmpeg_dir = None

#global active_project
#active_project = False

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


def save_settings():
    f = open(settings_file, "w+")
    content = "127.0.0.1" + "\n"  # 0
    content += "1337" + "\n"  # 1
    content += "." + "\n"  # 2
    content += "user" + "\n"  # 3
    content += "password" + "\n"  # 4
    content += "0" + "\n"  # 5
    content += "9090" + "\n"  # 6
    content += "." + "\n"  # 7
    content += "D:/Program Files/ffmpeg/bin" + "\n"  # 8
    f.write(content)
    f.close()

    load_settings(True)


def load_settings(again: bool):
    try:
        f = open(settings_file, "r")
        content = [line[:-1] for line in f]
        f.close()

        content_inted = content
        content_inted[1] = int(content[1])
        content_inted[6] = bool(int(content[5]))
        content_inted[7] = int(content[6])

        print(content_inted)

        global ftp_stuff
        global ftp_local
        if content_inted[0] == master_ip:
            ftp_local = True
        ftp_stuff.append(content_inted[0])
        ftp_stuff.append(content_inted[1])
        if content_inted[2] == ".":
            ftp_stuff.append("")
        else:
            ftp_stuff.append(content_inted[2])
        ftp_stuff.append(content_inted[3])
        ftp_stuff.append(content_inted[4])

        global delete_after_done
        delete_after_done = content_inted[5]
        global master_port
        master_port = content_inted[6]
        global working_dir
        if content_inted[7] == ".":
            working_dir = script_dir
        else:
            working_dir = content_inted[7]
        global ffmpeg_dir
        ffmpeg_dir = content_inted[8]

        global indicator
        indicator = True
    except:
        if again:
            sys.exit("settings file still broken")
        else:
            print("Settings file broken")
            save_settings()


def save_project():
    global project_name, path, blend_name, project_settings, start_frame, end_frame, frame_count, frames_left, frames_received

    f = open(project_name + ".rrfp", "w+")
    content = path + "\n"
    content += blend_name + "\n"

    content += project_settings[0] + "\n"
    content += project_settings[1] + "\n"
    content += project_settings[2] + "\n"
    content += project_settings[3] + "\n"
    content += project_settings[4] + "\n"
    content += project_settings[5] + "\n"
    content += project_settings[6] + "\n"
    content += project_settings[7] + "\n"
    content += project_settings[8] + "\n"

    content += str(start_frame) + "\n"
    content += str(end_frame) + "\n"
    content += str(frame_count) + "\n"

    '''content += "[" + "\n"
    for left in frames_left:
        content += left + "\n"
    content += "]" + "\n"'''

    content += "[" + "\n"
    for received in frames_received:
        content += str(received) + "\n"
    content += "]" + "\n"

    f.write(content)
    f.close()


def load_project(project_path: str):
    global project_name, path, blend_name, project_settings, start_frame, end_frame, frame_count, frames_left, frames_received

    try:
        f = open(project_path, "r")
        content = [line[:-1] for line in f]
        f.close()

        project_name = project_path.split('.')[0]
        path = content[0]
        blend_name = content[1]

        project_settings[0] = content[2]
        project_settings[1] = content[3]
        project_settings[2] = content[4]
        project_settings[3] = content[5]
        project_settings[4] = content[6]
        project_settings[5] = content[7]
        project_settings[6] = content[8]
        project_settings[7] = content[9]
        project_settings[8] = content[10]

        start_frame = int(content[11])
        end_frame = int(content[12])
        frame_count = int(content[13])

        tmp = start_frame
        tmp_list = []

        while tmp <= end_frame:
            tmp_list.append(tmp)
            tmp += 1

        tmp = 15

        while content[tmp] != "]":
            frames_received.append(int(content[tmp]))
            tmp += 1

        for part in tmp_list:
            if not part in frames_received:
                frames_left.append(part)

        job()
    except:
        sys.exit("project file broken")


def master(first_boot):
    if first_boot == "Yes":
        subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
        subprocess.call([sys.executable, "-m", "pip",
                         "install", "--upgrade", "pip"])
        subprocess.call([sys.executable, "-m", "pip",
                         "install", "ffmpeg-python"])

    if not indicator:
        load_settings(False)

    jobs_on_the_go = True
    global ftp_local

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
            global path
            path = input("Project: ")

            while not os.path.isfile(path):
                print("that is not a file")
                path = input("Project: ")

            global blend_name
            blend_name = os.path.split(path)[1]
            global project_name
            project_name = blend_name.split('.')[0]

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
                split = data_from_client.split('|')

                print(
                    f"Receive! {client_address}; Project: {blend_name}; Frame: {frames_left[0]}")

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

        except:
            print("an ERROR occoured, continuing anyway")

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
    # print(sys.argv)
    try:
        arg = sys.argv[1]
        master(arg)
    except:
        master("No")
        # load_settings(False)
