from genericpath import isdir
import sys
import os
import time
from zipfile import ZipFile
import shutil


def wait(cName, con0, con1, con2):
    global exportName
    digits_str = str(con1)
    digits = len(digits_str)

    if digits == 1:
        exportName = "frame_" + "000" + digits_str + ".png"
    elif digits == 2:
        exportName = "frame_" + "00" + digits_str + ".png"
    elif digits == 3:
        exportName = "frame_" + "0" + digits_str + ".png"
    elif digits == 4:
        exportName = "frame_" + digits_str + ".png"

    print(exportName)

    print("Rendering")

    global isdir
    isdir = False

    os.chdir(os.path.dirname(__file__))

    while not isdir:
        if os.path.isdir("frames"):
            os.chdir(os.path.dirname(__file__) + '/frames')
            isdir = True
            break

        time.sleep(15)

    print("First Frame Rendered")

    time.sleep(10)

    from mega import Mega
    #global mega
    mega = Mega()
    global m
    #m = mega.login()
    m = mega.login("", "")

    while isdir:
        if os.path.isfile(exportName):
            #zip = ZipFile(cName + ".zip", 'w')

            # for render in os.scandir("C:/frames"):
            # zip.write(render.path)

            # zip.close()

            mZIP = m.find(cName + ".zip", exclude_deleted=True)
            if not mZIP == None:
                m.delete(mZIP[0])

            shutil.make_archive(
                cName, 'zip', os.path.dirname(__file__) + '/frames')

            m.upload(cName + ".zip")
            os.chdir(os.path.dirname(__file__))
            #os.chdir(os.path.dirname(__file__) + "/job")
            shutil.rmtree(os.path.dirname(__file__) + '/frames')
            #os.remove(cName + ".zip")
            # os.remove(con[2])
            #shutil.rmtree(os.path.dirname(__file__) + '/job')

            os.system("Farm.py No")

        else:
            print("Still Rendering")

        time.sleep(30)


if __name__ == "__main__":
    print(sys.argv)
    name = str(sys.argv[1])
    con0 = int(sys.argv[2])
    con1 = int(sys.argv[3])
    con2 = sys.argv[4]
    wait(name, con0, con1, con2)
