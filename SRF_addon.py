import bpy
from bpy.types import PropertyGroup, Panel
from bpy.props import StringProperty, BoolProperty, IntProperty, FloatProperty, EnumProperty

bl_info = {
    "name": "Super Render Farm",
    "author": "PigeonTools - Crafto 1337",
    "version": (0, 1),
    "blender": (2, 93, 0),
    "description": "Setup the render farm directy from Blender without having to start and setting it up manually",
    "warning": "",
    "wiki_url": "",
    "category": "Render",
}


class properties(PropertyGroup):
    string: StringProperty(
        name="String",
        description="Test",
        default=""
    )

    bool: BoolProperty(
        name="Bool",
        description="Test",
        default=False
    )

    int: IntProperty(
        name="Int",
        description="Test",
        default=0,
        min=0,
        max=10
    )

    float: FloatProperty(
        name="Float",
        description="Test",
        default=0.0,
        min=0.0,
        max=10.0
    )

    enum: EnumProperty(
        name="Enum",
        description="",
        items=[
            ('OP1', "Test", "")
        ],
        # update=LoadPreset
    )

    #-----  -----#

    render_time_test: BoolProperty(
        name="Test Render Time",
        description="Render one test frame to approximate the render time per frame",
        default=False
    )

    file_format: EnumProperty(
        name="File Format",
        description="Because you have a video format selected, you now have the option to change that.",
        items=[
            ('PNG', "PNG", ""),
            ('BMP', "BMP", ""),
            #('AVIJPEG', "AVIJPEG", ""),
            #('AVIRAW', "AVIRAW", ""),
            #('IRIZ', "IRIZ", ""),
            ('IRIS', "Iris", ""),
            ('JPEG', "JPEG", ""),
            ('RAWTGA', "Targa RAW", ""),
            ('TGA', "Targa", ""),

            ('WEBP', "WebP", "Experimental!"),
            ('JP2', "JPEG 2000", "Experimental!"),
            #('DDS', "DDS", "Experimental!"),
            ('DPX', "DPX", "Experimental!"),
            ('CINEON', "Cineon", "Experimental!"),
            #('MPEG', "MPEG", "Experimental!"),
            ('OPEN_EXR_MULTILAYER', "OpenEXR Multilayer", "Experimental!"),
            ('OPEN_EXR', "OpenEXR", "Experimental!"),
            ('TIFF', "TIFF", "Experimental!"),
            ('HDR', "Radiance HDR", "Experimental!"),
        ],

        #
        # update=LoadPreset
    )

    video: BoolProperty(
        name="Generate Video",
        description="",
        default=False
    )

    fps: IntProperty(
        name="Video FPS",
        description="",
        default=24,
        min=1,
        #max = 10
    )

    vrc: EnumProperty(
        name="Video Rate Control",
        description="",
        items=[
            ('CBR', "CBR", "Constant Bitrate"),
            ('CRF', "CRF", "Constant Quality")
        ],
        # update=LoadPreset
    )

    vrc_value: IntProperty(
        name="Video Rate Control Value",
        description="Test",
        default=0,
        min=0,
        #max = 10
    )

    resize: BoolProperty(
        name="Resize the video",
        description="",
        default=False
    )

    res_x: IntProperty(
        name="New video width",
        description="Test",
        default=1920,
        min=1,
        #max = 10
    )

    res_y: IntProperty(
        name="New video height",
        description="Test",
        default=1080,
        min=1,
        #max = 10
    )


class render_button(bpy.types.Operator):
    bl_idname = "object.render"
    bl_label = "Render with SRF"

    @classmethod
    def poll(cls, context):
        return context.active_object is not None

    def execute(self, context):
        render_srf(context)
        return {'FINISHED'}


def render_srf(context):
    scene = context.scene
    props = scene.properties

    import json
    import sys
    import os
    import time
    import subprocess
    from subprocess import CREATE_NEW_CONSOLE

    jO = {
        "VER": bpy.app.version_string,
        "FS": bpy.context.scene.frame_start,
        "FE": bpy.context.scene.frame_end,
        "RE": bpy.context.scene.render.engine,
        "FF": bpy.context.scene.render.image_settings.file_format,
        "RT": 0
    }

    if props.render_time_test:
        startTime = time.time()
        bpy.ops.render.render()
        jO["RT"] = time.time() - startTime

    if jO["FF"] in ["AVI_JPEG", "AVI_RAW", "FFMPEG"]:
        jO["FF"] = props.file_format

    jO["V"] = props.video

    if jO["V"]:
        jO["FPS"] = bpy.context.scene.render.fps  # props.fps
        jO["VRC"] = props.vrc
        jO["VRCV"] = props.vrc_value

        jO["R"] = props.resize

        if jO["R"]:
            jO["RESX"] = props.res_x
            jO["RESY"] = props.res_y

    jS = json.dumps(jO)

    subprocess.call(['ipconfig'], creationflags=CREATE_NEW_CONSOLE)


class srf_panel(Panel):
    bl_label = "Super Render Farm"
    bl_idname = "OBJECT_SRF_PANEL"
    bl_space_type = 'PROPERTIES'
    bl_region_type = 'WINDOW'
    bl_context = "render"

    def draw(self, context):
        layout = self.layout
        scene = context.scene
        props = scene.properties

        row = layout.row()
        row.prop(props, "render_time_test")

        if bpy.context.scene.render.image_settings.file_format in ["AVI_JPEG", "AVI_RAW", "FFMPEG"]:
            row = layout.row()
            row.prop(props, "file_format")

        row = layout.row()
        row.prop(props, "video")

        if props.video:
            # row = layout.row()
            # row.prop(props, "fps")

            row = layout.row()
            row.prop(props, "vrc")

            row = layout.row()
            row.prop(props, "vrc_value")

            row = layout.row()
            row.prop(props, "resize")

            if props.resize:
                row = layout.row()
                row.prop(props, "res_x")
                #row = layout.row()
                row.prop(props, "res_y")

        row = layout.row()
        row = layout.row()
        row = layout.row()
        row.operator("object.render")  # , text="Overwrite")


def register():
    bpy.utils.register_class(properties)
    bpy.utils.register_class(render_button)
    bpy.utils.register_class(srf_panel)

    bpy.types.Scene.properties = bpy.props.PointerProperty(type=properties)


def unregister():
    bpy.utils.register_class(properties)
    bpy.utils.unregister_class(render_button)
    bpy.utils.unregister_class(srf_panel)

    del bpy.types.Scene.properties


if __name__ == "__main__":
    register()
