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
json_object = {
    "blender_version": bpy.app.version_string,
    "first_frame": bpy.context.scene.frame_start,
    "last_frame": bpy.context.scene.frame_end,
    "render_engine": bpy.context.scene.render.engine,
    "file_format": bpy.context.scene.render.image_settings.file_format,
    "render_time": 0 # placeholder
}

# Unsupported formats replaced by PNG
if json_object["file_format"] in ["AVI_JPEG", "AVI_RAW", "FFMPEG"]:
    json_object["file_format"] = "PNG"

# If render time test wanted, test it
if argv[1] == "1":
    startTime = time.time()
    bpy.ops.render.render()
    # Pray it is compatible with C# float...
    json_object["render_time"] = time.time() - startTime

# Write object to .json
json_string = json.dumps(json_object)
with open(os.path.join(argv[0], "vars.json"), "w+") as f:
    f.write(json_string)

# Quit Blender and continue in C#
bpy.ops.wm.quit_blender()
