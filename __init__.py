import bpy
from pathlib import Path
import json
import os
import random
import socket
import threading
from .io_import_scene_unreal_psa_psk_280 import pskimport

bl_info = {
    "name": "Valorant Porting",
    "author": "Half, BK",
    "version": (1, 0, 0),
    "blender": (3, 0, 0),
    "description": "Blender Server for Valorant Porting",
    "category": "Import",
}

global import_assets_root
global import_settings
global import_data

global server
global N_SHADER
class Log:
    INFO = u"\u001b[36m"
    WARNING = u"\u001b[31m"
    RESET = u"\u001b[0m"

    @staticmethod
    def information(message):
        print(f"{Log.INFO}[INFO] {Log.RESET}{message}")

    @staticmethod
    def warning(message):
        print(f"{Log.WARNING}[WARN] {Log.RESET}{message}")


class Receiver(threading.Thread):

    def __init__(self, event):
        threading.Thread.__init__(self, daemon=True)
        self.event = event
        self.data = None
        self.socket_server = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.keep_alive = True

    def run(self):
        host, port = 'localhost', 24280
        self.socket_server.bind((host, port))
        self.socket_server.settimeout(1.0)
        Log.information(f"ValorantPorting Server Listening at {host}:{port}")

        while self.keep_alive:
            try:
                data_string = ""
                while True:
                    info = self.socket_server.recvfrom(4096)
                    if data := info[0].decode('utf-8'):
                        if data == "FPMessageFinished":
                            break
                        data_string += data
                self.event.set()
                self.data = json.loads(data_string)
            except OSError:
                pass

    def stop(self):
        self.keep_alive = False
        self.socket_server.close()
        Log.information("ValorantPorting Server Closed")


# Name, Slot, Location, *Linear
texture_mappings = {
    ("Diffuse (SRGB) texture", 1, (-300, -75)),
    ("MRAE (SRGB) texture", 10, (-300, -75)),
    ("MRS (SRGB) texture", 6, (-300, -125)),
    ("AEM (SRGB) texture", 2, (-300, -125)),
    ("Normal (SRGB) texture", 19, (-300, -175)),
}

# Name, Slot
scalar_mappings = {
    ("RoughnessMin", 3),
    ("Roughness Min", 3),
    ("SpecRoughnessMin", 3),

    ("RoughnessMax", 4),
    ("Roughness Max", 4),
    ("SpecRoughnessMax", 4),

    ("emissive mult", 13),
    ("TH_StaticEmissiveMult", 13),
    ("Emissive", 13),

    ("Emissive_BrightnessMin", 14),

    ("Emissive_BrightnessMax", 15),

    ("Emissive Fres EX", 16),
    ("EmissiveFresnelExp", 16),
}

# Name, Slot, *Alpha
vector_mappings = {
    ("Skin Undercolor", 7, 11)
}
shaders_v = [
            "VALORANT_Agent",
            "1P_Weapon_Mat_Base_V5",
            "FP Mapping"
        ]


def import_mesh(path: str) -> bpy.types.Object:
    path = path[1:] if path.startswith("/") else path
    mesh_path = os.path.join(import_assets_root, path.split(".")[0])

    if os.path.exists(mesh_path + ".psk"):
        mesh_path += ".psk"
    if os.path.exists(mesh_path + ".pskx"):
        mesh_path += ".pskx"

    if not pskimport(mesh_path):
        return None

    return bpy.context.active_object


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
    import_shaders("VALORANT_Weapon.blend")
    import_shaders("VALORANT_Agent.blend")
    output_node = nodes.new(type="ShaderNodeOutputMaterial")
    output_node.location = (200, 0)

    shader_node = nodes.new(type="ShaderNodeGroup")
    N_SHADER = shader_node
    shader_node.name = "1P_Weapon_Mat_Base_V5"
    if mat_type == "Character":
        shader_node.name = "VALORANT_Agent"
    shader_node.node_tree = bpy.data.node_groups.get(shader_node.name)

    links.new(shader_node.outputs[0], output_node.inputs[0])

    def texture_parameter(data):
        name = data.get("Name")
        value = data.get("Value")
        tex_image_node: bpy.types.Node
        tex_image_node = nodes.new(type="ShaderNodeTexImage")
        if (image := import_texture(value)) is None:
            return
        tex_image_node.image = image
        tex_image_node.image.alpha_mode = 'CHANNEL_PACKED'
        tex_image_node.hide = True
        #tex_image_node.location = location
        if name in N_SHADER.inputs:
            links.new(tex_image_node.outputs[0], shader_node.inputs[name])
        else:
            print(f"No Texture node with this name {name}")

    def scalar_parameter(data):
        name = data.get("Name")
        value = data.get("Value")

        if name in N_SHADER.inputs:
            shader_node.inputs[name].default_value = value
        else:
            print(f"No Scalar node with this name: {name}")

    def vector_parameter(data):
        name = data.get("Name")
        value = data.get("Value")
        if name in N_SHADER.inputs:
            shader_node.inputs[name].default_value = (value["R"], value["G"], value["B"], 1)
        else:
            print(f"No Vector node with this name: {name}")


    for texture in material_data.get("Textures"):
        texture_parameter(texture)

    for scalar in material_data.get("Scalars"):
        scalar_parameter(scalar)

    for vector in material_data.get("Vectors"):
        vector_parameter(vector)

def AttachToObject(entire_loop):
    for entire_object in entire_loop:
        if type(entire_object) != dict:
            return
        all_attachs = entire_object.get("Attatchments")
        for idex,attachs in enumerate(all_attachs):
            target_obj = bpy.data.objects[entire_object.get("MeshName")]
            child_obj = bpy.data.objects[attachs.get("AttatchmentName")]
            constraint = child_obj.constraints.new(type='CHILD_OF')
            constraint.name = str(idex)
            constraint.target = target_obj
            bone_name = attachs.get("BoneName")
            if "revolver" in target_obj.name.lower():
                bone_name = "Magazine_Extra"
            constraint.subtarget = bone_name
            try:
                bpy.context.view_layer.objects.active = child_obj
                bpy.ops.constraint.childof_clear_inverse(constraint=constraint.name, owner='OBJECT')

            except:
                continue



def clear_and_set_inverse_object_constraint_child_of(object_name, constraint_name):

    previous_mode = set_mode(active_object_name=object_name, mode='OBJECT', configure_scene_for_basic_ops=False)

    current_object = bpy.data.objects[object_name]

    override_context = bpy.context.copy()
    override_context['constraint'] = current_object.constraints[constraint_name]

    bpy.ops.constraint.childof_clear_inverse(override_context, constraint=constraint_name, owner='OBJECT')

    bpy.ops.object.mode_set(mode=previous_mode)
def import_shaders(shaderName):
    script_root = Path(os.path.dirname(os.path.abspath(__file__)))
    shaders_blend_file = Path(script_root.joinpath(shaderName))
    nodegroups_folder = shaders_blend_file.joinpath("NodeTree")
    for shader in shaders_v:
        if shader not in bpy.data.node_groups.keys():
            bpy.ops.wm.append(filename=shader, directory=nodegroups_folder.__str__())

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
    global import_assets_root
    import_assets_root = response.get("AssetsRoot")

    global import_settings
    import_settings = response.get("Settings")

    global import_data
    import_data = response.get("Data")

    name = import_data.get("Name")
    import_type = import_data.get("Type")

    Log.information(f"Received Import for {import_type}: {name}")

    imported_parts = []
    def import_part(parts):
        for part in parts:
            part_type = part.get("Part")
            #if any(imported_parts, lambda x: False if x is None else x.get("Part") == part_type):
                #continue

            if (imported_part := import_mesh(part.get("MeshPath"))) is None:
                continue

            has_armature = imported_part.type == "ARMATURE"
            if has_armature:
                mesh = mesh_from_armature(imported_part)
            else:
                #prob here for attach
                mesh = imported_part
                AttachToObject(part)
            bpy.context.view_layer.objects.active = mesh

            imported_parts.append({
                "Part": part_type,
                "Armature": imported_part if has_armature else None,
                "Mesh": mesh
            })

            for material in part.get("Materials"):
                index = material.get("SlotIndex")
                import_material(mesh.material_slots.values()[index], material,import_type)

            for override_material in part.get("OverrideMaterials"):
                index = override_material.get("SlotIndex")
                import_material(mesh.material_slots.values()[index], override_material,import_type)
    import_part(import_data.get("StyleParts"))
    import_part(import_data.get("Parts"))
    AttachToObject(import_data.get("Parts"))



    for imported_part in imported_parts:
        mesh = imported_part.get("Mesh")
        for style_material in import_data.get("StyleMaterials"):
            if style_material.get("SlotIndex") < len(mesh.material_slots):
                slot = mesh.material_slots[style_material.get("SlotIndex")]
                import_material(slot, style_material,import_type)





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