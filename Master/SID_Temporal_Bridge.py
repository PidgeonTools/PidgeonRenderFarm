# Master script

import bpy
import sys
from os import path

scene = bpy.context.scene
scene.use_nodes = True
# Set SID Temporal settings
scene.sid_settings.denoiser_type = 'SID TEMPORAL'
scene.sid_settings.inputdir = path.dirname(bpy.data.filepath)

bpy.ops.object.superimagedenoisealign_bg()