import React, { useEffect, useState, useRef, Suspense } from 'react'
import { Canvas, useFrame } from '@react-three/fiber'
import { OrbitControls, Grid, useGLTF, Html, Line } from '@react-three/drei'
import * as THREE from 'three'

// Flyway dimensions: S3-AS-04-04 = 4x4 tiles * 60mm = 240mm x 240mm
const FLYWAY_SIZE_X = 0.24   // PMC X dimension in meters (4 tiles)
const FLYWAY_SIZE_Y = 0.24   // PMC Y dimension in meters (4 tiles)
const FLYWAY_TOP_OFFSET = 0.08  // Height offset for XBOT

// Support multiple flyways tiled along +X
const DEFAULT_FLYWAY_COUNT = 2
const DEFAULT_FLYWAY_GAP_X = 0.0 // meters between flyways (0 = touching)

// Coordinate mapping to match vendor display:
// - Origin (0,0) at LOWER-LEFT corner of flyway
// - X axis goes RIGHT (horizontal)
// - Y axis goes UP (vertical on screen, which is -Z in Three.js from our camera angle)
// 
// Three.js coords when camera looks from +Z towards -Z (towards origin):
// - X goes right
// - Y goes up (vertical)
// - Z goes towards/away from camera
//
// For top-down-ish view with origin at lower-left:
// - PMC X -> Three.js X (right)
// - PMC Y -> Three.js -Z (so Y+ goes "up" on screen when camera is in front)

function AxisIndicator() {
    const arrowLength = 0.08
    const height = 0.02
    
    return (
        <group position={[0, height, 0]}>
            {/* X axis - Red arrow pointing RIGHT (+X in Three.js) */}
            <Line
                points={[[0, 0, 0], [arrowLength, 0, 0]]}
                color="#ff0000"
                lineWidth={5}
            />
            <mesh position={[arrowLength, 0, 0]} rotation={[0, 0, -Math.PI / 2]}>
                <coneGeometry args={[0.012, 0.025, 12]} />
                <meshBasicMaterial color="#ff0000" />
            </mesh>
            <Html position={[arrowLength + 0.02, 0, 0]}>
                <div style={{ color: 'red', fontWeight: 'bold', fontSize: '16px', textShadow: '1px 1px 2px black' }}>X</div>
            </Html>
            
            {/* Y axis - Green arrow pointing UP on screen (-Z in Three.js) */}
            <Line
                points={[[0, 0, 0], [0, 0, -arrowLength]]}
                color="#00ff00"
                lineWidth={5}
            />
            <mesh position={[0, 0, -arrowLength]} rotation={[-Math.PI / 2, 0, 0]}>
                <coneGeometry args={[0.012, 0.025, 12]} />
                <meshBasicMaterial color="#00ff00" />
            </mesh>
            <Html position={[0, 0, -arrowLength - 0.02]}>
                <div style={{ color: 'lime', fontWeight: 'bold', fontSize: '16px', textShadow: '1px 1px 2px black' }}>Y</div>
            </Html>
            
            {/* Origin sphere - blue */}
            <mesh position={[0, 0, 0]}>
                <sphereGeometry args={[0.015, 16, 16]} />
                <meshBasicMaterial color="#0088ff" />
            </mesh>
            <Html position={[0.02, 0.02, 0.02]}>
                <div style={{ color: 'cyan', fontWeight: 'bold', fontSize: '14px', textShadow: '1px 1px 2px black' }}>(0,0)</div>
            </Html>
        </group>
    )
}

// GLTF Loader component for Flyway
function FlywayModel({ flywayUrl, offsetX = 0 }) {
    const { scene } = useGLTF(flywayUrl)
    const clonedScene = React.useMemo(() => scene.clone(), [scene])
    
    useEffect(() => {
        clonedScene.traverse((child) => {
            if (child.isMesh) {
                child.castShadow = true
                child.receiveShadow = true
                if (child.material) {
                    child.material.side = THREE.DoubleSide
                }
            }
        })
    }, [clonedScene])
    
    // Position flyway so PMC (0,0) is at lower-left corner.
    // Rotate -90° X to lay flat, then position so corner is at origin.
    // `offsetX` lets us tile multiple flyways along +X (right).
    return (
        <group position={[offsetX + FLYWAY_SIZE_X / 2, 0, -FLYWAY_SIZE_Y / 2]}>
            <primitive object={clonedScene} scale={1} rotation={[-Math.PI / 2, 0, 0]} />
        </group>
    )
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
                if (child.material) {
                    child.material.side = THREE.DoubleSide
                }
            }
        })
    }, [clonedScene])
    
    useFrame(() => {
        if (groupRef.current && position) {
            // Coordinate mapping to match vendor display:
            // PMC X -> Three.js +X (right on screen)
            // PMC Y -> Three.js -Z (up on screen, towards back of scene)
            // PMC Z -> Three.js +Y (levitation height)
            
            const pmX = position.x || 0
            const pmY = position.y || 0
            const pmZ = position.z || 0.001
            
            groupRef.current.position.set(
                pmX,                      // PM X -> 3JS +X (right)
                FLYWAY_TOP_OFFSET + pmZ,  // Height + levitation
                -pmY                      // PM Y -> 3JS -Z (up on screen)
            )
            
            // Apply rotation from PMC (RZ is yaw around vertical axis)
            if (rotation) {
                groupRef.current.rotation.set(
                    rotation.rx || 0,
                    -(rotation.rz || 0),  // Negate RZ due to flipped Z axis
                    rotation.ry || 0
                )
            }
        }
    })
    
    return (
        <group ref={groupRef}>
            {/* Rotate XBOT model to align with coordinate system */}
            <primitive object={clonedScene} scale={1} rotation={[0, Math.PI, 0]} />
        </group>
    )
}

function PlanarMotorScene({ xbots, flywayUrl, xbotUrl, flywayCount = DEFAULT_FLYWAY_COUNT, flywayGapX = DEFAULT_FLYWAY_GAP_X }) {
    const safeFlywayCount = Math.max(1, Number.isFinite(flywayCount) ? flywayCount : DEFAULT_FLYWAY_COUNT)
    const safeGap = Number.isFinite(flywayGapX) ? flywayGapX : DEFAULT_FLYWAY_GAP_X
    const totalWidthX = safeFlywayCount * FLYWAY_SIZE_X + Math.max(0, safeFlywayCount - 1) * safeGap

    // Center of the full flyway array for camera target (in Three.js coords)
    // Flyways extend from (0,0) to (totalWidthX, -FLYWAY_SIZE_Y) in XZ plane
    const centerX = totalWidthX / 2
    const centerZ = -FLYWAY_SIZE_Y / 2
    
    return (
        <>
            <ambientLight intensity={0.6} />
            <directionalLight position={[5, 5, 5]} intensity={0.8} />
            <directionalLight position={[-5, 5, -5]} intensity={0.4} />
            
            {/* Flyways - positioned so PMC (0,0) is at lower-left corner of flyway #1 */}
            <Suspense fallback={null}>
                {Array.from({ length: safeFlywayCount }).map((_, idx) => {
                    const offsetX = idx * (FLYWAY_SIZE_X + safeGap)
                    return <FlywayModel key={idx} flywayUrl={flywayUrl} offsetX={offsetX} />
                })}
            </Suspense>
            
            {/* Axis indicator at origin (0,0) - lower-left corner */}
            <AxisIndicator />
            
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
            
            {/* Grid centered on flyway */}
            <Grid 
                args={[1, 1]} 
                cellColor="#6f6f6f" 
                sectionColor="#9d9d9d"
                position={[centerX, -0.001, centerZ]}
            />
            <OrbitControls 
                target={[centerX, 0.05, centerZ]}
            />
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

export default function PlanarMotorViewer({
    xbots,
    modelBaseUrl = 'http://localhost:3062',
    flywayCount = DEFAULT_FLYWAY_COUNT,
    flywayGapX = DEFAULT_FLYWAY_GAP_X
}) {
    const MAC_BACKEND_URL = import.meta.env.VITE_API_URL || "http://localhost:8091"
    const flywayModelUrl = `${MAC_BACKEND_URL}/models/planar_motor/S3-AS-04-06-OEM-Rev0-FLYWAY-S3-AS.gltf`
    const xbotModelUrl = `${MAC_BACKEND_URL}/models/planar_motor/M3-06-04-OEM-Rev3-XBOT.gltf`
    
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
    
    const safeFlywayCount = Math.max(1, Number.isFinite(flywayCount) ? flywayCount : DEFAULT_FLYWAY_COUNT)
    const safeGap = Number.isFinite(flywayGapX) ? flywayGapX : DEFAULT_FLYWAY_GAP_X
    const totalWidthX = safeFlywayCount * FLYWAY_SIZE_X + Math.max(0, safeFlywayCount - 1) * safeGap

    // Camera positioned to view flyways from front, looking towards -Z
    // This gives a view matching vendor: X goes right, Y goes up
    // Camera at +Z (in front), +Y (above), looking at center of flyway array
    const cameraPosition = [
        totalWidthX / 2,
        0.5,
        0.4 + Math.max(0, totalWidthX - FLYWAY_SIZE_X) * 1.0
    ]
    
    return (
        <div style={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column' }}>
            <div style={{ flex: 1, minHeight: '400px', border: '1px solid #ccc', overflow: 'hidden', position: 'relative' }}>
                <Canvas camera={{ position: cameraPosition, fov: 50 }}>
                    <Suspense fallback={<Fallback />}>
                        <PlanarMotorScene
                            xbots={xbots}
                            flywayUrl={flywayModelUrl}
                            xbotUrl={xbotModelUrl}
                            flywayCount={safeFlywayCount}
                            flywayGapX={safeGap}
                        />
                    </Suspense>
                </Canvas>
                
                {/* Message when no XBOTs connected */}
                {(!xbots || Object.keys(xbots).length === 0) && (
                    <div style={{ 
                        position: 'absolute',
                        top: '50%',
                        left: '50%',
                        transform: 'translate(-50%, -50%)',
                        background: 'rgba(0, 0, 0, 0.8)', 
                        color: '#aaa', 
                        padding: '20px 30px', 
                        borderRadius: '8px', 
                        fontFamily: 'system-ui, sans-serif', 
                        fontSize: '14px',
                        textAlign: 'center',
                        border: '1px solid #555',
                        pointerEvents: 'none',
                        zIndex: 10
                    }}>
                        <div style={{ fontSize: '16px', fontWeight: 'bold', marginBottom: '8px', color: '#69c0ff' }}>
                            Connect to see XBOT
                        </div>
                        <div>Click "Connect" to view XBOT position on the flyway</div>
                    </div>
                )}
                
                {/* Position display */}
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
                        <div style={{ borderBottom: '1px solid #555', marginBottom: '4px', fontWeight: 'bold' }}>XBOT Positions</div>
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
                
                {/* Axis legend in bottom-left */}
                <div style={{
                    position: 'absolute',
                    bottom: '10px',
                    left: '10px',
                    background: 'rgba(0, 0, 0, 0.75)',
                    color: 'white',
                    padding: '8px 12px',
                    borderRadius: '6px',
                    fontFamily: 'monospace',
                    fontSize: '11px',
                    border: '1px solid #555',
                    pointerEvents: 'none',
                    zIndex: 10
                }}>
                    <div><span style={{ color: 'red', fontWeight: 'bold' }}>→ X+</span> (right)</div>
                    <div><span style={{ color: 'green', fontWeight: 'bold' }}>→ Y+</span> (forward)</div>
                    <div><span style={{ color: 'blue', fontWeight: 'bold' }}>●</span> Origin (0,0)</div>
                </div>
            </div>
        </div>
    )
}
