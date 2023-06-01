import bpy
from pathlib import Path
import json
import os
import socket
import threading
import bpy
import os
from math import radians
import bpy.props
from mathutils import Matrix, Vector, Euler, Quaternion
from .io_import_scene_unreal_psa_psk_280 import pskimport

bl_info = {
    "name": "Valorant Porting Blender Octane",
    "author": "Half, BK, Zain",
    "version": (1, 0, 0),
    "blender": (3, 0, 0),
    "description": "Blender Server for Valorant Porting",
    "category": "Import",
}

global import_assets_root
global import_settings
global import_data

global server
global MAIN_SHADER
global INNER_SHADER

class Log:
    INFO = u"\u001b[36m"
    WARNING = u"\u001b[31m"
    ERROR = u"\u001b[33m"
    RESET = u"\u001b[0m"

    @staticmethod
    def information(message):
        print(f"{Log.INFO}[INFO] {Log.RESET}{message}")

    @staticmethod
    def warning(message):
        print(f"{Log.WARNING}[WARN] {Log.RESET}{message}")
        
    @staticmethod
    def error(message):
        print(f"{Log.WARNING}[ERROR] {Log.RESET}{message}")

    
class Receiver(threading.Thread):

    def __init__(self, event):
        threading.Thread.__init__(self, daemon=True)
        self.event = event
        self.data = None
        self.socket_server = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.keep_alive = True

    def run(self):
        host, port = 'localhost', 24283
        self.socket_server.bind((host, port))
        self.socket_server.settimeout(3.0)
        Log.information(f"ValorantPorting Server Listening at {host}:{port}")

        while self.keep_alive:
            try:
                data_string = ""
                while True:
                    info = self.socket_server.recvfrom(4096)
                    if data := info[0].decode('utf-8'):
                        if data == "MessageFinished":
                            break
                        data_string += data
                self.event.set()
                self.data = json.loads(data_string)
                           
            except OSError as e:
                pass
            except EOFError as e:
                Log.error(e)
            except zlib.error as e:
                Log.error(e)
            except json.JSONDecodeError as e:
                Log.error(e) 

    def stop(self):
        self.keep_alive = False
        self.socket_server.close()
        Log.information("ValorantPorting Server Closed")

def import_mesh(path: str) -> bpy.types.Object:
    path = path[1:] if path.startswith("/") else path
    mesh_path = os.path.join(import_assets_root, path.split(".")[0] + "_LOD0")

    if os.path.exists(mesh_path + ".psk"):
        mesh_path += ".psk"
        pskimport(
        mesh_path,
        bReorientBones = import_settings.get("ReorientBones"),
        bScaleDown = True,
        bToSRGB = False)

        return bpy.context.active_object
    
    if os.path.exists(mesh_path + ".pskx"):
        mesh_path += ".pskx"
        pskimport(
        mesh_path,
        bScaleDown = True,
        bToSRGB = False)
        return bpy.context.active_object
    else:
        return None

def import_texture(path: str) -> bpy.types.Image:
    path, name = path.split(".")
    if existing := bpy.data.images.get(name):
        return existing

    path = path[1:] if path.startswith("/") else path
    texture_path = os.path.join(import_assets_root, path + ".png")

    if not os.path.exists(texture_path):
        return None

    return bpy.data.images.load(texture_path, check_existing=True)


def import_material(target_slot: bpy.types.MaterialSlot, material_data, mat_type):
    material_name = material_data.get("MaterialName")
    if (existing := bpy.data.materials.get(material_name)) and existing.use_nodes is True:  # assume default psk mat
        target_slot.material = existing
        return
    target_material = target_slot.material
    if target_material.name.casefold() != material_name.casefold():
        target_material = target_material.copy()
        target_material.name = material_name
        target_slot.material = target_material
    target_material.use_nodes = True

    nodes = target_material.node_tree.nodes
    nodes.clear()
    links = target_material.node_tree.links
    links.clear()

    #fix parent & eye material
    parent_name = material_data.get("ParentName")
    if parent_name  == None:
        parent_name = material_name
    #if "Eye" in parent_name:
    #    target_material.blend_method = 'BLEND'

    output_node = nodes.new(type="ShaderNodeOutputMaterial")
    output_node.location = (200, 0)

    main_shader_node = nodes.new(type="ShaderNodeGroup")
    MAIN_SHADER = main_shader_node
    main_shader_node.name = parent_name
    parent_node_exists = True

    if bpy.data.node_groups.get(parent_name) != None:
        main_shader_node.node_tree = bpy.data.node_groups.get(main_shader_node.name).copy()
        #assign this so group input stays consistent
        group_inputs = main_shader_node.inputs
    else:
        parent_node_exists = False

        new_shader_internals = bpy.data.node_groups.new(parent_name, 'ShaderNodeTree')
        main_shader_node.node_tree = new_shader_internals
        # create group input
        group_inputs = new_shader_internals.nodes.new("NodeGroupInput")
        group_inputs.location = (-350,0)
        # create group output
        group_outputs = new_shader_internals.nodes.new("NodeGroupOutput")
        group_outputs.location = (600,0)
        #create imported inner goup
        imported_shader_node = new_shader_internals.nodes.new(type="ShaderNodeGroup")
        imported_shader_node.name = "1P_Weapon_Mat_Base_V5"
        if mat_type == "Character":
            imported_shader_node.name = "3P_Character_Mat_V5"
        imported_shader_node.node_tree = bpy.data.node_groups.get(imported_shader_node.name)

        #create output on outer group
        new_shader_internals.outputs.new("NodeSocketShader", "BSDF")

        #link outer group output to imported inner group's input
        for output in group_inputs.outputs:
            if imported_shader_node.inputs.get(output.name) != None:
               new_shader_internals.links.new(output, imported_shader_node.node_tree.inputs.get(output.name))
        new_shader_internals.links.new(imported_shader_node.outputs[0], group_outputs.inputs[0])
        
    #link outer group's outputs to shader output
    links.new(main_shader_node.outputs[0], output_node.inputs[0])


    def texture_parameter(data):
        name = data.get("Name")
        value = data.get("Value")
        tex_image_node: bpy.types.Node
        tex_image_node = nodes.new(type="OctaneRGBImage")
        if (image := import_texture(value)) is None:
            return
        tex_image_node.image = image
        #tex_image_node.image.alpha_mode = 'CHANNEL_PACKED'
        #tex_image_node.hide = True
        #tex_image_node.image.colorspace_settings.name = 'Linear'
        tex_image_node.inputs[2].default_value = 1.0
        if name == 'Albedo': tex_image_node.inputs[2].default_value = 2.2
        #tex_image_node.location = location

        #if 'decal' in name.lower():
        #    uv_node = nodes.new(type="ShaderNodeUVMap")
        #    uv_node.uv_map = 'EXTRAUVS0'
        #    links.new(uv_node.outputs[0], tex_image_node.inputs[0])

        if name in MAIN_SHADER.inputs:
            if name == 'Decal Mask Texture':
                links.new(tex_image_node.outputs[1], main_shader_node.inputs[name])
            else:
                links.new(tex_image_node.outputs[0], main_shader_node.inputs[name])
        else:
            MAIN_SHADER.node_tree.inputs.new("NodeSocketColor", name)
            if name == 'Decal Mask Texture':
                links.new(tex_image_node.outputs[1], MAIN_SHADER.inputs[name])
            else:
                links.new(tex_image_node.outputs[0], MAIN_SHADER.inputs[name])

            print(f"Created & connected Texture node with name {name}")

    def scalar_parameter(data):
        name = data.get("Name")
        value = data.get("Value")
        scalar_node = nodes.new(type="OctaneGreyscaleColor")
        scalar_node.label = name
        scalar_node.a_value = value

        if MAIN_SHADER.inputs.get(name) is not None and MAIN_SHADER.inputs[name].type == "VALUE":
            links.new(scalar_node.outputs[0], MAIN_SHADER.inputs[name])
        else:
            MAIN_SHADER.node_tree.inputs.new("NodeSocketFloat", name)
            links.new(scalar_node.outputs[0], MAIN_SHADER.inputs[name])
            print(f"Created & connected Scalar node with name: {name}")

    def vector_parameter(data):
        name = data.get("Name")
        value = data.get("Value")
        color_node = nodes.new(type="OctaneRGBColor")
        color_node.label = name
        color_node.a_value = (value["R"], value["G"], value["B"])

        if MAIN_SHADER.inputs.get(name) is not None and MAIN_SHADER.inputs[name].type == "COLOR":
            links.new(color_node.outputs[0], MAIN_SHADER.inputs[name])
        else:
            MAIN_SHADER.node_tree.inputs.new("NodeSocketColor", name)
            links.new(color_node.outputs[0], MAIN_SHADER.inputs[name])
            print(f"Created & connected Vector node with name: {name}")


    for texture in material_data.get("Textures"):
        texture_parameter(texture)

    for scalar in material_data.get("Scalars"):
        scalar_parameter(scalar)

    for vector in material_data.get("Vectors"):
        vector_parameter(vector)

    #link inputs to imported inner group
    if not parent_node_exists:
        for output in group_inputs.outputs:
            if imported_shader_node.inputs.get(output.name) != None:
                new_shader_internals.links.new(output, imported_shader_node.inputs.get(output.name))





def import_shaders(shaderName):
    script_root = Path(os.path.dirname(os.path.abspath(__file__)))
    shaders_blend_file = Path(script_root.joinpath(shaderName))
    nodegroups_folder = shaders_blend_file.joinpath("NodeTree")

    with bpy.data.libraries.load(str(shaders_blend_file), link=False) as (data_from, data_to):
        for node_group in data_from.node_groups:
            if node_group not in bpy.data.node_groups.keys():
                data_to.node_groups.append(node_group)


def mesh_from_armature(armature) -> bpy.types.Mesh:
    return armature.children[0]  # only used with psk, mesh is always first child


def first(target, expr, default=None):
    if not target:
        return None
    filtered = filter(expr, target)

    return next(filtered, default)

def where(target, expr):
    if not target:
        return None
    filtered = filter(expr, target)

    return filtered

def any(target, expr):
    if not target:
        return None

    filtered = list(filter(expr, target))
    return len(filtered) > 0


def import_response(response):
    import_shaders("VALORANT_Weapon.blend")
    import_shaders("VALORANT_Agent.blend")

    global import_assets_root
    import_assets_root = response.get("AssetsRoot")

    global import_settings
    import_settings = response.get("Settings")

    global import_data
    import_data = response.get("Data")

    name = import_data.get("Name")
    import_type = import_data.get("Type")

    Log.information(f"Received Import for {import_type}: {name}")

    def constraint_object(child: bpy.types.Object, parent: bpy.types.Object, bone: str, loc, rot):
        constraint = child.constraints.new('CHILD_OF')
        constraint.target = parent
        if bone != None: constraint.subtarget = bone
        child.rotation_mode = 'XYZ'
        constraint.inverse_matrix = Matrix()
        if loc != None: child.location = (0.01 * loc["X"], 0.01 * loc["Y"], 0.01 * loc["Z"])
        if rot != None: child.rotation_euler = (rot["Pitch"], rot["Yaw"], rot["Roll"])
    
    imported_parts = []
    def import_part(parts):
        for part in parts:
            imported_part = import_mesh(part.get("MeshPath"))
            attachments = part.get("Attatchments")

            if imported_part is None:
                continue
            has_armature = imported_part.type == "ARMATURE"
            if has_armature:
                mesh = mesh_from_armature(imported_part)
            else:
                mesh = imported_part
            bpy.context.view_layer.objects.active = mesh

            imported_parts.append({
                "Attachments": attachments,
                "Parent": imported_part,
                "Mesh": mesh
            })

            for material in part.get("Materials"):
                index = material.get("SlotIndex")
                if len(mesh.material_slots) > index:
                    import_material(mesh.material_slots.values()[index], material, import_type)

            for override_material in part.get("OverrideMaterials"):
                index = override_material.get("SlotIndex")
                if len(mesh.material_slots) > index:
                    import_material(mesh.material_slots.values()[index], override_material, import_type)
            for style_material in part.get("StyleMaterials"):
                index = style_material.get("SlotIndex")
                if len(mesh.material_slots) > index:
                    import_material(mesh.material_slots.values()[index], style_material, import_type)        

    import_part(import_data.get("Parts"))

    #attachments
    for imported_part in imported_parts:
        attachments = imported_part.get("Attachments")
        parent_obj = imported_part.get("Parent")
        for attachment in attachments:
                child_name = attachment.get("AttatchmentName")
                child_obj = bpy.context.scene.objects[child_name]
                if child_obj.parent is not None: #AttatchmentName somehow points to mesh, get parent armature instead
                    child_obj = child_obj.parent
                if "revolver" in parent_obj.name.lower(): bone_name = "Magazine_Extra"
                if child_obj: constraint_object(child_obj, parent_obj, attachment.get("BoneName"), attachment.get("Offset"), attachment.get("Rotation"))

def register():
    import_event = threading.Event()

    global server
    server = Receiver(import_event)
    server.start()

    def handler():
        if import_event.is_set():
            import_response(server.data)
            import_event.clear()
        return 0.01

    bpy.app.timers.register(handler)

def unregister():
    server.stop()