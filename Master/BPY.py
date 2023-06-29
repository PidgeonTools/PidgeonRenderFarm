import bpy
import json
import sys
import os
import time
from os import path

# Seperate arguments
argv = sys.argv
index = argv.index("--") + 1
argv = argv[index:]

print(argv)

# Create new object containing Blender version, start frame, end frame, render engine, output file format and render time
json_object = {
    "blender_version": bpy.app.version_string,
    "first_frame": bpy.context.scene.frame_start,
    "last_frame": bpy.context.scene.frame_end,
    "frame_step": bpy.context.scene.frame_step,
    "render_engine": bpy.context.scene.render.engine,
    "file_format": bpy.context.scene.render.image_settings.file_format,
    "render_time": 0 # placeholder
}

# Unsupported formats replaced by PNG
if json_object["file_format"] in ["AVI_JPEG", "AVI_RAW", "FFMPEG"]:
    json_object["file_format"] = "PNG"
    bpy.context.scene.render.image_settings.file_format = json_object["file_format"]

# If opimization with SuperFastRender wanted, do it with default settings
if argv[0] == "1":
    # Try to call SFR
    try:
        bpy.ops.render.superfastrender_benchmark()
        # Save the new settings
        bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
    except AttributeError:
       print("SuperFastRender is NOT installed!")

# If render time test wanted, test it
if argv[1] == "1":
    startTime = time.time()
    bpy.context.scene.frame_current = bpy.context.scene.frame_start
    name = "frame_" + str(bpy.context.scene.frame_current).rjust(6, '0')
    bpy.context.scene.render.filepath = path.join(path.dirname(bpy.data.filepath), name)
    bpy.ops.render.render(write_still=True)
    # Pray it is compatible with C# float...
    json_object["render_time"] = time.time() - startTime


# Write object to .json
json_string = json.dumps(json_object)
with open(path.join(path.dirname(bpy.data.filepath), "vars.json"), "w+") as f:
    f.write(json_string)


# Quit Blender and continue in C#
bpy.ops.wm.quit_blender()
