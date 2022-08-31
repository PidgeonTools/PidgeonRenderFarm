import json
#import shutil
import subprocess
import sys
import os
import time
#from zipfile import ZipFile
import socket
#import ftplib
#import urllib.request
import re
from PIL import Image

#---client related---#
client_ip = socket.gethostbyname(socket.gethostname())
print(client_ip)
settings_file = f"client_{client_ip}_settings.json"
settings_object: dict = {}

indicator = False
script_dir = os.path.dirname(os.path.abspath(__file__))
print(script_dir)

connected = False


async def save_settings(save_object: dict = {}):
    if save_object == {}:
        save_object = {
            "Master IP": "192.168.178.117",
            "Master Port": 9090,
            "Blender Executable": "D:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe",
            "Working Directory": ".",
            # CPU, CUDA, OPTIX, HIP, METAL, (OPENCL) / CUDA+CPU, OPTIX+CPU, HIP+CPU, METAL+CPU, (OPENCL+CPU)
            "Render Device": "CPU",
            "Thread Limit": 0,
            "RAM Limit": 0,
            "Allow EEVEE": True,
            "Allow Cycles": True,
            "Allow Workbench": True,
            "Job Limit": 0,
            "Time Limit": 0
        }

    global settings_object
    settings_object = save_object

    json_string = json.dumps(save_object)

    with open(settings_file, "w+") as file_to_write:
        file_to_write.write(json_string)


async def load_settings(again: bool = False):
    if os.path.isfile(settings_file):
        global settings_object

        with open(settings_file, "r") as loaded_settings_file:
            loaded_string = loaded_settings_file.read()
            settings_object = json.loads(loaded_string)

        print(settings_object)

    else:
        save_settings()


def client(first_boot: str = "Yes"):
    if first_boot == "Yes":
        subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
        subprocess.call([sys.executable, "-m", "pip",
                         "install", "--upgrade", "pip"])

    # os.mkdir("job")

    global settings_object
    global connected

    load_settings()

    while True:
        while not connected:
            try:
                print("trying to connect to master...")
                client_socket = socket.socket(
                    socket.AF_INET, socket.SOCK_STREAM)
                client_socket.connect(
                    (settings_object["Master IP"], settings_object["Master Port"]))
                connected = True
            except Exception as e:
                print("could not connect to the server, waiting 60 seconds")
                print(str(e))
                time.sleep(60)

        try:
            data_object_to_server = {
                "Message": "New",
                "RAM Limit": 0,
                "Allow EEVEE": True,
                "Allow Cycles": True,
                "Allow Workbench": True,
            }
            data_to_server = json.dumps(data_object_to_server)
            client_socket.send(data_to_server.encode())

            data_from_server = client_socket.recv(1024).decode()
            data_object_from_server = json.loads(data_from_server)

            # open

            client_socket.close()
            connected = False

            if data_from_server.startswith("here"):
                split = data_from_server.split('|')

                print(
                    f"Job-Request! {master_ip}; Project: {split[1]}; Frame: {split[2]}")

                if not os.path.isfile(working_dir + split[1]):
                    print("downloading project")
                    urllib.request.urlretrieve(
                        f'ftp://{split[7]}:{split[8]}@{split[4]}:{split[5]}/{split[1]}', working_dir + split[1])

                if os.path.isfile(split[1]):
                    done = False
                    upload = False
                    bad = False

                    print("Starting blender render")
                    if split[3] == "eevee":
                        subprocess.run(
                            f'"{blender_path}" -b "{working_dir + split[1]}" -o "{working_dir}frame_####" -f {split[2]}', shell=True)

                    elif split[3] == "cycles":
                        if gpu != "NONE":
                            if hybrid:
                                subprocess.run(
                                    f'"{blender_path}" -b "{working_dir + split[1]}" -o "{working_dir}frame_####" --cycles-device {gpu} -f {split[2]}', shell=True)
                            else:
                                subprocess.run(
                                    f'"{blender_path}" -b "{working_dir + split[1]}" -o "{working_dir}frame_####" --cyles-device {gpu}+CPU -t {maxThreads} -f {split[2]}', shell=True)
                        else:
                            subprocess.run(
                                f'"{blender_path}" -b "{working_dir + split[1]}" -o "{working_dir}frame_####" --cyles-device CPU -t {maxThreads} -f {split[2]}', shell=True)

                    print("verifying...")
                    export_name = ""
                    digits = len(split[2])

                    if digits == 1:
                        export_name = "frame_" + "000" + split[2] + ".png"
                    elif digits == 2:
                        export_name = "frame_" + "00" + split[2] + ".png"
                    elif digits == 3:
                        export_name = "frame_" + "0" + split[2] + ".png"
                    elif digits == 4:
                        export_name = "frame_" + split[2] + ".png"

                    try:
                        im = Image.open(working_dir + export_name)
                        im.verify()
                        im.close()
                    except:
                        print("faulty image detected")
                        bad = True
                        upload = True

                    while not upload:
                        try:
                            print("trying to upload...")
                            session = ftplib.FTP()
                            session.connect(split[4], int(split[5]))
                            session.login(split[7], split[8])
                            file = open(working_dir + export_name, 'rb')
                            session.storbinary(f"STOR {export_name}", file)
                            file.close()
                            session.quit()
                            print("file uploaded")
                            upload = True
                        except:
                            print("uploading has failed, waiting 30 seconds")
                            time.sleep(30)

                    while not connected:
                        try:
                            print("trying to connect to master...")
                            client_socket = socket.socket(
                                socket.AF_INET, socket.SOCK_STREAM)
                            client_socket.connect((master_ip, masterPORT))
                            connected = True
                        except Exception as e:
                            print(
                                "could not connect to the server, waiting 60 seconds")
                            print(str(e))
                            time.sleep(60)

                    while not done:
                        try:
                            print("trying to report...")

                            if bad:
                                data = "error"
                                print(data)
                                client_socket.send(data.encode())
                                done = True

                            else:
                                data = f"done|{split[2]}|{export_name}"
                                print(data)
                                client_socket.send(data.encode())
                                done = True

                        except Exception as e:
                            print("ERROR while transfering, waiting 60 seconds")
                            print(e)
                            time.sleep(60)

                        client_socket.close()
        except Exception as e:
            print("an ERROR occoured, waiting 60 seconds")
            print(str(e))
            time.sleep(60)


async def available_cpu_count():
    """ Number of available virtual or physical CPUs on this system, i.e.
    user/real as output by time(1) when called with an optimally scaling
    userspace-only program"""

    # cpuset
    # cpuset may restrict the number of *available* processors
    try:
        m = re.search(r'(?m)^Cpus_allowed:\s*(.*)$',
                      open('/proc/self/status').read())
        if m:
            res = bin(int(m.group(1).replace(',', ''), 16)).count('1')
            if res > 0:
                return res
    except IOError:
        pass

    # Python 2.6+
    try:
        import multiprocessing
        return multiprocessing.cpu_count()
    except (ImportError, NotImplementedError):
        pass

    # POSIX
    try:
        res = int(os.sysconf('SC_NPROCESSORS_ONLN'))

        if res > 0:
            return res
    except (AttributeError, ValueError):
        pass

    # Windows
    try:
        res = int(os.environ['NUMBER_OF_PROCESSORS'])

        if res > 0:
            return res
    except (KeyError, ValueError):
        pass

    # BSD
    try:
        sysctl = subprocess.Popen(['sysctl', '-n', 'hw.ncpu'],
                                  stdout=subprocess.PIPE)
        sc_stdout = sysctl.communicate()[0]
        res = int(sc_stdout)

        if res > 0:
            return res
    except (OSError, ValueError):
        pass

    # Linux
    try:
        res = open('/proc/cpuinfo').read().count('processor\t:')

        if res > 0:
            return res
    except IOError:
        pass

    # Solaris
    try:
        pseudo_devices = os.listdir('/devices/pseudo/')
        res = 0
        for pd in pseudo_devices:
            if re.match(r'^cpuid@[0-9]+$', pd):
                res += 1

        if res > 0:
            return res
    except OSError:
        pass

    # Other UNIXes (heuristic)
    try:
        try:
            dmesg = open('/var/run/dmesg.boot').read()
        except IOError:
            dmesgProcess = subprocess.Popen(['dmesg'], stdout=subprocess.PIPE)
            dmesg = dmesgProcess.communicate()[0]

        res = 0
        while '\ncpu' + str(res) + ':' in dmesg:
            res += 1

        if res > 0:
            return res
    except OSError:
        pass

    raise Exception('Can not determine number of CPUs on this system')


if __name__ == "__main__":
    print(sys.argv)
    try:
        arg = sys.argv[1]
        client(arg)
    except:
        client()
