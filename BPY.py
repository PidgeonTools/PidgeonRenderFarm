import bpy
import json
import sys
import os

argv = sys.argv
index = argv.index("--") + 1
argv = argv[index:]

# print(argv[0])

j_o = {
    "VER": bpy.app.version_string,
    "RE": bpy.context.scene.render.engine,
    "FF": bpy.context.scene.render.image_settings.file_format,
    "RT": 0
}

j_s = json.dumps(j_o)

with open(os.path.join(argv[0], "vars.json"), "w+") as f:
    f.write(j_s)

bpy.ops.wm.quit_blender()
