import bpy
import json
import os
import sys
from os import path

# Add Blender version and integrated engines to string
string = bpy.app.version_string + "\n"
string += "BLENDER_EEVEE\n"
string += "BLENDER_WORKBENCH\n"

# add all the other engines to string
for re in bpy.types.RenderEngine.__subclasses__():
    try:
        print(re.bl_idname)
        string += re.bl_idname + "\n"
    except:
        pass
   
# write string to file
with open(path.join(path.dirname(bpy.app.binary_path), "engines.txt"), "w+") as f:
    f.write(string)

# Quit Blender and continue in C#
bpy.ops.wm.quit_blender()
