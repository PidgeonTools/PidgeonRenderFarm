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

connected = False
client_ip = socket.gethostbyname(socket.gethostname())
master_ip = ""

ftp_url = ""
ftp_port = 1337
ftp_user = ""
ftp_password = ""

blender_path = "D:/Program Files (x86)/Steam/steamapps/common/Blender/blender.exe"
working_path = ""


def load_settings():
    return


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
                        f'ftp://{ftp_user}:{ftp_password}@{ftp_url}:{ftp_port}/{split[1]}', working_path + split[1])

                    if os.path.isfile(split[1]):
                        done = False
                        upload = False
                        bad = False

                        print("Starting blender render")
                        subprocess.run(
                            f'"{blender_path}" -b "{working_path + split[1]}" -o "{working_path}frame_####" -f {split[2]}', shell=True)

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

                        if not os.path.isfile(working_path + export_name):
                            print("bad image")
                            bad = True
                            upload = True

                        while not upload:
                            try:
                                print("trying to upload...")
                                session = ftplib.FTP()
                                session.connect(ftp_url, ftp_port)
                                session.login(ftp_user, ftp_password)
                                file = open(working_path + export_name, 'rb')
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


if __name__ == "__main__":
    print(sys.argv)
    try:
        arg = sys.argv[1]
        client(arg)
    except:
        client("No")
