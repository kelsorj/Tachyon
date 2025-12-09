import json
import os

def inspect_gltf(file_path):
    with open(file_path, 'r') as f:
        data = json.load(f)

    print(f"Asset: {data.get('asset')}")
    print(f"Scenes: {len(data.get('scenes', []))}")
    print(f"Nodes: {len(data.get('nodes', []))}")
    print(f"Meshes: {len(data.get('meshes', []))}")
    print(f"Buffers: {len(data.get('buffers', []))}")

    # Check buffers
    for i, buf in enumerate(data.get('buffers', [])):
        uri = buf.get('uri', '')
        print(f"Buffer {i}: byteLength={buf.get('byteLength')}, uri_start={uri[:50]}...")

    # Print hierarchy
    nodes = data.get('nodes', [])
    
    def print_node(node_idx, indent=0):
        node = nodes[node_idx]
        name = node.get('name', f"Node_{node_idx}")
        mesh_idx = node.get('mesh')
        mesh_info = ""
        if mesh_idx is not None:
            mesh_name = data['meshes'][mesh_idx].get('name', f"Mesh_{mesh_idx}")
            mesh_info = f" (Mesh: {mesh_name})"
        
        print(f"{'  ' * indent}- {name}{mesh_info}")
        
        for child_idx in node.get('children', []):
            print_node(child_idx, indent + 1)

    if 'scenes' in data:
        scene = data['scenes'][data.get('scene', 0)]
        for node_idx in scene.get('nodes', []):
            print_node(node_idx)

if __name__ == "__main__":
    inspect_gltf('/Users/kelsorj/My Drive/Code/Aikon/models/pf400Complete.gltf')
