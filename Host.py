import os
import shutil
from zipfile import ZipFile
import time as waiter
import subprocess
import sys

from requests.models import ContentDecodingError


def host(firstBoot):
    if firstBoot == "Yes":
        subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
        subprocess.call([sys.executable, "-m", "pip",
                         "install", "--upgrade", "pip"])
        subprocess.call([sys.executable, "-m", "pip", "install", "mega.py"])
        subprocess.call([sys.executable, "-m", "pip",
                         "install", "ffmpeg-python"])

        firstBoot = "No"

    from mega import Mega
    import ffmpeg

    mega = Mega()
    global m
    m = mega.login()
    m = mega.login("", "")

    global clients
    clients = ["a|2.1", "b|2.5"]

    global clientZIP
    clientZIP = []

    for c in clients:
        list = c.split("|")
        clientZIP.append(list[0] + ".zip")

    global startEnd
    startEnd = []

    global project
    project = False

    global copy
    copy = None

    global projectSettings
    projectSettings = ["zip", "mp4", "fps",
                       "rate control", "value", "upscale", "res"]

    def getFrames(blend):
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

            fName = os.path.basename(path)
            print(fName)

            size = os.path.getsize(path)
            usedTotal = m.get_storage_space()
            maxSize = int(usedTotal["total"] - usedTotal["used"])

            print(maxSize)

            if size < maxSize:
                print("Enough free space")

                while projectSettings[0] != "y" or projectSettings[0] != "n":
                    projectSettings[0] = input("Extract Frames? y/n: ").lower()

                if projectSettings[0] == "y" or projectSettings[0] == "n":
                    while projectSettings[1] != "y" or projectSettings[1] != "n":
                        projectSettings[1] = input(
                            "Generate MP4? y/n: ").lower()

                if projectSettings[1] == "y" or projectSettings[1] == "n":
                    while not projectSettings[2].isdigit():
                        projectSettings[2] = input("Video FPS? Whole Number: ")

                    while projectSettings[3] != "crf" or projectSettings[2] != "cbr":
                        projectSettings[3] = input(
                            "Rate Control Type? cbr/crf: ").lower()

                    while not projectSettings[4].isdigit():
                        projectSettings[4] = input("Bitrate? kbps: ")

                times = []
                for c in clients:
                    list = c.split("|")
                    times.append(float(list[1]))

                print("Calculating Time")
                global totalTime
                totalTime = 0
                for t in times:
                    totalTime = totalTime + t
                print(totalTime)

                print("Calculating Frames")
                startEnd = getFrames(path)
                print(startEnd)
                start = startEnd[0]
                end = startEnd[1]
                totalAmount = end - (start - 1)
                currentFrame = start

                print("Generating Ranges")
                for c in clients:
                    list = c.split("|")
                    name = list[0]
                    time = float(list[1])

                    percent = time / totalTime
                    print(percent)
                    rev_percent = 1.0 - percent
                    print(rev_percent)
                    cAmount = int(round(totalAmount * rev_percent))
                    print(name + str(cAmount))

                    filename = name + ".txt"
                    with open(filename, "w+") as f:
                        f.write(
                            f"{currentFrame}|{currentFrame + (cAmount - 1)}|{fName}")
                        f.close

                    m.upload(filename)

                    currentFrame = currentFrame + cAmount

                print("Copying")
                copy = shutil.copy2(path, fName)  # "FarmMe.blend")

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

    global allFiles
    allFiles = False

    '''os.chdir(os.path.dirname(__file__))
    if os.path.isdir("output"):
        shutil.rmtree(os.path.dirname(__file__) + '/output')
    os.mkdir("output")
    os.chdir(os.path.dirname(__file__) + '/output')
    if os.path.isdir("frames"):
        shutil.rmtree(os.path.dirname(__file__) + '/frames')
    os.mkdir("frames")'''
    os.chdir(os.path.dirname(__file__) + '/output')

    global cZIPe
    cZIPe = clientZIP

    for cZIP in cZIPe:
        zip = m.find(cZIP, exclude_deleted=True)
        print(zip)
        if zip != None:
            m.delete(zip[0])

    while project and not allFiles:
        print("checking")
        for cZIP in cZIPe:
            zip = m.find(cZIP, exclude_deleted=True)
            print(zip)
            if zip != None:
                m.download(zip, os.path.dirname(__file__) + "/output")
                m.delete(zip[0])
                #link = m.get_link(zip)
                # print(link)
                # m.delete_url(link)

                cZIPe.remove(cZIP)

                if all([os.path.isfile(f) for f in clientZIP]):
                    print("All there")
                    allFiles = True
                    break

        waiter.sleep(10)

    while project and allFiles:
        if projectSettings[0] == "y":
            os.chdir(os.path.dirname(__file__) + "/output")
            for cZIP in clientZIP:
                with ZipFile(os.path.dirname(__file__) + "/output/" + cZIP, "r") as fZIP:
                    print("extracting")
                    fZIP.extractall(os.path.dirname(
                        __file__) + "/output/" + "/frames")

            if projectSettings[1] == "y":
                SYSpath = 'C:/Program Files/ffmpeg/bin'
                os.environ['PATH'] += ';' + SYSpath

                os.chdir(os.path.dirname(__file__) + "/output")
                if os.path.isfile("render.mp4"):
                    os.remove("render.mp4")

                loc = os.path.join(os.path.dirname(
                    __file__) + "/output" + "/frames" + "/frame_%04d.png")
                stream = ffmpeg.input(loc, framerate=int(
                    projectSettings[2]), start_number=startEnd[0])
                if projectSettings[3] == "crf":
                    stream = ffmpeg.output(
                        stream, "render.mp4", crf=int(projectSettings[4]))
                elif projectSettings[3] == "cbr":
                    stream = ffmpeg.output(
                        stream, "render.mp4", video_bitrate=int(projectSettings[4]))
                ffmpeg.run(stream)

        pF = m.find(copy)
        m.delete(pF[0])

        host("No")


if __name__ == "__main__":
    print(sys.argv)
    host("No")
