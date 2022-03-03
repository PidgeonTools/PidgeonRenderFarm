from genericpath import isdir
import sys
import os
import time
from zipfile import ZipFile
import shutil


def wait(c_name, con0, con1, con2):
    global export_name
    digits_str = str(con1)
    digits = len(digits_str)

    if digits == 1:
        export_name = "frame_" + "000" + digits_str + ".png"
    elif digits == 2:
        export_name = "frame_" + "00" + digits_str + ".png"
    elif digits == 3:
        export_name = "frame_" + "0" + digits_str + ".png"
    elif digits == 4:
        export_name = "frame_" + digits_str + ".png"

    print(export_name)

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
        if os.path.isfile(export_name):
            #zip = ZipFile(cName + ".zip", 'w')

            # for render in os.scandir("C:/frames"):
            # zip.write(render.path)

            # zip.close()

            m_zip = m.find(c_name + ".zip", exclude_deleted=True)
            if not m_zip == None:
                m.delete(m_zip[0])

            shutil.make_archive(
                c_name, 'zip', os.path.dirname(__file__) + '/frames')

            m.upload(c_name + ".zip")
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
