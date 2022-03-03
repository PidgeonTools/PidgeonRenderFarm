from pyftpdlib.servers import FTPServer
from pyftpdlib.handlers import FTPHandler
from pyftpdlib.authorizers import DummyAuthorizer

import subprocess
import sys

subprocess.call([sys.executable, "-m", "ensurepip", "--user"])
subprocess.call([sys.executable, "-m", "pip", "install", "--upgrade", "pip"])
subprocess.call([sys.executable, "-m", "pip", "install", "fyftplib"])


authorizer = DummyAuthorizer()
authorizer.add_anonymous("C:/")

handler = FTPHandler
handler.authorizer = authorizer

address = ("127.0.0.1", 21)
server = FTPServer(address, handler)
server.serve_forever()
