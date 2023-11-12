import bpy
import json
import os
import sys
from os import path

# write string to file
with open("version.txt", "w+") as f:
    f.write(bpy.app.version_string)

# Quit Blender and continue in C#
bpy.ops.wm.quit_blender()