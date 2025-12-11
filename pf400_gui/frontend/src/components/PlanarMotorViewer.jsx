import React, { useEffect, useState, useRef, Suspense } from 'react'
import { Canvas, useFrame } from '@react-three/fiber'
import { OrbitControls, Grid, useGLTF, Html } from '@react-three/drei'
import * as THREE from 'three'

// GLTF Loader component for Flyway
function FlywayModel({ flywayUrl }) {
    const { scene } = useGLTF(flywayUrl)
    const clonedScene = React.useMemo(() => scene.clone(), [scene])
    
    useEffect(() => {
        clonedScene.traverse((child) => {
            if (child.isMesh) {
                child.castShadow = true
                child.receiveShadow = true
                // Ensure materials are visible
                if (child.material) {
                    child.material.side = THREE.DoubleSide
                }
            }
        })
    }, [clonedScene])
    
    // Flyway model - S4-AS-04-06 is a 4x6 stator system
    // Model is already in METERS
    // Try no rotation first to see natural orientation
    return <primitive object={clonedScene} scale={1} />
}

// GLTF Loader component for XBOT
function XBOTModel({ position, rotation, xbotUrl }) {
    const { scene } = useGLTF(xbotUrl)
    const clonedScene = React.useMemo(() => scene.clone(), [scene])
    const groupRef = useRef()
    
    useEffect(() => {
        clonedScene.traverse((child) => {
            if (child.isMesh) {
                child.castShadow = true
                child.receiveShadow = true
                // Ensure materials are visible
                if (child.material) {
                    child.material.side = THREE.DoubleSide
                }
            }
        })
    }, [clonedScene])
    
    // Update position and rotation
    useFrame(() => {
        if (groupRef.current && position) {
            // Planar Motor coordinates: X and Y are in the horizontal plane, Z is vertical (levitation height)
            // Three.js: X is right, Y is up, Z is forward (right-handed)
            // Backend returns positions in METERS
            // Mapping: PM X -> 3JS X, PM Y -> 3JS Z, PM Z -> 3JS Y (up)
            
            const pmX = position.x || 0  // PM X position in meters
            const pmY = position.y || 0  // PM Y position in meters  
            const pmZ = position.z || 0.001  // PM Z (levitation height) in meters
            
            // Flyway top surface offset - XBOT sits on top of the flyway
            // The flyway model has significant height, offset XBOT above it
            const FLYWAY_TOP_OFFSET = 0.12  // ~120mm to sit on top of the flyway
            
            groupRef.current.position.set(
                pmX,  // PM X -> 3JS X
                FLYWAY_TOP_OFFSET + pmZ,  // Flyway height + levitation height
                pmY   // PM Y -> 3JS Z
            )
            
            if (rotation) {
                groupRef.current.rotation.set(
                    rotation.rx || 0,
                    rotation.rz || 0,
                    rotation.ry || 0
                )
            }
        }
    })
    
    return (
        <group ref={groupRef}>
            <primitive object={clonedScene} scale={1} />
        </group>
    )
}

function PlanarMotorScene({ xbots, flywayUrl, xbotUrl }) {
    return (
        <>
            <ambientLight intensity={0.6} />
            <directionalLight position={[5, 5, 5]} intensity={0.8} />
            <directionalLight position={[-5, 5, -5]} intensity={0.4} />
            
            {/* Flyway (floor) - temporarily hidden for debugging */}
            {/* <Suspense fallback={null}>
                <FlywayModel flywayUrl={flywayUrl} />
            </Suspense> */}
            
            {/* Debug: Add a simple floor plane to see where Y=0 is */}
            <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0.2, 0, 0.3]}>
                <planeGeometry args={[0.4, 0.6]} />
                <meshStandardMaterial color="#444" transparent opacity={0.5} />
            </mesh>
            
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
                        xbotUrl={xbotUrl}
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

export default function PlanarMotorViewer({ xbots, modelBaseUrl = 'http://localhost:3062' }) {
    // Models are served from Mac backend (PF400 backend on 3061), not PC backend
    const MAC_BACKEND_URL = "http://localhost:3061"
    const flywayModelUrl = `${MAC_BACKEND_URL}/models/planar_motor/S4-AS-04-06-OEM-Rev3-FLYWAY-S4-AS.gltf`
    const xbotModelUrl = `${MAC_BACKEND_URL}/models/planar_motor/M3-06-04-OEM-Rev3-XBOT.gltf`
    
    // Preload models
    useEffect(() => {
        if (typeof window !== 'undefined') {
            try {
                useGLTF.preload(flywayModelUrl)
                useGLTF.preload(xbotModelUrl)
            } catch (e) {
                console.warn('Failed to preload models:', e)
            }
        }
    }, [])
    
    return (
        <div style={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column' }}>
            <div style={{ flex: 1, minHeight: '400px', border: '1px solid #ccc', overflow: 'hidden', position: 'relative' }}>
                <Canvas camera={{ position: [0.3, 0.4, 0.5], fov: 50 }}>
                    <Suspense fallback={<Fallback />}>
                        <PlanarMotorScene xbots={xbots} flywayUrl={flywayModelUrl} xbotUrl={xbotModelUrl} />
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

