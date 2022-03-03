import os
import shutil
from zipfile import ZipFile
import time as waiter
import subprocess
import sys

from requests.models import ContentDecodingError


def host(first_boot):
    if first_boot == "Yes":
        subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
        subprocess.call([sys.executable, "-m", "pip",
                         "install", "--upgrade", "pip"])
        subprocess.call([sys.executable, "-m", "pip", "install", "mega.py"])
        subprocess.call([sys.executable, "-m", "pip",
                         "install", "ffmpeg-python"])

        first_boot = "No"

    from mega import Mega
    import ffmpeg

    mega = Mega()
    global m
    m = mega.login()
    m = mega.login("", "")

    global clients
    clients = ["a|2.1", "b|2.5"]

    global client_zip
    client_zip = []

    for c in clients:
        list = c.split("|")
        client_zip.append(list[0] + ".zip")

    global start_end
    start_end = []

    global project
    project = False

    global copy
    copy = None

    global project_settings
    project_settings = ["zip", "mp4", "fps",
                        "rate control", "value", "upscale", "res"]

    def get_frames(blend):
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

        blendfile.close()

        return scenes

    while not project:
        command = input("Command: ").lower()

        if command == "h" or command == "help":
            print(f"n - New Project \n\
a - manage account \n\
cs - specify clients")

        elif command == "n" or command == "new":
            path = input("File: ")

            f_name = os.path.basename(path)
            print(f_name)

            size = os.path.getsize(path)
            used_total = m.get_storage_space()
            max_size = int(used_total["total"] - used_total["used"])

            print(max_size)

            if size < max_size:
                print("Enough free space")

                while project_settings[0] != "y" or project_settings[0] != "n":
                    project_settings[0] = input(
                        "Extract Frames? y/n: ").lower()

                if project_settings[0] == "y" or project_settings[0] == "n":
                    while project_settings[1] != "y" or project_settings[1] != "n":
                        project_settings[1] = input(
                            "Generate MP4? y/n: ").lower()

                if project_settings[1] == "y" or project_settings[1] == "n":
                    while not project_settings[2].isdigit():
                        project_settings[2] = input(
                            "Video FPS? Whole Number: ")

                    while project_settings[3] != "crf" or project_settings[2] != "cbr":
                        project_settings[3] = input(
                            "Rate Control Type? cbr/crf: ").lower()

                    while not project_settings[4].isdigit():
                        project_settings[4] = input("Bitrate? kbps: ")

                times = []
                for c in clients:
                    list = c.split("|")
                    times.append(float(list[1]))

                print("Calculating Time")
                global total_time
                total_time = 0
                for t in times:
                    total_time = total_time + t
                print(total_time)

                print("Calculating Frames")
                start_end = get_frames(path)
                print(start_end)
                start = start_end[0]
                end = start_end[1]
                total_amount = end - (start - 1)
                current_frame = start

                print("Generating Ranges")
                for c in clients:
                    list = c.split("|")
                    name = list[0]
                    time = float(list[1])

                    percent = time / total_time
                    print(percent)
                    rev_percent = 1.0 - percent
                    print(rev_percent)
                    c_amount = int(round(total_amount * rev_percent))
                    print(name + str(c_amount))

                    filename = name + ".txt"
                    with open(filename, "w+") as f:
                        f.write(
                            f"{current_frame}|{current_frame + (c_amount - 1)}|{f_name}")
                        f.close

                    m.upload(filename)

                    current_frame = current_frame + c_amount

                print("Copying")
                copy = shutil.copy2(path, f_name)  # "FarmMe.blend")

                print("Uploading")
                m.upload(copy)

                project = True
                break

        elif command == "a":
            e = input("Email: ")
            p = input("Password: ")

            m = mega.login(e, p)

        elif command == "cs":
            path = input("File: ")

            with open(path, "r") as f:
                clients = f.read

    global all_files
    all_files = False

    '''os.chdir(os.path.dirname(__file__))
    if os.path.isdir("output"):
        shutil.rmtree(os.path.dirname(__file__) + '/output')
    os.mkdir("output")
    os.chdir(os.path.dirname(__file__) + '/output')
    if os.path.isdir("frames"):
        shutil.rmtree(os.path.dirname(__file__) + '/frames')
    os.mkdir("frames")'''
    os.chdir(os.path.dirname(__file__) + '/output')

    global c_zip_e
    c_zip_e = client_zip

    for c_zip in c_zip_e:
        zip = m.find(c_zip, exclude_deleted=True)
        print(zip)
        if zip != None:
            m.delete(zip[0])

    while project and not all_files:
        print("checking")
        for c_zip in c_zip_e:
            zip = m.find(c_zip, exclude_deleted=True)
            print(zip)
            if zip != None:
                m.download(zip, os.path.dirname(__file__) + "/output")
                m.delete(zip[0])
                #link = m.get_link(zip)
                # print(link)
                # m.delete_url(link)

                c_zip_e.remove(c_zip)

                if all([os.path.isfile(f) for f in client_zip]):
                    print("All there")
                    all_files = True
                    break

        waiter.sleep(10)

    while project and all_files:
        if project_settings[0] == "y":
            os.chdir(os.path.dirname(__file__) + "/output")
            for c_zip in client_zip:
                with ZipFile(os.path.dirname(__file__) + "/output/" + c_zip, "r") as fZIP:
                    print("extracting")
                    fZIP.extractall(os.path.dirname(
                        __file__) + "/output/" + "/frames")

            if project_settings[1] == "y":
                sys_path = 'C:/Program Files/ffmpeg/bin'
                os.environ['PATH'] += ';' + sys_path

                os.chdir(os.path.dirname(__file__) + "/output")
                if os.path.isfile("render.mp4"):
                    os.remove("render.mp4")

                loc = os.path.join(os.path.dirname(
                    __file__) + "/output" + "/frames" + "/frame_%04d.png")
                stream = ffmpeg.input(loc, framerate=int(
                    project_settings[2]), start_number=start_end[0])
                if project_settings[3] == "crf":
                    stream = ffmpeg.output(
                        stream, "render.mp4", crf=int(project_settings[4]))
                elif project_settings[3] == "cbr":
                    stream = ffmpeg.output(
                        stream, "render.mp4", video_bitrate=int(project_settings[4]))
                ffmpeg.run(stream)

        p_f = m.find(copy)
        m.delete(p_f[0])

        host("No")


if __name__ == "__main__":
    print(sys.argv)
    host("No")
