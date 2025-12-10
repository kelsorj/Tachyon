import React, { useEffect, useState, useRef, Suspense, useMemo } from 'react'
import { Canvas } from '@react-three/fiber'
import { OrbitControls, Grid, Html } from '@react-three/drei'
import URDFLoader from 'urdf-loader'
import { STLLoader } from 'three/examples/jsm/loaders/STLLoader'
import * as THREE from 'three'

function RobotModel({ joints, cartesian }) {
    const [robot, setRobot] = useState(null)
    const overlayRef = useRef()

    // Load URDF
    useEffect(() => {
        const loader = new URDFLoader()
        loader.parseVisual = true
        loader.loadMeshCb = (path, manager, onComplete) => {
            const stlLoader = new STLLoader(manager)
            const cleanPath = path.replace(/^\/+/, '').replace(/.*meshes\//, 'meshes/')
            const url = `http://localhost:3061/${cleanPath}`
            stlLoader.load(url, (geo) => {
                const mesh = new THREE.Mesh(geo, new THREE.MeshPhongMaterial({ color: 0x9dcfe9, flatShading: false }))
                onComplete(mesh)
            }, undefined, (err) => {
                console.error('Error mesh:', url, err)
                onComplete(new THREE.Mesh())
            })
        }
        const urdfUrl = `http://localhost:3061/urdf/pf400Complete.urdf?t=${Date.now()}`
        loader.load(urdfUrl, (result) => {
            console.log('URDF loaded')
            
            // -- Fixes --
            if (result.joints.j1) { result.joints.j1.position.set(0, 0, -0.08); if(result.joints.j1.axis) result.joints.j1.axis.set(0,0,1); }
            if (result.joints.j2) result.joints.j2.position.set(0, -0.122, 0);
            if (result.joints.j3) { result.joints.j3.position.set(0, -0.233, 0); if(result.joints.j3.limit) { result.joints.j3.limit.lower=-6.28; result.joints.j3.limit.upper=6.28; } }
            if (result.joints.j4) result.joints.j4.position.set(0, -0.217, 0);
            if (result.joints.j5left?.axis) result.joints.j5left.axis.set(1,0,0);
            if (result.joints.j5right?.axis) { result.joints.j5right.axis.set(1,0,0); result.joints.j5right.mimic=null; }

            const linksToFix = ['shoulder', 'bicep', 'forearm', 'palm', 'fingerleft', 'fingerright']
            result.traverse(c => { if(c.isMesh) c.visible=true });
            
            linksToFix.forEach(link => {
                if (result.links[link]) {
                    result.links[link].traverse(c => {
                        if (c.isURDFVisual) {
                             if(link==='shoulder') { if(Math.abs(c.position.y)>0.1) { c.position.z=c.position.y/1000; c.position.y=-0.09 } else c.position.set(0,0,0) }
                             else if(link==='bicep') c.position.set(0, -0.112, -0.065)
                             else if(link==='forearm') { c.position.set(0, -0.115, -0.071); c.rotation.x=Math.PI }
                             else if(link==='palm') { c.position.set(0, 0.05, -0.15); c.rotation.y=Math.PI }
                             else if(link==='fingerleft' || link==='fingerright') c.position.set(0, 0.125, -0.150)
                        }
                    })
                }
            })
            setRobot(result)
        })
    }, [])

    // Update Joints & Overlay
    useEffect(() => {
        if (!robot) return

        // Update Joints
        if (robot.joints.j1) robot.joints.j1.setJointValue(joints?.j1 || 0)
        if (robot.joints.j2) robot.joints.j2.setJointValue(joints?.j2 || 0)
        if (robot.joints.j3) robot.joints.j3.setJointValue(joints?.j3 || 0)
        if (robot.joints.j4) robot.joints.j4.setJointValue((joints?.j4 || 0) + Math.PI) // Visual offset

        // Gripper
        if (joints && joints.gripper !== undefined) {
            const half = joints.gripper / 2.0
            if (robot.links.fingerleft) robot.links.fingerleft.traverse(c => { if(c.isURDFVisual) c.position.x = half - 0.065 })
            if (robot.links.fingerright) robot.links.fingerright.traverse(c => { if(c.isURDFVisual) c.position.x = -half + 0.065 })
        }

        // Force Matrix Update for World Position
        robot.updateMatrixWorld(true)

        // Update Overlay - position at fixed offset from robot base
        // Robot base is at [0, 0.225, 0] relative to the robot group
        // Position axes overlay at a fixed offset from base, not from palm
        if (overlayRef.current && robot) {
            // Position at fixed offset from robot base (above the base, offset towards red cap/front)
            // Offset in positive Z direction (towards red cap at +1000mm, where the arm extends) so Z arrows are visible
            // Y position tracks J1 (vertical joint) so axes move up/down with the arm
            const j1_m = joints?.j1 || 0 // J1 is in meters
            const baseY = 0.225 // Robot base Y position
            const offsetY = 0.15 // Fixed offset above base
            overlayRef.current.position.set(0, baseY + offsetY + j1_m, 0.2) // Moves with J1
            
            // Rotation: Face outward (keep rotation simple, or calculate from base if needed)
            overlayRef.current.rotation.y = 0
        }

    }, [robot, joints])

    // Arrows - Light/Dark variants
    const arrows = useMemo(() => [
        { dir: new THREE.Vector3(0, 1, 0), color: 0x69c0ff }, // Up (Light Blue)
        { dir: new THREE.Vector3(0, -1, 0), color: 0x0050b3 }, // Down (Dark Blue)
        { dir: new THREE.Vector3(0, 0, 1), color: 0x95de64 }, // Out (Light Green)
        { dir: new THREE.Vector3(0, 0, -1), color: 0x237804 }, // In (Dark Green)
        { dir: new THREE.Vector3(-1, 0, 0), color: 0xff7875 }, // Right (Light Red)
        { dir: new THREE.Vector3(1, 0, 0), color: 0xa8071a }, // Left (Dark Red)
    ], [])

    if (!robot) return null

    // Calculate rail position (J6 in meters)
    // The rail is 2m long, and J6 range is -1000mm to +1000mm (-1m to +1m)
    // So the rail extends from -1m to +1m along X axis
    // J6 value directly maps to X position
    const railPosition = (joints?.j6 || joints?.rail || 0) // J6 is in meters, range -1 to +1
    
    return (
        <group>
            {/* Rail visualization - extended to show full travel range including robot base width */}
            {/* After rotation, rail runs along world Z axis from -1.1m to +1.1m (extended) */}
            {/* Rail width (~0.18m) extends in world X direction, centered at x=0 */}
            {/* Actual travel limits are -1.0m to +1.0m (-1000mm to +1000mm) */}
            <group position={[0, -0.015, 0]} rotation={[0, Math.PI / 2, 0]}>
                {/* Rail base/beam - extended to 2.4m to account for robot base width */}
                <mesh position={[0, 0, 0]}>
                    <boxGeometry args={[2.4, 0.05, 0.18]} />
                    <meshStandardMaterial color={0xd0d0d0} metalness={0.3} roughness={0.7} />
                </mesh>
                {/* Rail end markers - blue at -1000mm, red at +1000mm (actual travel limits) */}
                {/* After rotation: local x=-1 → world z=-1, local x=+1 → world z=+1 */}
                {/* Blue end (-1000mm) */}
                <mesh position={[-1.0, 0, 0]}>
                    <boxGeometry args={[0.05, 0.08, 0.18]} />
                    <meshStandardMaterial color={0x0066ff} />
                </mesh>
                {/* Red end (+1000mm) */}
                <mesh position={[1.0, 0, 0]}>
                    <boxGeometry args={[0.05, 0.08, 0.18]} />
                    <meshStandardMaterial color={0xff0000} />
                </mesh>
            </group>
            
            {/* Robot positioned on rail - moves along Z axis (same direction as rail) */}
            <group position={[0, 0, railPosition]}>
                <primitive object={robot} rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.225, 0]} dispose={null} />
            
            {/* Coordinated Overlay */}
            <group ref={overlayRef}>
                {arrows.map((a, i) => (
                    <arrowHelper key={i} args={[a.dir, new THREE.Vector3(0,0,0), 0.2, a.color, 0.05, 0.03]} />
                ))}
                
                {/* Rotation Arcs (CW/CCW) */}
                {/* Light Purple Arc (Right Side) */}
                <mesh rotation={[-Math.PI/2, 0, 0]}>
                    <torusGeometry args={[0.25, 0.01, 8, 32, Math.PI]} />
                    <meshBasicMaterial color={0xb37feb} side={THREE.DoubleSide} />
                </mesh>
                
                {/* Dark Purple Arc (Left Side) */}
                <mesh rotation={[-Math.PI/2, 0, Math.PI]}>
                    <torusGeometry args={[0.25, 0.01, 8, 32, Math.PI]} />
                    <meshBasicMaterial color={0x391085} side={THREE.DoubleSide} />
                </mesh>
            </group>
            </group>
        </group>
    )
}

function Fallback() {
    return <mesh><boxGeometry args={[0.1]} /><meshStandardMaterial color="orange" /></mesh>
}

export default function RobotViewer({ joints, cartesian }) {
    return (
        <div style={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column' }}>
            <div style={{ flex: 1, minHeight: '400px', border: '1px solid #ccc', overflow: 'hidden', position: 'relative' }}>
                <Canvas camera={{ position: [1, 1, 1], fov: 50 }}>
                    <ambientLight intensity={0.6} />
                    <directionalLight position={[5, 5, 5]} intensity={0.8} />
                    <directionalLight position={[-5, -5, -5]} intensity={0.3} />
                    <Suspense fallback={<Fallback />}>
                        <RobotModel joints={joints} cartesian={cartesian} />
                        <Grid args={[10, 10]} cellColor="#6f6f6f" sectionColor="#9d9d9d" />
                        <OrbitControls />
                    </Suspense>
                </Canvas>
                
                {/* Position display - fixed in top-right corner */}
                <div style={{ 
                    position: 'absolute',
                    top: '10px',
                    right: '10px',
                    background: 'rgba(0, 0, 0, 0.75)', 
                    color: 'white', 
                    padding: '10px', 
                    borderRadius: '6px', 
                    fontFamily: 'monospace', 
                    fontSize: '12px',
                    whiteSpace: 'pre',
                    textAlign: 'left',
                    minWidth: '150px',
                    border: '1px solid #555',
                    pointerEvents: 'none',
                    zIndex: 10
                }}>
                    <div style={{ borderBottom: '1px solid #555', marginBottom: '4px', fontWeight: 'bold' }}>Position (mm/deg)</div>
                    <div>X: {(cartesian?.x || 0).toFixed(1)} mm</div>
                    <div>Y: {(cartesian?.y || 0).toFixed(1)} mm</div>
                    <div>Z: {(cartesian?.z || 0).toFixed(1)} mm</div>
                    <div>Yaw: {(cartesian?.yaw || 0).toFixed(1)}°</div>
                    <div style={{ borderBottom: '1px solid #555', margin: '4px 0', fontWeight: 'bold' }}>Joints</div>
                    <div>J1: {((joints?.j1 || 0) * 1000).toFixed(1)} mm</div>
                    <div>J2: {((joints?.j2 || 0) * 180/Math.PI).toFixed(1)}°</div>
                    <div>J3: {((joints?.j3 || 0) * 180/Math.PI).toFixed(1)}°</div>
                    <div>J4: {((joints?.j4 || 0) * 180/Math.PI).toFixed(1)}°</div>
                    <div>Grp: {((joints?.gripper || 0) * 1000).toFixed(1)} mm</div>
                    {(joints?.j6 !== undefined || joints?.rail !== undefined) && (
                        <div>J6/Rail: {((joints?.j6 || joints?.rail || 0) * 1000).toFixed(1)} mm</div>
                    )}
                </div>
            </div>
        </div>
    )
}
