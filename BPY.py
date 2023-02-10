import bpy
import json
import sys
import os
import time

# Seperate arguments
argv = sys.argv
index = argv.index("--") + 1
argv = argv[index:]

# print(argv[0])

# Create new object containing Blender version, start frame, end frame, render engine, output file format and render time
j_o = {
    "VER": bpy.app.version_string,
    "FS": bpy.context.scene.frame_start,
    "FE": bpy.context.scene.frame_end,
    "RE": bpy.context.scene.render.engine,
    "FF": bpy.context.scene.render.image_settings.file_format,
    "RT": 0
}

# Unsupported formats replaced by PNG
if j_o["FF"] in ["AVI_JPEG", "AVI_RAW", "FFMPEG"]:
    j_o["FF"] = "PNG"

# If render time test wanted, test it
if argv[1] == "1":
    startTime = time.time()
    bpy.ops.render.render()
    j_o["RT"] = time.time() - startTime

# Write object to .json
j_s = json.dumps(j_o)
with open(os.path.join(argv[0], "vars.json"), "w+") as f:
    f.write(j_s)

# Quit Blender and continue in pure Python
bpy.ops.wm.quit_blender()
