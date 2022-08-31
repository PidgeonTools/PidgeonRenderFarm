from distutils.command.upload import upload
from logging import exception
import shutil
import subprocess
import sys
import os
import time
from zipfile import ZipFile
import socket
import ftplib
import urllib.request
import re
from PIL import Image

#---client related---#
client_ip = socket.gethostbyname(socket.gethostname())
settings_file = f"client_{client_ip}_settings.txt"

indicator = False
master_ip = None
master_port = None
blender_path = None
working_dir = None
script_dir = os.path.dirname(os.path.abspath(__file__))
print(script_dir)
hybrid = None
gpu = None
max_threads = None
max_ram = None
allow_eevee = None
allow_cycles = None
job_limit = None
time_limit = None

connected = False


def save_settings():
    f = open(settings_file, "w+")
    content = "127.0.0.1" + "\n"  # 0
    content += "1337" + "\n"  # 1
    # 2
    content += "D:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" + "\n"
    content += "." + "\n"  # 3
    # Hybrid                                                             #4
    content += "0" + "\n"
    # (OPENCL; CUDA; HIP; OPTIX)                                         #5
    content += "NONE" + "\n"
    # Threads                                                            #6
    content += "0" + "\n"
    # RAM limit                                                          #7
    content += "0" + "\n"
    # EEVEE                                                              #8
    content += "1" + "\n"
    # Cycles                                                             #9
    content += "1" + "\n"
    # Job limit                                                          #10
    content += "0" + "\n"
    # Time limit                                                         #11
    content += "0" + "\n"
    f.write(content)
    f.close()

    load_settings(True)


def load_settings(again: bool):
    try:
        f = open(settings_file, "r")
        content = [line[:-1] for line in f]

        content_inted = content
        content_inted[1] = int(content[1])
        content_inted[4] = bool(int(content[4]))
        if content_inted != "0":
            content_inted[6] = int(content[6])
        else:
            content_inted[6] = available_cpu_count()
        content_inted[7] = int(content[7])
        content_inted[8] = bool(int(content[8]))
        content_inted[9] = bool(int(content[9]))
        content_inted[10] = int(content[10])
        content_inted[11] = int(content[11])

        print(content_inted)
        f.close()

        global master_ip
        master_ip = content_inted[0]
        global master_port
        master_port = content_inted[1]
        global blender_path
        blender_path = content_inted[2]
        global working_dir
        if content_inted[3] == ".":
            working_dir = script_dir
        else:
            working_dir = content_inted[3]
        global hybrid
        hybrid = content_inted[4]
        global gpu
        gpu = content_inted[5].upper()
        global max_threads
        max_threads = content_inted[6]
        global max_ram
        max_ram = content_inted[7]
        global allow_eevee
        allow_eevee = content_inted[8]
        global allow_cycles
        allow_cycles = content_inted[9]
        global job_limit
        job_limit = content_inted[10]
        global time_limit
        time_limit = content_inted[11]

        global indicator
        indicator = True
    except:
        if again:
            sys.exit("settings file still broken")
        else:
            print("Settings file broken")
            save_settings()


def client(first_boot):
    if first_boot == "Yes":
        subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
        subprocess.call([sys.executable, "-m", "pip",
                         "install", "--upgrade", "pip"])

    global master_ip, master_port

    global connected

    if not indicator:
        load_settings(False)

    while True:
        try:
            print("trying to connect to master...")
            client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            # print(master_ip)
            client_socket.connect((master_ip, master_port))
            connected = True

            try:
                data = f"new|{allow_eevee}|{allow_cycles}"
                client_socket.send(data.encode())

                data_from_server = client_socket.recv(1024).decode()
                print(data_from_server)

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
                                        f'"{blender_path}" -b "{working_dir + split[1]}" -o "{working_dir}frame_####" --cyles-device {gpu}+CPU -t {max_threads} -f {split[2]}', shell=True)
                            else:
                                subprocess.run(
                                    f'"{blender_path}" -b "{working_dir + split[1]}" -o "{working_dir}frame_####" --cyles-device CPU -t {max_threads} -f {split[2]}', shell=True)

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
                                client_socket.connect((master_ip, master_port))
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
        except Exception as e:
            print("could not connect to the server, waiting 60 seconds")
            print(str(e))
            time.sleep(60)


def available_cpu_count():
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
        client("No")
