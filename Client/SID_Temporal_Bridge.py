# Client script

import bpy
import sys
from os import path

# Seperate arguments
argv = sys.argv
index = argv.index("--") + 1
argv = argv[index:]

scene = bpy.context.scene
# Set SID Temporal settings
scene.sid_settings.denoiser_type = 'SIDT'
scene.sid_settings.working_directory = path.dirname(bpy.data.filepath)

bpy.context.scene.frame_start = int(argv[0])
bpy.context.scene.frame_end = int(argv[1])
bpy.context.scene.frame_step = int(argv[2])

bpy.ops.superimagedenoiser.sidtrender()