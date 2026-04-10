import { GameState, IQ_CONFIGS } from './types';

interface GameUIProps {
  gameState: GameState;
  powerUpMessage: string;
}

export default function GameUI({ gameState, powerUpMessage }: GameUIProps) {
  return (
    <div className="absolute inset-0 pointer-events-none z-10">
      {/* Top HUD */}
      <div className="flex justify-between items-start p-4">
        <div className="flex flex-col gap-1">
          <div className="font-display text-sm tracking-widest text-neon-cyan text-glow-cyan uppercase">
            Score
          </div>
          <div className="font-display text-3xl font-bold text-foreground text-glow-cyan">
            {gameState.score.toLocaleString()}
          </div>
        </div>

        <div className="flex flex-col items-center gap-1">
          <div className="font-display text-sm tracking-widest text-neon-magenta text-glow-magenta uppercase">
            Level {gameState.level}
          </div>
          <div className="font-display text-xs text-muted-foreground">
            {IQ_CONFIGS[gameState.iqLevel].label}
          </div>
        </div>

        <div className="flex flex-col items-end gap-1">
          <div className="font-display text-sm tracking-widest text-neon-cyan text-glow-cyan uppercase">
            Lives
          </div>
          <div className="flex gap-1">
            {Array.from({ length: gameState.lives }).map((_, i) => (
              <span key={i} className="text-xl">💎</span>
            ))}
          </div>
        </div>
      </div>

      {/* Score multiplier */}
      {gameState.scoreMultiplier > 1 && (
        <div className="absolute top-20 left-1/2 -translate-x-1/2 font-display text-lg text-neon-yellow animate-pulse">
          {gameState.scoreMultiplier}x SCORE
        </div>
      )}

      {/* Power-up message */}
      {powerUpMessage && (
        <div className="absolute top-1/3 left-1/2 -translate-x-1/2 font-display text-2xl text-foreground text-glow-magenta animate-bounce">
          {powerUpMessage}
        </div>
      )}
    </div>
  );
}
