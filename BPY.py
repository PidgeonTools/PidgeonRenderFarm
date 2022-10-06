import bpy
import json
import sys
import os
import time

argv = sys.argv
index = argv.index("--") + 1
argv = argv[index:]

# print(argv[0])

j_o = {
    "VER": bpy.app.version_string,
    "FS": bpy.context.scene.frame_start,
    "FE": bpy.context.scene.frame_end,
    "RE": bpy.context.scene.render.engine,
    "FF": bpy.context.scene.render.image_settings.file_format,
    "RT": 0
}

if j_o["FF"] in ["AVI_JPEG", "AVI_RAW", "FFMPEG"]:
    j_o["FF"] = "PNG"

if argv[1] == "1":
    startTime = time.time()
    bpy.ops.render.render()
    j_o["RT"] = time.time() - startTime

j_s = json.dumps(j_o)

with open(os.path.join(argv[0], "vars.json"), "w+") as f:
    f.write(j_s)

bpy.ops.wm.quit_blender()
