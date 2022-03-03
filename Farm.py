import shutil
import subprocess
import sys
import os
import time
from zipfile import ZipFile


def client(first_boot):
    if first_boot == "Yes":
        subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
        subprocess.call([sys.executable, "-m", "pip",
                         "install", "--upgrade", "pip"])
        subprocess.call([sys.executable, "-m", "pip", "install", "mega.py"])

    from mega import Mega
    mega = Mega()
    global m
    #m = mega.login()
    m = mega.login("", "")

    global c_name
    c_name = 'a'
    info_file = c_name + '.txt'
    print(info_file)

    global content

    os.chdir(os.path.dirname(__file__))
    if os.path.isdir("job"):
        shutil.rmtree(os.path.dirname(__file__) + '/job')
    os.mkdir("job")

    while True:
        info = m.find(info_file, exclude_deleted=True)
        print(info)
        if info != None:
            m.download(info, os.path.dirname(__file__) + "/job")
            #link = m.export(infoFile)
            # m.download_url(link)

            os.chdir(os.path.dirname(__file__) + "/job")

            with open(info_file, "r") as f:
                con = f.read()

            content = con.split("|")
            print(content)

            m_blend = m.find(content[2], exclude_deleted=True)
            print(m_blend)

            if m_blend != None:
                m.download(m_blend, os.path.dirname(__file__) + "/job")

                print("Deleting info file")
                m.delete(info[0])

                blend = content[2]
                blend_path = os.path.abspath(blend)

                ziper = os.path.join(os.path.abspath(
                    os.path.dirname(__file__)), "ZIPer.py")
                with open("run.bat", "w+") as f:
                    f.write(
                        f'start "" "C:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" -b "{blend_path}" -o "{os.path.dirname(__file__) + "/frames/frame_####"}" -s {content[0]} -e {content[1]} -a\n')
                    f.write(
                        f'"{sys.executable}" "{ziper}" {c_name} {content[0]} {content[1]} "{content[2]}"')

                os.system("run.bat")
                break

        time.sleep(30)


if __name__ == "__main__":
    print(sys.argv)
    try:
        arg = sys.argv[1]
        client(arg)
    except:
        client("Yes")
