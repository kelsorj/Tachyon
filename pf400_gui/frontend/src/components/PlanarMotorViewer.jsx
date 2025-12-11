import React, { useEffect, useState, useRef, Suspense } from 'react'
import { Canvas, useFrame } from '@react-three/fiber'
import { OrbitControls, Grid, useGLTF } from '@react-three/drei'
import * as THREE from 'three'

// Preload GLTF models (drei's useGLTF.preload can be called at module level)
if (typeof window !== 'undefined') {
  useGLTF.preload('http://localhost:3062/models/S4-AS-04-06-OEM-Rev3-FLYWAY-S4-AS.gltf')
  useGLTF.preload('http://localhost:3062/models/M3-06-04-OEM-Rev3-XBOT.gltf')
}

// GLTF Loader component for Flyway
function FlywayModel() {
    const { scene } = useGLTF('http://localhost:3062/models/S4-AS-04-06-OEM-Rev3-FLYWAY-S4-AS.gltf')
    
    useEffect(() => {
        scene.traverse((child) => {
            if (child.isMesh) {
                child.castShadow = true
                child.receiveShadow = true
            }
        })
    }, [scene])
    
    return <primitive object={scene} />
}

// GLTF Loader component for XBOT
function XBOTModel({ position, rotation }) {
    const { scene } = useGLTF('http://localhost:3062/models/M3-06-04-OEM-Rev3-XBOT.gltf')
    const groupRef = useRef()
    
    useEffect(() => {
        scene.traverse((child) => {
            if (child.isMesh) {
                child.castShadow = true
                child.receiveShadow = true
            }
        })
    }, [scene])
    
    // Update position and rotation
    useFrame(() => {
        if (groupRef.current && position) {
            // Planar Motor coordinates: X and Y are in the horizontal plane, Z is vertical
            // Three.js: X is right, Y is up, Z is forward (right-handed)
            // We'll map: PM X -> 3JS X, PM Y -> 3JS Z, PM Z -> 3JS Y
            groupRef.current.position.set(
                position.x || 0,  // PM X -> 3JS X (right)
                position.z || 0,  // PM Z -> 3JS Y (up)
                position.y || 0   // PM Y -> 3JS Z (forward)
            )
            if (rotation) {
                groupRef.current.rotation.set(
                    rotation.rx || 0,  // Roll around X
                    rotation.rz || 0,  // Yaw around Y (Z rotation in PM)
                    rotation.ry || 0   // Pitch around Z (Y rotation in PM)
                )
            }
        }
    })
    
    return (
        <group ref={groupRef}>
            <primitive object={scene} />
        </group>
    )
}

function PlanarMotorScene({ xbots }) {
    return (
        <>
            <ambientLight intensity={0.6} />
            <directionalLight position={[5, 5, 5]} intensity={0.8} />
            <directionalLight position={[-5, 5, -5]} intensity={0.4} />
            
            {/* Flyway (floor) */}
            <Suspense fallback={null}>
                <FlywayModel />
            </Suspense>
            
            {/* XBOTs */}
            {xbots && Object.entries(xbots).map(([xbotId, xbot]) => (
                <Suspense key={xbotId} fallback={null}>
                    <XBOTModel 
                        position={xbot.position}
                        rotation={{
                            rx: xbot.position?.rx || 0,
                            ry: xbot.position?.ry || 0,
                            rz: xbot.position?.rz || 0
                        }}
                    />
                </Suspense>
            ))}
            
            <Grid args={[2, 2]} cellColor="#6f6f6f" sectionColor="#9d9d9d" />
            <OrbitControls />
        </>
    )
}

function Fallback() {
    return (
        <Html center>
            <div style={{ color: 'white' }}>Loading Planar Motor...</div>
        </Html>
    )
}

export default function PlanarMotorViewer({ xbots }) {
    return (
        <div style={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column' }}>
            <div style={{ flex: 1, minHeight: '400px', border: '1px solid #ccc', overflow: 'hidden', position: 'relative' }}>
                <Canvas camera={{ position: [0.5, 0.5, 0.5], fov: 50 }}>
                    <Suspense fallback={<Fallback />}>
                        <PlanarMotorScene xbots={xbots} />
                    </Suspense>
                </Canvas>
                
                {/* Position display - fixed in top-right corner */}
                {xbots && Object.keys(xbots).length > 0 && (
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
                        minWidth: '200px',
                        border: '1px solid #555',
                        pointerEvents: 'none',
                        zIndex: 10
                    }}>
                        <div style={{ borderBottom: '1px solid #555', marginBottom: '4px', fontWeight: 'bold' }}>XBOT Positions (m)</div>
                        {Object.entries(xbots).map(([xbotId, xbot]) => (
                            <div key={xbotId} style={{ marginBottom: '4px' }}>
                                <div style={{ fontWeight: 'bold', color: '#69c0ff' }}>XBOT {xbotId}</div>
                                <div>State: {xbot.state || 'UNKNOWN'}</div>
                                {xbot.position && (
                                    <>
                                        <div>X: {(xbot.position.x * 1000).toFixed(1)} mm</div>
                                        <div>Y: {(xbot.position.y * 1000).toFixed(1)} mm</div>
                                        <div>Z: {(xbot.position.z * 1000).toFixed(1)} mm</div>
                                    </>
                                )}
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    )
}

