import { useState } from 'react';
import { IQ_CONFIGS } from './types';

interface MainMenuProps {
  onStart: (iqLevel: number) => void;
  gameOver?: boolean;
  finalScore?: number;
}

export default function MainMenu({ onStart, gameOver, finalScore }: MainMenuProps) {
  const [selectedIQ, setSelectedIQ] = useState(2);

  return (
    <div className="absolute inset-0 z-20 flex items-center justify-center bg-background/90 backdrop-blur-md">
      <div className="flex flex-col items-center gap-8 max-w-md w-full px-6">
        {/* Title */}
        <div className="text-center">
          <h1 className="font-display text-5xl md:text-6xl font-black tracking-wider text-glow-cyan text-primary">
            BRICK
          </h1>
          <h1 className="font-display text-5xl md:text-6xl font-black tracking-wider text-glow-magenta text-accent">
            BREAKER
          </h1>
          <p className="font-display text-sm tracking-[0.3em] text-muted-foreground mt-2">
            3D EDITION
          </p>
        </div>

        {gameOver && (
          <div className="text-center">
            <p className="font-display text-xl text-destructive">GAME OVER</p>
            <p className="font-display text-3xl text-foreground text-glow-cyan mt-1">
              {finalScore?.toLocaleString()} PTS
            </p>
          </div>
        )}

        {/* IQ Level Selector */}
        <div className="w-full">
          <p className="font-display text-sm tracking-widest text-muted-foreground mb-3 text-center uppercase">
            Select Difficulty (IQ Level)
          </p>
          <div className="flex flex-col gap-2">
            {Object.entries(IQ_CONFIGS).map(([key, config]) => {
              const level = Number(key);
              const isSelected = selectedIQ === level;
              return (
                <button
                  key={key}
                  onClick={() => setSelectedIQ(level)}
                  className={`
                    w-full py-3 px-4 rounded-lg border font-body text-base font-semibold
                    transition-all duration-200 cursor-pointer
                    ${isSelected
                      ? 'border-primary bg-primary/15 text-primary box-glow-cyan'
                      : 'border-border bg-muted/30 text-muted-foreground hover:border-muted-foreground'
                    }
                  `}
                >
                  <div className="flex justify-between items-center">
                    <span>{config.label}</span>
                    <span className="text-xs opacity-60">
                      Speed: {'●'.repeat(level)}{'○'.repeat(5 - level)}
                    </span>
                  </div>
                </button>
              );
            })}
          </div>
        </div>

        {/* Start Button */}
        <button
          onClick={() => onStart(selectedIQ)}
          className="
            w-full py-4 rounded-xl font-display text-xl font-bold tracking-widest uppercase
            bg-primary text-primary-foreground box-glow-cyan
            hover:scale-105 active:scale-95 transition-transform duration-150
            cursor-pointer
          "
        >
          {gameOver ? 'PLAY AGAIN' : 'START GAME'}
        </button>
      </div>
    </div>
  );
}
