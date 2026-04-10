export interface Brick {
  id: string;
  position: [number, number, number];
  health: number;
  maxHealth: number;
  isBonus: boolean;
  bonusType?: 'multiball' | 'wide_paddle' | 'extra_life' | 'score_x2' | 'slow_ball';
  color: string;
  emissive: string;
}

export interface GameState {
  score: number;
  lives: number;
  level: number;
  iqLevel: number;
  ballSpeed: number;
  paddleWidth: number;
  isPlaying: boolean;
  gameOver: boolean;
  won: boolean;
  activePowerUps: string[];
  scoreMultiplier: number;
}

export const IQ_CONFIGS: Record<number, { label: string; ballSpeed: number; brickStrength: number; rows: number; cols: number; bonusChance: number }> = {
  1: { label: 'Beginner (IQ 80)', ballSpeed: 0.08, brickStrength: 1, rows: 3, cols: 6, bonusChance: 0.3 },
  2: { label: 'Average (IQ 100)', ballSpeed: 0.10, brickStrength: 2, rows: 4, cols: 7, bonusChance: 0.25 },
  3: { label: 'Smart (IQ 120)', ballSpeed: 0.13, brickStrength: 3, rows: 5, cols: 8, bonusChance: 0.2 },
  4: { label: 'Genius (IQ 140)', ballSpeed: 0.16, brickStrength: 4, rows: 6, cols: 9, bonusChance: 0.15 },
  5: { label: 'Mastermind (IQ 160+)', ballSpeed: 0.20, brickStrength: 5, rows: 7, cols: 10, bonusChance: 0.1 },
};

export const BONUS_COLORS: Record<string, { color: string; emissive: string; label: string }> = {
  multiball: { color: '#ff00ff', emissive: '#ff00ff', label: '⚡ Multi-Ball' },
  wide_paddle: { color: '#00ff00', emissive: '#00ff00', label: '📏 Wide Paddle' },
  extra_life: { color: '#ff0000', emissive: '#ff0000', label: '❤️ Extra Life' },
  score_x2: { color: '#ffff00', emissive: '#ffff00', label: '✨ 2x Score' },
  slow_ball: { color: '#00ffff', emissive: '#00ffff', label: '🐢 Slow Ball' },
};
