from distutils.command.upload import upload
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

#---Client related---#
client_ip = socket.gethostbyname(socket.gethostname())
settings_file = f"client_{client_ip}_settings.txt"

indicator = False
master_ip = None
master_port = None
blender_path = None
working_dir = None
script_dir = os.path.dirname(os.path.abspath(__file__))
print(script_dir)

connected = False


def save_settings():
    f = open(settings_file, "w+")
    content = "127.0.0.1" + "\n"  # 0
    content += "1337" + "\n"  # 1
    # 2
    content += "D:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe" + "\n"
    content += "." + "\n"  # 3
    content += "0" + "\n"  # 4
    # (OPENCL; CUDA; HIP; OPTIX)                                         #5
    content += "CUDA" + "\n"
    content += "0" + "\n"  # 6
    content += "y" + "\n"  # 7
    content += "y" + "\n"  # 8
    content += "0" + "\n"  # 9
    content += "0" + "\n"  # 10
    f.write(content)
    f.close()

    load_settings(True)


def load_settings(again: bool):
    try:
        f = open(settings_file, "r")
        content = [line[:-1] for line in f]

        content_inted = content
        content_inted[1] = int(content[1])
        content_inted[3] = bool(int(content[3]))
        content_inted[6] = bool(int(content[6]))
        content_inted[7] = int(content[7])

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

    os.chdir(os.path.dirname(__file__))
    if os.path.isdir("job"):
        shutil.rmtree(os.path.dirname(__file__) + '/job')
    # os.mkdir("job")

    global master_ip

    global ftp_url
    global ftp_port
    global ftp_user
    global ftp_password

    global connected

    # while not connected:

    while True:
        try:
            print("trying to connect to master...")
            client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            client_socket.connect(("127.0.0.1", 9090))
            connected = True

            try:
                data = "new"
                client_socket.send(data.encode())

                data_from_server = client_socket.recv(1024).decode()
                print(data_from_server)

                client_socket.close()
                connected = False

                if data_from_server.startswith("here"):
                    split = data_from_server.split('|')

                    print(
                        f"Job-Request! {master_ip}; Project: {split[1]}; Frame: {split[2]}")

                    urllib.request.urlretrieve(
                        f'ftp://{ftp_user}:{ftp_password}@{ftp_url}:{ftp_port}/{split[1]}', working_dir + split[1])

                    if os.path.isfile(split[1]):
                        done = False
                        upload = False
                        bad = False

                        print("Starting blender render")
                        subprocess.run(
                            f'"{blender_path}" -b "{working_dir + split[1]}" -o "{working_dir}frame_####" -f {split[2]}', shell=True)

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

                        if not os.path.isfile(working_dir + export_name):
                            print("bad image")
                            bad = True
                            upload = True

                        while not upload:
                            try:
                                print("trying to upload...")
                                session = ftplib.FTP()
                                session.connect(ftp_url, ftp_port)
                                session.login(ftp_user, ftp_password)
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
                                client_socket.connect(("127.0.0.1", 9090))
                                connected = True
                            except:
                                print(
                                    "could not connect to the server, waiting 60 seconds")
                                time.sleep(60)

                        while not done:
                            try:
                                print("trying to report...")

                                if bad:
                                    data = "error"
                                    client_socket.send(data.encode())

                                else:
                                    data = f"done|{split[2]}|{export_name}"
                                    client_socket.send(data.encode())

                                done = True
                            except:
                                print("ERROR while transfering, waiting 60 seconds")
                                time.sleep(60)

                        client_socket.close()
            except:
                print("an ERROR occoured, waiting 60 seconds")
                time.sleep(60)
        except:
            print("could not connect to the server, waiting 60 seconds")
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

    # https://github.com/giampaolo/psutil
    try:
        import psutil
        return psutil.cpu_count()   # psutil.NUM_CPUS on old versions
    except (ImportError, AttributeError):
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

    # jython
    try:
        from java.lang import Runtime
        runtime = Runtime.getRuntime()
        res = runtime.availableProcessors()
        if res > 0:
            return res
    except ImportError:
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
            dmesg_process = subprocess.Popen(['dmesg'], stdout=subprocess.PIPE)
            dmesg = dmesg_process.communicate()[0]

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
