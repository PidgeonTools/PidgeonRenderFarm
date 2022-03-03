import os
import time

rendering = False

output_file = os.path.isfile("a.txt")


def check_rendering():
    global output_file
    global rendering

    if output_file:
        rendering = False

        output_file = os.path.isfile("level_log1.txt")


while True:
    print(rendering)
    if not rendering:
        print("a")
        rendering = True

    if rendering:
        print("b")

        check_rendering()

    time.sleep(3)
