# Master script

import bpy
import sys
from os import path

scene = bpy.context.scene
scene.use_nodes = True
# Set SID Temporal settings
scene.sid_settings.denoiser_type = 'SIDT'
scene.sid_settings.working_directory = path.dirname(bpy.data.filepath)

bpy.ops.superimagedenoiser.sidtdenoise()