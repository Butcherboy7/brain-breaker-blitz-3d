import { useState, useCallback, useRef } from 'react';
import { Brick, GameState, IQ_CONFIGS, BONUS_COLORS } from './types';

const BRICK_COLORS = ['#0ff', '#f0f', '#ff0', '#0f0', '#f60', '#06f'];

function generateBricks(level: number, iqLevel: number): Brick[] {
  const config = IQ_CONFIGS[iqLevel];
  const rows = Math.min(config.rows + Math.floor(level / 2), 8);
  const cols = Math.min(config.cols + Math.floor(level / 3), 10);
  const bricks: Brick[] = [];
  const bonusTypes: Array<'multiball' | 'wide_paddle' | 'extra_life' | 'score_x2' | 'slow_ball'> =
    ['multiball', 'wide_paddle', 'extra_life', 'score_x2', 'slow_ball'];

  const startX = -(cols - 1) * 1.1 / 2;
  const startY = 4;

  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      const isBonus = Math.random() < config.bonusChance;
      const bonusType = isBonus ? bonusTypes[Math.floor(Math.random() * bonusTypes.length)] : undefined;
      const health = isBonus ? 1 : config.brickStrength + Math.floor(r / 2);
      const colorIdx = r % BRICK_COLORS.length;

      bricks.push({
        id: `${r}-${c}`,
        position: [startX + c * 1.1, startY - r * 0.55, 0],
        health,
        maxHealth: health,
        isBonus,
        bonusType,
        color: isBonus ? BONUS_COLORS[bonusType!].color : BRICK_COLORS[colorIdx],
        emissive: isBonus ? BONUS_COLORS[bonusType!].emissive : BRICK_COLORS[colorIdx],
      });
    }
  }
  return bricks;
}

export function useBrickBreaker() {
  const [gameState, setGameState] = useState<GameState>({
    score: 0, lives: 3, level: 1, iqLevel: 2,
    ballSpeed: IQ_CONFIGS[2].ballSpeed, paddleWidth: 2,
    isPlaying: false, gameOver: false, won: false,
    activePowerUps: [], scoreMultiplier: 1,
  });
  const [bricks, setBricks] = useState<Brick[]>([]);
  const [showMenu, setShowMenu] = useState(true);
  const [powerUpMessage, setPowerUpMessage] = useState('');

  const ballPos = useRef<[number, number, number]>([0, -3.5, 0]);
  const ballVel = useRef<[number, number]>([0.04, 0.08]);
  const paddleX = useRef(0);

  const startGame = useCallback((iqLevel: number) => {
    const config = IQ_CONFIGS[iqLevel];
    const newBricks = generateBricks(1, iqLevel);
    setBricks(newBricks);
    ballPos.current = [0, -3.5, 0];
    ballVel.current = [config.ballSpeed * 0.5, config.ballSpeed];
    paddleX.current = 0;
    setGameState({
      score: 0, lives: 3, level: 1, iqLevel,
      ballSpeed: config.ballSpeed, paddleWidth: 2,
      isPlaying: true, gameOver: false, won: false,
      activePowerUps: [], scoreMultiplier: 1,
    });
    setShowMenu(false);
    setPowerUpMessage('');
  }, []);

  const nextLevel = useCallback(() => {
    setGameState(prev => {
      const newLevel = prev.level + 1;
      const config = IQ_CONFIGS[prev.iqLevel];
      const speedMult = 1 + (newLevel - 1) * 0.1;
      const newBricks = generateBricks(newLevel, prev.iqLevel);
      setBricks(newBricks);
      ballPos.current = [0, -3.5, 0];
      ballVel.current = [config.ballSpeed * speedMult * 0.5, config.ballSpeed * speedMult];
      return {
        ...prev,
        level: newLevel,
        ballSpeed: config.ballSpeed * speedMult,
        paddleWidth: 2,
        isPlaying: true,
        activePowerUps: [],
        scoreMultiplier: 1,
      };
    });
  }, []);

  const hitBrick = useCallback((id: string) => {
    setBricks(prev => {
      const brick = prev.find(b => b.id === id);
      if (!brick) return prev;

      if (brick.health <= 1) {
        if (brick.isBonus && brick.bonusType) {
          applyPowerUp(brick.bonusType);
        }
        setGameState(gs => ({
          ...gs,
          score: gs.score + (brick.isBonus ? 50 : 10) * gs.scoreMultiplier,
        }));
        return prev.filter(b => b.id !== id);
      }

      return prev.map(b =>
        b.id === id ? { ...b, health: b.health - 1 } : b
      );
    });
  }, []);

  const applyPowerUp = useCallback((type: string) => {
    const label = BONUS_COLORS[type]?.label || type;
    setPowerUpMessage(label);
    setTimeout(() => setPowerUpMessage(''), 2000);

    setGameState(prev => {
      switch (type) {
        case 'extra_life':
          return { ...prev, lives: prev.lives + 1 };
        case 'wide_paddle':
          setTimeout(() => setGameState(g => ({ ...g, paddleWidth: 2 })), 8000);
          return { ...prev, paddleWidth: 3.5 };
        case 'score_x2':
          setTimeout(() => setGameState(g => ({ ...g, scoreMultiplier: 1 })), 10000);
          return { ...prev, scoreMultiplier: 2 };
        case 'slow_ball': {
          const origSpeed = prev.ballSpeed;
          ballVel.current = [ballVel.current[0] * 0.5, ballVel.current[1] * 0.5];
          setTimeout(() => {
            ballVel.current = [
              Math.sign(ballVel.current[0]) * origSpeed * 0.5,
              Math.sign(ballVel.current[1]) * origSpeed,
            ];
          }, 6000);
          return prev;
        }
        default:
          return prev;
      }
    });
  }, []);

  const loseLife = useCallback(() => {
    setGameState(prev => {
      if (prev.lives <= 1) {
        return { ...prev, lives: 0, isPlaying: false, gameOver: true };
      }
      ballPos.current = [0, -3.5, 0];
      const config = IQ_CONFIGS[prev.iqLevel];
      const speedMult = 1 + (prev.level - 1) * 0.1;
      ballVel.current = [config.ballSpeed * speedMult * 0.5, config.ballSpeed * speedMult];
      return { ...prev, lives: prev.lives - 1 };
    });
  }, []);

  const backToMenu = useCallback(() => {
    setShowMenu(true);
    setGameState(prev => ({ ...prev, isPlaying: false, gameOver: false }));
  }, []);

  return {
    gameState, bricks, showMenu, powerUpMessage,
    ballPos, ballVel, paddleX,
    startGame, nextLevel, hitBrick, loseLife, backToMenu,
    setBricks, setGameState,
  };
}
