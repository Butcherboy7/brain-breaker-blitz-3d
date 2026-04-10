import { useRef, useMemo } from 'react';
import { useFrame, useThree } from '@react-three/fiber';
import * as THREE from 'three';
import { Brick } from './types';

const ARENA_W = 8;
const ARENA_H = 10;
const BALL_R = 0.15;

interface GameSceneProps {
  bricks: Brick[];
  paddleX: React.MutableRefObject<number>;
  ballPos: React.MutableRefObject<[number, number, number]>;
  ballVel: React.MutableRefObject<[number, number]>;
  paddleWidth: number;
  isPlaying: boolean;
  onHitBrick: (id: string) => void;
  onLoseLife: () => void;
  onLevelClear: () => void;
}

export default function GameScene({
  bricks, paddleX, ballPos, ballVel, paddleWidth,
  isPlaying, onHitBrick, onLoseLife, onLevelClear,
}: GameSceneProps) {
  const ballRef = useRef<THREE.Mesh>(null);
  const paddleRef = useRef<THREE.Mesh>(null);
  const { viewport } = useThree();
  const brickRefs = useRef<Map<string, THREE.Mesh>>(new Map());
  const trailRef = useRef<THREE.Points>(null);

  // Mouse tracking for paddle
  const { pointer } = useThree();

  useFrame(() => {
    if (!isPlaying) return;

    // Move paddle
    const targetX = (pointer.x * viewport.width) / 2;
    const halfPaddle = paddleWidth / 2;
    const halfArena = ARENA_W / 2;
    paddleX.current = Math.max(-halfArena + halfPaddle, Math.min(halfArena - halfPaddle, targetX));
    if (paddleRef.current) {
      paddleRef.current.position.x = paddleX.current;
      paddleRef.current.scale.x = paddleWidth / 2;
    }

    // Move ball
    const [bx, by] = ballPos.current;
    const [vx, vy] = ballVel.current;
    let nx = bx + vx;
    let ny = by + vy;
    let nvx = vx;
    let nvy = vy;

    // Wall collisions
    if (nx <= -halfArena + BALL_R || nx >= halfArena - BALL_R) {
      nvx = -nvx;
      nx = Math.max(-halfArena + BALL_R, Math.min(halfArena - BALL_R, nx));
    }
    if (ny >= ARENA_H / 2 - BALL_R) {
      nvy = -Math.abs(nvy);
      ny = ARENA_H / 2 - BALL_R;
    }

    // Bottom - lose life
    if (ny <= -ARENA_H / 2) {
      onLoseLife();
      return;
    }

    // Paddle collision
    const py = -4.2;
    if (nvy < 0 && ny <= py + 0.3 && ny >= py - 0.1) {
      if (nx >= paddleX.current - halfPaddle - 0.1 && nx <= paddleX.current + halfPaddle + 0.1) {
        nvy = Math.abs(nvy);
        const hitPos = (nx - paddleX.current) / halfPaddle;
        nvx = hitPos * Math.abs(nvy) * 0.8;
        ny = py + 0.31;
      }
    }

    // Brick collisions
    let hitAny = false;
    for (const brick of bricks) {
      const [bkx, bky] = brick.position;
      const hw = 0.5;
      const hh = 0.22;
      if (nx >= bkx - hw && nx <= bkx + hw && ny >= bky - hh && ny <= bky + hh) {
        // Determine collision side
        const dx = nx - bkx;
        const dy = ny - bky;
        if (Math.abs(dx / hw) > Math.abs(dy / hh)) {
          nvx = -nvx;
        } else {
          nvy = -nvy;
        }
        onHitBrick(brick.id);
        hitAny = true;
        break;
      }
    }

    ballVel.current = [nvx, nvy];
    ballPos.current = [nx, ny, 0];
    if (ballRef.current) {
      ballRef.current.position.set(nx, ny, 0);
    }

    // Check level clear
    if (bricks.length === 0 || (hitAny && bricks.length <= 1)) {
      onLevelClear();
    }
  });

  // Brick health color interpolation
  const getBrickColor = (brick: Brick) => {
    const ratio = brick.health / brick.maxHealth;
    if (brick.isBonus) return brick.color;
    const brightness = 0.3 + ratio * 0.7;
    return new THREE.Color(brick.color).multiplyScalar(brightness);
  };

  return (
    <>
      {/* Ambient and point lights */}
      <ambientLight intensity={0.3} />
      <pointLight position={[0, 5, 5]} intensity={1} color="#00ffff" />
      <pointLight position={[-5, -3, 3]} intensity={0.5} color="#ff00ff" />
      <pointLight position={[5, -3, 3]} intensity={0.5} color="#ffff00" />

      {/* Arena walls */}
      <mesh position={[-ARENA_W / 2 - 0.1, 0, 0]}>
        <boxGeometry args={[0.15, ARENA_H, 0.5]} />
        <meshStandardMaterial color="#0ff" emissive="#0ff" emissiveIntensity={0.5} transparent opacity={0.6} />
      </mesh>
      <mesh position={[ARENA_W / 2 + 0.1, 0, 0]}>
        <boxGeometry args={[0.15, ARENA_H, 0.5]} />
        <meshStandardMaterial color="#0ff" emissive="#0ff" emissiveIntensity={0.5} transparent opacity={0.6} />
      </mesh>
      <mesh position={[0, ARENA_H / 2 + 0.1, 0]}>
        <boxGeometry args={[ARENA_W + 0.3, 0.15, 0.5]} />
        <meshStandardMaterial color="#0ff" emissive="#0ff" emissiveIntensity={0.5} transparent opacity={0.6} />
      </mesh>

      {/* Arena floor grid */}
      <gridHelper args={[ARENA_W, 16, '#0ff', '#112']} position={[0, -ARENA_H / 2, -0.3]} rotation={[Math.PI / 2, 0, 0]} />

      {/* Paddle */}
      <mesh ref={paddleRef} position={[0, -4.2, 0]}>
        <boxGeometry args={[1, 0.25, 0.4]} />
        <meshStandardMaterial color="#0ff" emissive="#0ff" emissiveIntensity={0.8} metalness={0.9} roughness={0.1} />
      </mesh>

      {/* Ball */}
      <mesh ref={ballRef} position={[0, -3.5, 0]}>
        <sphereGeometry args={[BALL_R, 16, 16]} />
        <meshStandardMaterial color="#fff" emissive="#ff00ff" emissiveIntensity={1.5} metalness={0.3} roughness={0.2} />
      </mesh>

      {/* Bricks */}
      {bricks.map(brick => (
        <mesh
          key={brick.id}
          position={brick.position}
          ref={ref => {
            if (ref) brickRefs.current.set(brick.id, ref);
            else brickRefs.current.delete(brick.id);
          }}
        >
          <boxGeometry args={[1, 0.4, 0.3]} />
          <meshStandardMaterial
            color={getBrickColor(brick) as any}
            emissive={brick.emissive}
            emissiveIntensity={brick.isBonus ? 1.2 : 0.4 * (brick.health / brick.maxHealth)}
            metalness={brick.isBonus ? 0.8 : 0.5}
            roughness={brick.isBonus ? 0.1 : 0.3}
            transparent={brick.isBonus}
            opacity={brick.isBonus ? 0.85 : 1}
          />
        </mesh>
      ))}

      {/* Background plane */}
      <mesh position={[0, 0, -1]}>
        <planeGeometry args={[20, 20]} />
        <meshStandardMaterial color="#050510" />
      </mesh>
    </>
  );
}
