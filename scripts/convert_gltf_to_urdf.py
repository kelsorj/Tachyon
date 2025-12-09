import json
import struct
import base64
import math
import os

def get_buffer_data(gltf, buffer_idx):
    buf = gltf['buffers'][buffer_idx]
    uri = buf['uri']
    if uri.startswith('data:application/octet-stream;base64,'):
        return base64.b64decode(uri.split(',')[1])
    else:
        # Assume external file
        with open(uri, 'rb') as f:
            return f.read()

def get_accessor_data(gltf, buffer_data, accessor_idx):
    accessor = gltf['accessors'][accessor_idx]
    buffer_view = gltf['bufferViews'][accessor['bufferView']]
    
    # Calculate offset
    offset = buffer_view.get('byteOffset', 0) + accessor.get('byteOffset', 0)
    count = accessor['count']
    component_type = accessor['componentType']
    type_str = accessor['type']
    
    # Determine stride and format
    if component_type == 5126: # FLOAT
        comp_size = 4
        fmt_char = 'f'
    elif component_type == 5123: # UNSIGNED_SHORT
        comp_size = 2
        fmt_char = 'H'
    elif component_type == 5125: # UNSIGNED_INT
        comp_size = 4
        fmt_char = 'I'
    else:
        raise ValueError(f"Unsupported component type: {component_type}")
        
    if type_str == 'SCALAR':
        num_comps = 1
    elif type_str == 'VEC3':
        num_comps = 3
    else:
        raise ValueError(f"Unsupported type: {type_str}")
        
    stride = buffer_view.get('byteStride', num_comps * comp_size)
    
    data = []
    for i in range(count):
        chunk = buffer_data[offset + i * stride : offset + i * stride + num_comps * comp_size]
        values = struct.unpack(f'<{num_comps}{fmt_char}', chunk)
        if num_comps == 1:
            data.append(values[0])
        else:
            data.append(values)
            
    return data

def write_stl(filename, vertices, indices):
    with open(filename, 'wb') as f:
        # Header (80 bytes)
        f.write(b'\x00' * 80)
        # Number of triangles
        num_triangles = len(indices) // 3
        f.write(struct.pack('<I', num_triangles))
        
        for i in range(0, len(indices), 3):
            # Normal (0,0,0) - standard STL doesn't strictly require it for some viewers, or we can compute it
            f.write(struct.pack('<3f', 0.0, 0.0, 0.0))
            
            v1 = vertices[indices[i]]
            v2 = vertices[indices[i+1]]
            v3 = vertices[indices[i+2]]
            
            f.write(struct.pack('<3f', *v1))
            f.write(struct.pack('<3f', *v2))
            f.write(struct.pack('<3f', *v3))
            
            # Attribute byte count
            f.write(struct.pack('<H', 0))

def matrix_to_rpy(m):
    # m is a list of 16 floats (column-major)
    # Rotation matrix is top-left 3x3
    # R11 R12 R13
    # R21 R22 R23
    # R31 R32 R33
    
    # Column major:
    # 0  4  8  12
    # 1  5  9  13
    # 2  6  10 14
    # 3  7  11 15
    
    r11, r21, r31 = m[0], m[1], m[2]
    r12, r22, r32 = m[4], m[5], m[6]
    r13, r23, r33 = m[8], m[9], m[10]
    
    # Sy = sqrt(R11*R11 + R21*R21)
    sy = math.sqrt(r11*r11 + r21*r21)
    
    singular = sy < 1e-6
    
    if not singular:
        x = math.atan2(r32, r33)
        y = math.atan2(-r31, sy)
        z = math.atan2(r21, r11)
    else:
        x = math.atan2(-r23, r22)
        y = math.atan2(-r31, sy)
        z = 0
        
    return x, y, z

def convert(gltf_path, output_dir):
    with open(gltf_path, 'r') as f:
        gltf = json.load(f)
        
    buffer_data = get_buffer_data(gltf, 0) # Assume single buffer
    
    nodes = gltf['nodes']
    
    # Map of part name to node index
    parts = {
        'vertical': None,
        'shoulder': None,
        'bicep': None,
        'forearm': None,
        'palm': None,
        'fingerRight': None,
        'fingerLeft': None
    }
    
    # Find nodes
    for i, node in enumerate(nodes):
        name = node.get('name', '')
        for part in parts:
            if part in name and 'occurrence' in name:
                parts[part] = i
                break
                
    print("Found parts:", parts)
    
    urdf_links = []
    urdf_joints = []
    
    # Base link
    urdf_links.append('<link name="base_link"/>')
    
    for part_name, node_idx in parts.items():
        if node_idx is None:
            print(f"Warning: Part {part_name} not found")
            continue
            
        node = nodes[node_idx]
        # The mesh is in the child node for these "occurrences"
        child_idx = node['children'][0]
        child_node = nodes[child_idx]
        mesh_idx = child_node['mesh']
        
        # Extract Mesh
        mesh = gltf['meshes'][mesh_idx]
        primitive = mesh['primitives'][0]
        pos_accessor = primitive['attributes']['POSITION']
        idx_accessor = primitive['indices']
        
        vertices = get_accessor_data(gltf, buffer_data, pos_accessor)
        indices = get_accessor_data(gltf, buffer_data, idx_accessor)
        
        stl_filename = f"pf400_{part_name}.stl"
        write_stl(os.path.join(output_dir, stl_filename), vertices, indices)
        print(f"Wrote {stl_filename}")
        
        # Get Transform
        matrix = node.get('matrix', [1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1])
        tx, ty, tz = matrix[12], matrix[13], matrix[14]
        roll, pitch, yaw = matrix_to_rpy(matrix)
        
        # Create Link
        link_name = f"{part_name}_link"
        urdf_links.append(f'''
  <link name="{link_name}">
    <visual>
      <geometry>
        <mesh filename="package://models/{stl_filename}"/>
      </geometry>
      <material name="grey">
        <color rgba="0.7 0.7 0.7 1.0"/>
      </material>
    </visual>
    <collision>
      <geometry>
        <mesh filename="package://models/{stl_filename}"/>
      </geometry>
    </collision>
  </link>''')
        
        # Create Joint (Fixed to base for now)
        joint_name = f"base_to_{part_name}"
        urdf_joints.append(f'''
  <joint name="{joint_name}" type="fixed">
    <parent link="base_link"/>
    <child link="{link_name}"/>
    <origin xyz="{tx} {ty} {tz}" rpy="{roll} {pitch} {yaw}"/>
  </joint>''')

    # Write URDF
    urdf_content = f'''<?xml version="1.0"?>
<robot name="pf400">
{''.join(urdf_links)}
{''.join(urdf_joints)}
</robot>
'''
    
    with open(os.path.join(output_dir, 'pf400.urdf'), 'w') as f:
        f.write(urdf_content)
    print("Wrote pf400.urdf")

if __name__ == "__main__":
    convert('/Users/kelsorj/My Drive/Code/Aikon/models/pf400Complete.gltf', '/Users/kelsorj/My Drive/Code/Aikon/models')
