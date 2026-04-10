import { Canvas } from '@react-three/fiber';
import GameScene from './GameScene';
import GameUI from './GameUI';
import MainMenu from './MainMenu';
import { useBrickBreaker } from './useBrickBreaker';

export default function BrickBreakerGame() {
  const {
    gameState, bricks, showMenu, powerUpMessage,
    ballPos, ballVel, paddleX,
    startGame, nextLevel, hitBrick, loseLife, backToMenu,
  } = useBrickBreaker();

  return (
    <div className="relative w-full h-screen bg-background overflow-hidden select-none">
      {/* 3D Canvas */}
      <Canvas
        camera={{ position: [0, 0, 9], fov: 55 }}
        className="!absolute inset-0"
      >
        <GameScene
          bricks={bricks}
          paddleX={paddleX}
          ballPos={ballPos}
          ballVel={ballVel}
          paddleWidth={gameState.paddleWidth}
          isPlaying={gameState.isPlaying}
          onHitBrick={hitBrick}
          onLoseLife={loseLife}
          onLevelClear={nextLevel}
        />
      </Canvas>

      {/* HUD */}
      {gameState.isPlaying && (
        <GameUI gameState={gameState} powerUpMessage={powerUpMessage} />
      )}

      {/* Menu */}
      {(showMenu || gameState.gameOver) && (
        <MainMenu
          onStart={startGame}
          gameOver={gameState.gameOver}
          finalScore={gameState.gameOver ? gameState.score : undefined}
        />
      )}
    </div>
  );
}
