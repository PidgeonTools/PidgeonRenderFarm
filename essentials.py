import ipaddress
import shutil
import string
import random
import math
from colorama import init, Fore, Back, Style
import subprocess
import os
import re


def print_help_message():
    """Show available commands."""
    print("##################################################")
    print("N    -   New project")
    print("L    -   Load project")
    print("S    -   Re-Run setup")
    print("H    -   Show this message")
    print("##################################################")


def generate_project_id(length: int = 8):
    """Generate a random ID for a project."""
    letters = string.ascii_letters + string.digits
    id = ""

    for i in range(length):
        id += random.choice(letters)

    return id


def parse_bool(value: str, default: bool = None) -> bool:
    """Parse user input as boolean value.
    Return default_value or None, if the input can't be interpreted as boolean value."""
    if value.lower() in ["true", "yes", "y", "1"]:
        return True
    elif value.lower() in ["false", "no", "n", "0"]:
        return False
    elif value in ["", None]:
        return default

    return None


def validate_ip(address: str = "127.0.0.1"):
    """Validate a given IP adress."""
    try:
        ipaddress.ip_address(address)
        return True
    except:
        return False


def is_port(port: str = "9090"):
    """Check if a value is a port number."""
    if not port.isdigit():
        return False

    return int(port) >= 1 and int(port) <= 65535


def write_to_log(path: str, txt: str):
    with open(path, "a+") as f:
        f.write(txt)


class progressbar:
    def __init__(self, base, part=0):
        self.base = base
        self.part = part

        self.show_progressbar_variant2()

    def update(self, dif):
        self.part += dif
        self.show_progressbar_variant2()

    def show_progressbar_variant0(self):  # " 50%|█████     |"
        columns, rows = shutil.get_terminal_size()

        leftover = self.base - self.part
        percent = round(self.part / self.base * 100)
        percent_leftover = 3 - len(str(percent))

        factor = math.floor((columns - 6) / self.base)

        ending = "\n"
        if self.part != self.base:
            bar = f'{" " * percent_leftover}{percent}%|{Fore.MAGENTA}{"█" * (self.part * factor)}{Fore.WHITE}{" " * (leftover * factor)}|'
            ending = "\r"
        else:
            bar = f'{" " * percent_leftover}{percent}%|{Fore.GREEN}{"█" * (self.part * factor)}{Fore.WHITE}|'

        print(bar)  # , end=ending)

    def show_progressbar_variant1(self):  # " 50%|━━━━━━━━━━|"
        columns, rows = shutil.get_terminal_size()
        init(autoreset=True)

        leftover = self.base - self.part
        percent = round(self.part / self.base * 100)
        percent_leftover = 3 - len(str(percent))

        factor = math.floor((columns - 6) / self.base)

        ending = "\n"
        if self.part != self.base:
            bar = f'{" " * percent_leftover}{percent}%|{Fore.MAGENTA}{"━" * (self.part * factor)}{Fore.WHITE}{"━" * (leftover * factor)}|'
            ending = "\r"
        else:
            bar = f'{" " * percent_leftover}{percent}%|{Fore.GREEN}{"━" * (self.part * factor)}{Fore.WHITE}|'

        print(bar)  # , end=ending)

    def show_progressbar_variant2(self):  # " 50%|━━━━━     |"
        columns, rows = shutil.get_terminal_size()
        init(autoreset=True)

        leftover = self.base - self.part
        percent = round(self.part / self.base * 100)
        percent_leftover = 3 - len(str(percent))

        factor = math.floor((columns - 6) / self.base)

        ending = "\n"
        if self.part == 0:
            bar = f'{" " * percent_leftover}{percent}%| {" " * (leftover * factor - 1)}|'
        elif self.part != self.base:
            bar = f'{" " * percent_leftover}{percent}%|{Fore.MAGENTA}{"━" * (self.part * factor)}{Fore.WHITE}{" " * (leftover * factor)}|'
            ending = "\r"
        else:
            bar = f'{" " * percent_leftover}{percent}%|{Fore.GREEN}{"━" * (self.part * factor)}{Fore.WHITE}|'

        print(bar)  # , end=ending)

    def show_progressbar_variant3(self):  # " 50%|━━━━ ━━━━━|"
        columns, rows = shutil.get_terminal_size()
        init(autoreset=True)

        leftover = self.base - self.part
        percent = round(self.part / self.base * 100)
        percent_leftover = 3 - len(str(percent))

        factor = math.floor((columns - 6) / self.base)

        ending = "\n"
        if self.part == 0:
            bar = f'{" " * percent_leftover}{percent}%| {" " * (leftover * factor - 1)}|'
        elif self.part != self.base:
            bar = f'{" " * percent_leftover}{percent}%|{Fore.MAGENTA}{"━" * (self.part * factor - 1)}{Fore.WHITE} {"━" * (leftover * factor)}|'
            ending = "\r"
        else:
            bar = f'{" " * percent_leftover}{percent}%|{Fore.GREEN}{"━" * (self.part * factor)}{Fore.WHITE}|'

        print(bar)  # , end=ending)


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
