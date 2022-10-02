import ipaddress
import shutil
import string
import random
import math


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
