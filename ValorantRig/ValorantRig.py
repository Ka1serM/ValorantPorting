import bpy
import os
from math import radians
from enum import Enum
import bpy.props

from mathutils import Matrix, Vector, Euler, Quaternion


bl_info = {
    "name": "Valorant Rig",
    "author": "Zain, Tasty for script base",
    "version": (1, 0, 8),
    "blender": (3, 0, 0),
    "description": "Valorant Rig",
    "category": "Import",
}


#original main bones
root = 'Root'
hips = 'Splitter'
pelvis = 'Pelvis'
spine_01 = 'Spine1'
spine_02 = 'Spine2'
spine_03 = 'Spine3'
spine_04 = 'Spine4'
head = 'Head'
neck_01 = 'Collar'
neck_02 = 'Neck'
clavicle_r = 'R_Clavicle'
clavicle_l = 'L_Clavicle'
shoulder_r = 'R_Shoulder'
shoulder_l = 'L_Shoulder'
elbow_r = 'R_Elbow'
elbow_l = 'L_Elbow'
hand_r = 'R_Hand'
hand_l = 'L_Hand'
hip_r = 'R_Hip'
hip_l = 'L_Hip'
knee_r = 'R_Knee'
knee_l = 'L_Knee'
foot_r = 'R_Foot'
foot_l = 'L_Foot'
toe_r = 'R_Toe'
toe_l = 'L_Toe'
eye_r = 'R_Eyeball'
eye_l = 'L_Eyeball'

#pole & target for IK
hand_pole_r = 'hand_pole_r'
hand_pole_l = 'hand_pole_l'

foot_pole_r = 'foot_pole_r'
foot_pole_l = 'foot_pole_l'

# IK chain arms
shoulder_r_ik = 'shoulder_r_ik'
elbow_r_ik = 'elbow_r_ik'
hand_r_ik = 'hand_r_ik'

shoulder_l_ik = 'shoulder_l_ik'
elbow_l_ik = 'elbow_l_ik'
hand_l_ik = 'hand_l_ik'

#IK chain feet
hip_r_ik = 'hip_r_ik'
knee_r_ik = 'knee_r_ik'
foot_r_ik = 'foot_r_ik'

hip_l_ik = 'hip_l_ik'
knee_l_ik = 'knee_l_ik'
foot_l_ik = 'foot_l_ik'

#eye controls
eye_mid_control = 'eye_mid_control'
eye_r_control = 'eye_r_control'
eye_l_control = 'eye_l_control'

scale = 100
scale_fix = 1

main_layer_bones = [
    root,
    hips,
    pelvis,
    spine_01,
    spine_02,
    spine_03,
    spine_04,
    neck_01,
    neck_02,
    head,
    clavicle_r,
    clavicle_l,
    shoulder_r,
    shoulder_l,
    elbow_r,
    elbow_l,
    hand_r,
    hand_l,
    hip_r,
    hip_l,
    knee_r,
    knee_l,
    foot_r,
    foot_l,
    toe_r,
    toe_l,
    eye_r,
    eye_l
    ]

new_bones = [
    hand_pole_r,
    hand_pole_l,
    foot_pole_r, 
    foot_pole_l, 
    shoulder_r_ik, 
    elbow_r_ik, 
    hand_r_ik,
    shoulder_l_ik,
    elbow_l_ik,
    hand_l_ik,
    hip_r_ik,
    knee_r_ik,
    foot_r_ik,
    hip_l_ik,
    knee_l_ik,
    foot_l_ik,
    eye_mid_control,
    eye_r_control,
    eye_l_control
    ]

def apply_rig(master_skeleton: bpy.types.Armature):

    addon_dir = os.path.dirname(os.path.splitext(__file__)[0])
    with bpy.data.libraries.load(os.path.join(addon_dir, "Data.blend")) as (data_from, data_to):

        for obj in data_from.objects:
            if not bpy.data.objects.get(obj):
                data_to.objects.append(obj)

    hidden_group = master_skeleton.pose.bone_groups.new(name='HiddenGroup')
    hidden_group.color_set = 'THEME05'
    ik_group = master_skeleton.pose.bone_groups.new(name='IKGroup')
    ik_group.color_set = 'THEME04'
    pole_group = master_skeleton.pose.bone_groups.new(name='PoleGroup')
    pole_group.color_set = 'THEME07'
    twist_group = master_skeleton.pose.bone_groups.new(name='TwistGroup')
    twist_group.color_set = 'THEME09'
    face_group = master_skeleton.pose.bone_groups.new(name='FaceGroup')
    face_group.color_set = 'THEME01'
    extra_group = master_skeleton.pose.bone_groups.new(name='ExtraGroup')
    extra_group.color_set = 'THEME10'
    master_skeleton.pose.bone_groups[0].color_set = "THEME05"
    master_skeleton.pose.bone_groups[1].color_set = "THEME09"
  
    bpy.ops.object.mode_set(mode='EDIT')
    edit_bones = master_skeleton.data.edit_bones


    # name, head, tail, roll
    ik_rig_bones = [
        #create ik bones pole & target
        (hand_pole_r, edit_bones.get(elbow_r).head + Vector((-0.33, 0, 0))*scale*scale_fix, edit_bones.get(elbow_r).head + Vector((-0.33, 0, -0.05))*scale*scale_fix, 0),
        (hand_pole_l, edit_bones.get(elbow_l).head + Vector((-0.33, 0, 0))*scale*scale_fix, edit_bones.get(elbow_l).head + Vector((-0.33, 0, -0.05))*scale*scale_fix, 0),
        (foot_pole_r, edit_bones.get(knee_r).head + Vector((0.33, 0, 0))*scale*scale_fix, edit_bones.get(knee_r).head + Vector((0.33, 0, -0.05))*scale*scale_fix, 0),
        (foot_pole_l, edit_bones.get(knee_l).head + Vector((0.33, 0, 0))*scale*scale_fix, edit_bones.get(knee_l).head + Vector((0.33, 0, -0.05))*scale*scale_fix, 0),

        (eye_mid_control, edit_bones.get(head).head + Vector((0.675, 0, 0))*scale*scale_fix, edit_bones.get(head).head + Vector((0.7, 0, 0))*scale*scale_fix, 0)
        ]

    for new_bone in ik_rig_bones:
        edit_bone: bpy.types.EditBone = edit_bones.new(new_bone[0])
        edit_bone.head = new_bone[1]
        edit_bone.tail = new_bone[2]
        edit_bone.roll = new_bone[3]
        edit_bone.parent = edit_bones.get("Root")

    # name, head, tail, roll, parent
    # relies on other rig bones for creation
    dependent_rig_bones = [
        #IK bone chain hands
        (shoulder_r_ik, edit_bones.get(shoulder_r).head, edit_bones.get(shoulder_r).tail, edit_bones.get(shoulder_r).roll, clavicle_r),
        (shoulder_l_ik, edit_bones.get(shoulder_l).head, edit_bones.get(shoulder_l).tail, edit_bones.get(shoulder_l).roll, clavicle_l),
        (elbow_r_ik, edit_bones.get(elbow_r).head, edit_bones.get(elbow_r).tail, edit_bones.get(elbow_r).roll, shoulder_r_ik),
        (elbow_l_ik, edit_bones.get(elbow_l).head, edit_bones.get(elbow_l).tail, edit_bones.get(elbow_l).roll, shoulder_l_ik),
        (hand_r_ik, edit_bones.get(hand_r).head, edit_bones.get(hand_r).tail, edit_bones.get(hand_r).roll, root),
        (hand_l_ik, edit_bones.get(hand_l).head, edit_bones.get(hand_l).tail, edit_bones.get(hand_l).roll, root),
       
        #IK bone chain feet
        (hip_r_ik, edit_bones.get(hip_r).head, edit_bones.get(hip_r).tail, edit_bones.get(hip_r).roll, pelvis),
        (hip_l_ik, edit_bones.get(hip_l).head, edit_bones.get(hip_l).tail, edit_bones.get(hip_l).roll, pelvis),
        (knee_r_ik, edit_bones.get(knee_r).head, edit_bones.get(knee_r).tail, edit_bones.get(knee_r).roll, hip_r_ik),
        (knee_l_ik, edit_bones.get(knee_l).head, edit_bones.get(knee_l).tail, edit_bones.get(knee_l).roll, hip_l_ik),
        (foot_r_ik, edit_bones.get(foot_r).head, edit_bones.get(foot_r).tail, edit_bones.get(foot_r).roll, root),
        (foot_l_ik, edit_bones.get(foot_l).head, edit_bones.get(foot_l).tail, edit_bones.get(foot_l).roll, root),

        (eye_r_control, edit_bones.get(eye_mid_control).head + Vector((0, 0.0325, 0))*scale*scale_fix, edit_bones.get(eye_mid_control).tail + Vector((0, 0.0325, 0))*scale*scale_fix, 0, eye_mid_control),
        (eye_l_control, edit_bones.get(eye_mid_control).head + Vector((0, -0.0325, 0))*scale*scale_fix, edit_bones.get(eye_mid_control).tail + Vector((0, -0.0325, 0))*scale*scale_fix, 0, eye_mid_control)
    ]

    for new_bone in dependent_rig_bones:
        edit_bone: bpy.types.EditBone = edit_bones.new(new_bone[0])
        edit_bone.head = new_bone[1]
        edit_bone.tail = new_bone[2]
        edit_bone.roll = new_bone[3]
        edit_bone.parent = edit_bones.get(new_bone[4])

    # current, target
    # connect bone tail to target head, so IK works
    tail_head_connection_bones = [
        (shoulder_r_ik, elbow_r_ik),
        (shoulder_l_ik, elbow_l_ik),
        (elbow_r_ik, hand_r_ik),
        (elbow_l_ik, hand_l_ik),

        (hip_r_ik, knee_r_ik),
        (hip_l_ik, knee_l_ik),
        (knee_r_ik, foot_r_ik),
        (knee_l_ik, foot_l_ik),
    ]

    for edit_bone in tail_head_connection_bones:
        if (parent_bone := edit_bones.get(edit_bone[0])) and (child_bone := edit_bones.get(edit_bone[1])):
            parent_bone.tail = child_bone.head

    bpy.ops.object.mode_set(mode='OBJECT')

    pose_bones = master_skeleton.pose.bones

    #regular bone shape
    for pose_bone in pose_bones:
        pose_bone.custom_shape = bpy.data.objects.get('RIG_Cube')
        pose_bone.custom_shape_scale_xyz = 0.03*scale, 0.03*scale, 0.03*scale
        pose_bone.custom_shape_rotation_euler = 0, 0, 0

    # bone name, shape object name, scale, *rotation
    # bones that have custom shapes as bones instead

    custom_shape_bones = [
        (root, 'RIG_Root', 0.15*scale),
        (hips, 'RIG_Torso', 0.15*scale),
        #pelvis,
        (spine_01, 'RIG_Hips', 0.15*scale),
        (spine_02, 'RIG_Hips', 0.15*scale),
        (spine_03, 'RIG_Hips', 0.15*scale),
        (spine_04, 'RIG_Hips', 0.15*scale),
        (neck_01, 'RIG_Hips', 0.10*scale),
        (neck_02, 'RIG_Hips', 0.075*scale),
        (head, 'RIG_Hips', 0.15*scale),
        (clavicle_r, 'RIG_Shoulder', 12, (0, 0, 90)),
        (clavicle_l, 'RIG_Shoulder', 12, (0, 0, -90)),

        (toe_r, 'RIG_Toe', 0.25*scale),
        (toe_l, 'RIG_Toe', 0.25*scale, (0, 0, 180)),

        (hand_r_ik, 'RIG_Hand', 0.35*scale, (0, 0, 90)),
        (hand_l_ik, 'RIG_Hand', 0.35*scale, (0, 0, -90)),
        (foot_r_ik, 'RIG_FootR', 0.35*scale, (0, 0, 180)),
        (foot_l_ik, 'RIG_FootL', 0.35*scale, (0, 180, 0)),

        (hand_pole_r, 'RIG_Tweak', 0.2*scale),
        (hand_pole_l, 'RIG_Tweak', 0.2*scale),
        (foot_pole_r, 'RIG_Tweak', 0.2*scale),
        (foot_pole_l, 'RIG_Tweak', 0.2*scale),

        (eye_mid_control, 'RIG_EyeTrackMid', 0.75*scale),
        (eye_r_control, 'RIG_EyeTrackInd', 0.75*scale),
        (eye_l_control, 'RIG_EyeTrackInd', 0.75*scale)]

    for custom_shape_bone_data in custom_shape_bones:
        name, shape, bone_scale, *extra = custom_shape_bone_data
        if not (custom_shape_bone := pose_bones.get(name)):
            continue

        custom_shape_bone.custom_shape = bpy.data.objects.get(shape)
        custom_shape_bone.custom_shape_scale_xyz = bone_scale, bone_scale, bone_scale

        if len(extra) > 0 and (rot := extra[0]):
            custom_shape_bone.custom_shape_rotation_euler = [radians(rot[0]), radians(rot[1]), radians(rot[2])]

    sides = ['L_','R_']

    #custom shape on limbs
    limb_bones = ['Shoulder', 'Elbow', 'Hip', 'Knee', 'Hand', 'Foot']
    for limb_bone in limb_bones:
        for side in sides:
            bone_name = side + limb_bone
            bone = pose_bones.get(bone_name)
            bone.bone.hide = False
            bone.custom_shape = bpy.data.objects.get('RIG_Index')
            bone.custom_shape_scale_xyz = 1.5*scale, 1.5*scale, 1.5*scale
            bone.custom_shape_rotation_euler = radians(0),radians(0), radians(90)

    #custom shape on hands
    hand_bones = ['Thumb','Index','Middle','Ring','Pinky']
    for hand_bone in hand_bones:
        for side in sides:
            i = 1
            while i <= 3:
                bone_name = side + hand_bone + str(i)
                custom_shape_bone = pose_bones.get(bone_name)
                custom_shape_bone.custom_shape = bpy.data.objects.get('RIG_Index')
                custom_shape_bone.custom_shape_scale_xyz = 0.2*scale, 0.2*scale, 0.2*scale
                custom_shape_bone.custom_shape_rotation_euler = radians(0),radians(0), radians(90)
                
                i += 1

    #face shapes
    head_bone = pose_bones.get(head)
    children = head_bone.children_recursive
    for child in children:
        child.custom_shape = bpy.data.objects.get('RIG_FaceBone')
        child.custom_shape_rotation_euler = radians(90),radians(0), radians(0)
        child.custom_shape_scale_xyz = 0.01*scale, 0.01*scale, 0.01*scale

    # other tweaks by enumeration of pose bones

    for pose_bone in pose_bones:
        if pose_bone.bone_group is None:
            pose_bone.bone_group = extra_group

        if not pose_bone.parent: # root
            pose_bone.use_custom_shape_bone_size = False
            continue

        if '_Twst' in pose_bone.name or '_Twist' in pose_bone.name or '_Ndl' in pose_bone.name:
            pose_bone.custom_shape_scale_xyz = 0.1*scale, 0.1*scale, 0.1*scale
            pose_bone.use_custom_shape_bone_size = True
            pose_bone.custom_shape = bpy.data.objects.get('RIG_Tweak')
            pose_bone.bone_group = twist_group

        if '_ik' in pose_bone.name or '_target' in pose_bone.name or '_pole' in pose_bone.name:
            pose_bone.bone_group = ik_group

        ik_bones = [shoulder_r_ik, shoulder_l_ik, elbow_r_ik, elbow_l_ik, hip_r_ik, hip_l_ik, knee_r_ik, knee_l_ik]
        for bone in ik_bones:
            pose_bones.get(bone).custom_shape = None

        if any(["eyelid", "eye_lid_"], lambda x: x.casefold() in pose_bone.name.casefold()):
            pose_bone.bone_group = face_group
            continue

        if pose_bone.name.casefold().endswith("_eye"):
            pose_bone.bone_group = extra_group
            continue

    defined_group_bones = {

        foot_r: hidden_group,
        foot_l: hidden_group
    }

    for bone_name, group in defined_group_bones.items():
        if bone := pose_bones.get(bone_name):
            bone.bone_group = group

    # bone, target, weight
    # copy rotation modifier added to bones, so FK bones follow IK chain

    copy_rotation_bones = [

        #IK chain arms
        (shoulder_r, shoulder_r_ik, 1.0),
        (shoulder_l, shoulder_l_ik, 1.0), 
        (elbow_r, elbow_r_ik, 1.0),
        (elbow_l, elbow_l_ik, 1.0),       
        (hand_r, hand_r_ik, 1.0),
        (hand_l, hand_l_ik, 1.0),

        #IK chain feet
        (hip_r, hip_r_ik, 1.0),
        (hip_l, hip_l_ik, 1.0), 
        (knee_r, knee_r_ik, 1.0),
        (knee_l, knee_l_ik, 1.0),
        (foot_r, foot_r_ik, 1.0),
        (foot_l, foot_l_ik, 1.0),

    ]

    for bone_data in copy_rotation_bones:
        current, target, weight = bone_data
        if not (pose_bone := pose_bones.get(current)):
            continue

        con = pose_bone.constraints.new('COPY_ROTATION')
        con.target = master_skeleton
        con.subtarget = target
        con.influence = weight
        con.name = 'copy_IK'

        if foot_r_ik in target or foot_l_ik in target or hand_r_ik in target or hand_l_ik in target:
            con.target_space = 'WORLD'
            con.owner_space = 'WORLD'
        else:
            con.target_space = 'LOCAL_OWNER_ORIENT'
            con.owner_space = 'LOCAL'

    # target, ik, pole
    ik_bones = [
        (elbow_r_ik, hand_r_ik, hand_pole_r, '105.5'),
        (elbow_l_ik, hand_l_ik, hand_pole_l, '-100'),
        (knee_r_ik, foot_r_ik, foot_pole_r, '-84.5'),
        (knee_l_ik, foot_l_ik, foot_pole_l, '95.3'),
    ]

    for ik_bone_data in ik_bones:
        target, ik, pole, angle = ik_bone_data
        con = pose_bones.get(target).constraints.new('IK')
        con.target = master_skeleton
        con.subtarget = ik
        con.pole_target = master_skeleton
        con.pole_subtarget = pole
        con.pole_angle = radians(float(angle))
        con.chain_count = 2

    # only gonna be the head but whatever
    track_bones = [
        (eye_mid_control, 'Head', 0.285)
    ]

    for track_bone_data in track_bones:
        current, target, head_tail = track_bone_data
        if not (pose_bone := pose_bones.get(current)):
            continue

        con = pose_bone.constraints.new('TRACK_TO')
        con.target = master_skeleton
        con.subtarget = target
        con.head_tail = head_tail
        con.track_axis = 'TRACK_Y'
        con.up_axis = 'UP_Z'

    # bone, target, ignore axis', axis
    lock_track_bones = [
        (eye_r, eye_r_control, ['Y']),
        (eye_l, eye_l_control, ['Y']),
        ('FACIAL_R_Eye', eye_r_control, ['Y']),
        ('FACIAL_L_Eye', eye_l_control, ['Y']),
    ]

    for lock_track_bone_data in lock_track_bones:
        current, target, ignored = lock_track_bone_data
        if not (pose_bone := pose_bones.get(current)):
            continue

        for axis in ['X', 'Y', 'Z']:
            if axis in ignored:
                continue
            con = pose_bone.constraints.new('LOCKED_TRACK')
            con.target = master_skeleton
            con.subtarget = target
            con.track_axis = 'TRACK_Y'
            con.lock_axis = 'LOCK_' + axis

    bones = master_skeleton.data.bones

    # name, layer index
    # maps bone group to layer index
    bone_groups_to_layer_index = {

        'MainGroup': 1,
        'ExtraGroup': 2,
        'IKGroup': 3,
        'PoleGroup': 4,
        'TwistGroup': 5,
        'FaceGroup': 6,
        'HiddenGroup': 23,
    }
    #Disable main layer by default

    for bone in bones:
        if bone.name in main_layer_bones:
            bone.layers[1] = True
            continue

        if "eye" in bone.name.casefold():
            bone.layers[6] = True
            continue

        #if group := pose_bones.get(bone.name).bone_group:
        #    if group.name in ['Unused bones', 'No children']:
        #        bone.layers[5] = True
        #        continue
        #    index = bone_groups_to_layer_index[group.name]
        #    bone.layers[index] = True

def constraint_object(child: bpy.types.Object, parent: bpy.types.Object, bone: str, rot=[radians(0), radians(90), radians(0)]):
    constraint = child.constraints.new('CHILD_OF')
    constraint.target = parent
    constraint.subtarget = bone
    child.rotation_mode = 'XYZ'
    child.rotation_euler = rot
    constraint.inverse_matrix = Matrix()

def first(target, expr, default=None):
    if not target:
        return None
    filtered = filter(expr, target)

    return next(filtered, default)

def where(target, expr):
    if not target:
        return None
    filtered = filter(expr, target)

    return list(filtered)

def any(target, expr):
    if not target:
        return None

    filtered = list(filter(expr, target))
    return len(filtered) > 0

def create_collection(name):
    if name in bpy.context.view_layer.layer_collection.children:
        bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children.get(name)
        return
    bpy.ops.object.select_all(action='DESELECT')
    
    new_collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(new_collection)
    bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children.get(new_collection.name)

def message_box(message = "", title = "Message Box", icon = 'INFO'):

    def draw(self, context):
        self.layout.label(text=message)

    bpy.context.window_manager.popup_menu(draw, title = title, icon = icon)

class ValorantRigPanel(bpy.types.Panel):
    bl_category = "Valorant Rig"
    bl_description = "Valorant Utilities"
    bl_label = "Valorant Rig"
    bl_region_type = 'UI'
    bl_space_type = 'VIEW_3D'

    def draw(self, context):
        layout = self.layout
        
        box = layout.box()
        box.label(text="Rigging", icon="OUTLINER_OB_ARMATURE")
        box.row().operator("object.apply", icon='ARMATURE_DATA')
        box.row().operator("object.remove", icon='ARMATURE_DATA')        
               
class ValorantRigApply(bpy.types.Operator):
    bl_idname = "object.apply"
    bl_label = "Apply Valorant Rig"

    def execute(self, context):
        active = bpy.context.active_object
        if active.type == "ARMATURE":
            apply_rig(active)
        return {'FINISHED'}

def ik_changed(self, context):
    props = context.scene.my_properties
    print("Slider value:", props.ik_fk)

    sides = ['L_','R_']
    limb_bones = ['Shoulder', 'Elbow', 'Hip', 'Knee', 'Hand', 'Foot']
    for limb_bone in limb_bones:
        for side in sides:
            bone_name = side + limb_bone
            armature_obj = bpy.context.active_object
            bone = armature_obj.pose.bones[bone_name]

            for constraint in bone.constraints:
                if constraint.name == "copy_IK":
                    constraint.influence = props.ik_fk

def snap_IK_to_FK(armature, FK_upper, FK_lower, FK_end, IK_eff, IK_pole):
    
    # Set IK effector matrix relative to the original FK end bone in armature space
    IK_relative_to_Fk = FK_end.bone.matrix_local.inverted() @ IK_eff.bone.matrix_local
    IK_eff.matrix = FK_end.matrix @ IK_relative_to_Fk
    bpy.context.view_layer.update()

    obj = bpy.context.object
    
    #get FK bone vectors in world space part 1
    root_joint_vec_matrix = obj.matrix_world @ FK_upper.matrix
    mid_joint_vec_matrix = obj.matrix_world @ FK_lower.matrix
    end_joint_vec_matrix = obj.matrix_world @ FK_end.matrix

    #get FK bone vectors in world space part 2
    root_joint_vec = root_joint_vec_matrix.to_translation()
    mid_joint_vec = mid_joint_vec_matrix.to_translation()
    end_joint_vec = end_joint_vec_matrix.to_translation()

    line = (end_joint_vec - root_joint_vec)
    point = (mid_joint_vec - root_joint_vec)

    scale_value = line.dot(point) / line.dot(line)
    proj_vec = line * scale_value + root_joint_vec

    root_to_mid_len = (mid_joint_vec - root_joint_vec).length
    mid_to_end_len = (end_joint_vec - mid_joint_vec).length
    total_length = (root_to_mid_len + mid_to_end_len) / 2

    pole_vec_pos = ((mid_joint_vec - proj_vec).normalized()) * total_length + mid_joint_vec

    PV_matrix = Matrix.LocRotScale(pole_vec_pos, IK_pole.matrix.to_quaternion(), None)
    IK_pole.matrix = PV_matrix

def foot_fix(armature, FK_lower, IK_pole):

    if FK_lower.name == knee_r:
        size = -0.1 * scale
    if FK_lower.name == knee_l:    
        size = 0.1 * scale
    obj = bpy.context.object
    mid_joint_vec_matrix = obj.matrix_world @ FK_lower.matrix
    mid_joint_vec = mid_joint_vec_matrix.to_translation()
    pole_vec_pos = mid_joint_vec + (FK_lower.vector * size)
    PV_matrix = Matrix.LocRotScale(pole_vec_pos, IK_pole.matrix.to_quaternion(), None)
    IK_pole.matrix = PV_matrix    


class SnapIKToFKOperator(bpy.types.Operator):
    bl_idname = 'opr.snap_ik_to_fk_operator'
    bl_label = 'Snap IK to FK'

    def execute(self, context):
        arm = bpy.context.active_object
        pose_bones = arm.pose.bones
        scene = context.scene
        props = scene.my_properties

        FK_upper_list = [shoulder_r, shoulder_l, hip_r, hip_l]
        FK_lower_list  = [elbow_r, elbow_l, knee_r, knee_l]
        FK_end_list = [hand_r, hand_l, foot_r, foot_l]
        #IK_upper_list = [shoulder_r_ik, shoulder_l_ik, hip_r_ik, hip_l_ik]
        #IK_lower_list = [elbow_r_ik, elbow_l_ik, knee_r_ik, knee_l_ik]
        IK_end_list = [hand_r_ik, hand_l_ik, foot_r_ik, foot_l_ik]
        IK_pole_list = [hand_pole_r, hand_pole_l, foot_pole_r, foot_pole_l]

        def switch(bone_name):
            if bone_name == hand_r_ik:
                return 0
            elif bone_name == hand_l_ik:
                return 1
            elif bone_name == foot_r_ik:
                return 2
            elif bone_name == foot_l_ik:
                return 3

        start_frame = props.start_frame if props.use_frame_range else -1
        end_frame = props.end_frame if props.use_frame_range else -1
        
        if start_frame < 0 or end_frame < 0:
            start_frame = bpy.context.scene.frame_current
            end_frame = start_frame + 1
        
        selected_bones = bpy.context.selected_pose_bones
        for selected_bone in selected_bones:
            for frame in range(start_frame, end_frame):
                bpy.context.scene.frame_set(frame)
                if selected_bone.name in IK_end_list:
                    index = switch(selected_bone.name)
                    snap_IK_to_FK(
                        arm,
                        pose_bones.get(FK_upper_list[index]),
                        pose_bones.get(FK_lower_list[index]),
                        pose_bones.get(FK_end_list[index]),
                        pose_bones.get(IK_end_list[index]),
                        pose_bones.get(IK_pole_list[index])
                    )
                    if props.foot_pole_fix:
                        if selected_bone.name == foot_r_ik or selected_bone.name == foot_l_ik:
                            foot_fix(arm, pose_bones.get(FK_lower_list[index]), pose_bones.get(IK_pole_list[index]))
                    if props.use_frame_range:
                        pose_bones.get(IK_end_list[index]).keyframe_insert('location', frame=frame)
                        pose_bones.get(IK_end_list[index]).keyframe_insert('rotation_quaternion', frame=frame)
                        pose_bones.get(IK_pole_list[index]).keyframe_insert('location', frame=frame)
                        pose_bones.get(IK_pole_list[index]).keyframe_insert('rotation_quaternion', frame=frame)
            
        return {'FINISHED'}

class ValorantRigRemove(bpy.types.Operator):
    bl_idname = 'object.remove'
    bl_label = 'Remove Rig'

    def execute(self, context):
        arm = bpy.context.active_object
        pose_bones = arm.pose.bones
        scene = context.scene
        props = scene.my_properties

        for pose_bone in pose_bones:
            for constraint in pose_bone.constraints:
                pose_bone.constraints.remove(constraint)
            custom_shape_obj = pose_bone.custom_shape
            if custom_shape_obj is not None:
                pose_bone.custom_shape = None

        bpy.ops.object.mode_set(mode='EDIT')
        edit_bones = arm.data.edit_bones
        for new_bone in new_bones:
            bone = edit_bones.get(new_bone)
            if (bone != None): edit_bones.remove(bone)
        bpy.ops.object.mode_set(mode='OBJECT')

        return {'FINISHED'}

class RigProperties(bpy.types.PropertyGroup):
    ik_fk: bpy.props.FloatProperty(name="IK FK Switch", description="Influence of IK vs FK", default=1.0, min=0.0, max=1.0, subtype="PERCENTAGE", update=ik_changed);
    use_frame_range: bpy.props.BoolProperty(name='Key across frame range', default=False);
    start_frame: bpy.props.IntProperty(name='Start frame', default=0);
    end_frame: bpy.props.IntProperty(name='End frame', default=250);
    foot_pole_fix: bpy.props.BoolProperty(name='Foot Pole Fix', default=True);

class RigUI(bpy.types.Panel):
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_label = "Rig UI"
    bl_idname = "VIEW3D_PT_rig_ui"
    bl_category = 'Item'
    
    @classmethod
    def poll(self, context):
        obj = context.active_object
        if context.mode != 'POSE':
            return False
        try:
            return (obj.type == 'ARMATURE')
        except (AttributeError, KeyError, TypeError):
            return False

    def draw(self, context):
        layout = self.layout
        scene = context.scene
        props = scene.my_properties
        col = layout.column()

        layout.prop(props, "ik_fk")

        row = col.row()
        row.prop(props, "use_frame_range")

        row = col.row()
        row.prop(props, "start_frame")

        row.enabled = props.use_frame_range

        row = row.row()
        row.prop(props, "end_frame")

        row.enabled = props.use_frame_range
        layout.prop(props, "foot_pole_fix")

        layout.operator('opr.snap_ik_to_fk_operator', text='Snap IK to FK')   

class RigLayers(bpy.types.Panel):
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_label = "Rig Layers"
    bl_idname = "VIEW3D_PT_rig_layers"
    bl_category = 'Item'
    
    @classmethod
    def poll(self, context):
        obj = context.active_object
        if context.mode != 'POSE':
            return False
        try:
            return (obj is not None) and (obj.type == 'ARMATURE')
        except (AttributeError, KeyError, TypeError):
            return False

    def draw(self, context):
        layout = self.layout
        scene = context.scene
        props = scene.my_properties
        col = layout.column()

        row = col.row()
        row.prop(context.active_object.data, 'layers', index=1, toggle=True, text='Main')

        row = col.row()
        row.prop(context.active_object.data, 'layers', index=2, toggle=True, text='Extra')

        row = col.row()
        row.prop(context.active_object.data, 'layers', index=3, toggle=True, text='IK')

        row = col.row()
        row.prop(context.active_object.data, 'layers', index=4, toggle=True, text='IK Poles')

        row = col.row()
        row.prop(context.active_object.data, 'layers', index=5, toggle=True, text='Twist')

        row = col.row()
        row.prop(context.active_object.data, 'layers', index=6, toggle=True, text='Face')     

operators = [RigProperties, SnapIKToFKOperator, ValorantRigPanel, ValorantRigApply, ValorantRigRemove, RigUI, RigLayers]

def register():
    for operator in operators:
        bpy.utils.register_class(operator)
    bpy.types.Scene.my_properties = bpy.props.PointerProperty(type=RigProperties)

def unregister():
    for operator in operators:
        bpy.utils.unregister_class(operator)
    del bpy.types.Scene.my_properties