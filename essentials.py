import ipaddress
import shutil
import string
import random
import math
from colorama import init, Fore, Back, Style
import subprocess
import os
import re


def help_message():
    print("##################################################")
    print("N    -   New project")
    print("L    -   Load project")
    print("S    -   Re-Run setup")
    print("H    -   Show this")
    print("##################################################")


def generate_project_id(length: int = 8):
    letters = string.ascii_letters + string.digits
    id = ""

    for i in range(length):
        id += random.choice(letters)

    return id


def input_to_bool(input: str):
    if input.lower() == "true" or input.lower() == "yes":
        return True
    elif input.lower() == "false" or input.lower() == "no":
        return False


def is_bool(input: str):
    if input.lower() == "true":
        return True
    elif input.lower() == "yes":
        return True
    elif input.lower() == "false":
        return True
    elif input.lower() == "no":
        return True

    return False


def validate_ip(address: str = "127.0.0.1"):
    try:
        ipaddress.ip_address(address)
        return True
    except:
        return False


def show_progress_bar(base, part):
    columns, rows = shutil.get_terminal_size()

    leftover = base - part
    percent = round(part / base * 100)
    percent_leftover = 3 - len(str(percent))

    factor = math.floor((columns - 6) / base)

    bar = f'{" " * percent_leftover}{percent}%|{"█" * (part * factor)}{" " * (leftover * factor)}|'
    #" 50%|█████     |"

    print(bar)


def show_progress_bar2(base, part):
    columns, rows = shutil.get_terminal_size()

    leftover = base - part
    part_leftover = len(str(base)) - len(str(part))
    percent = round(part / base * 100)
    percent_leftover = 3 - len(str(percent))

    factor = math.floor((columns - 8 - part_leftover -
                         len(str(part)) - len(str(base))) / base)

    bar = f'{" " * percent_leftover}{percent}%|{"█" * (part * factor)}{" " * (leftover * factor)}| {"" * part_leftover}{part}/{base}'
    #" 50%|█████     | 1/2"

    print(bar)


def show_progress_bar3(base, part):
    columns, rows = shutil.get_terminal_size()
    init(autoreset=True)

    leftover = base - part
    percent = round(part / base * 100)
    percent_leftover = 3 - len(str(percent))

    factor = math.floor((columns - 6) / base)

    ending = "\n"
    if part != base:
        bar = f'{" " * percent_leftover}{percent}%|{Fore.MAGENTA}{"━" * (part * factor)}{Fore.WHITE}{"━" * (leftover * factor)}|'
        ending = "\r"
    else:
        bar = f'{" " * percent_leftover}{percent}%|{Fore.GREEN}{"━" * (part * factor)}{Fore.WHITE}|'
    #" 50%|━━━━━     |"

    print(bar, end=ending)


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
