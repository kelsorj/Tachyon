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

        // Update Overlay
        if (overlayRef.current && robot.links.palm) {
            const palmPos = new THREE.Vector3()
            robot.links.palm.getWorldPosition(palmPos)
            overlayRef.current.position.copy(palmPos)
            
            // Rotation: Face Outward from Base Z
            const angle = Math.atan2(palmPos.x, palmPos.z)
            overlayRef.current.rotation.y = angle
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

    return (
        <group>
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

                {/* HUD - Attached to end effector overlay or just in scene? */}
                {/* User asked for "HUD over the robot". Html component follows 3D position. */}
                <Html position={[0.3, 0.3, 0]} center style={{ pointerEvents: 'none' }}>
                    <div style={{ 
                        background: 'rgba(0, 0, 0, 0.7)', 
                        color: 'white', 
                        padding: '8px', 
                        borderRadius: '4px', 
                        fontFamily: 'monospace', 
                        fontSize: '12px',
                        whiteSpace: 'pre',
                        textAlign: 'left',
                        minWidth: '150px',
                        border: '1px solid #555'
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
                    </div>
                </Html>
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
            </div>
        </div>
    )
}
