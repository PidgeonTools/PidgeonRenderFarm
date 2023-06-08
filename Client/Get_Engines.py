import bpy
import json
import os
import sys

# Seperate arguments
argv = sys.argv
index = argv.index("--") + 1
argv = argv[index:]

# Add Blender version and integrated engines to string
string = bpy.app.version_string + "\n"
string += "BLENDER_EEVEE\n"
string += "BLENDER_WORKBENCH\n"

# add all the other engines to string
for re in bpy.types.RenderEngine.__subclasses__():
    print(re.bl_idname)
    string += re.bl_idname + "\n"
   
# write string to file
with open(os.path.join(argv[0], "engines.json"), "w+") as f:
    f.write(string)

# Quit Blender and continue in C#
bpy.ops.wm.quit_blender()
