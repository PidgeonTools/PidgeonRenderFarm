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
scene.sid_settings.denoiser_type = 'SID TEMPORAL'
scene.sid_settings.inputdir = path.dirname(bpy.data.filepath)

bpy.context.scene.frame_start = int(argv[0])
bpy.context.scene.frame_end = int(argv[1])

bpy.ops.object.superimagedenoisetemporal_bg()