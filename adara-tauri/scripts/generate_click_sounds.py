#!/usr/bin/env python3
"""
Synthesizes crisp, low-latency UI click sound assets (.wav) for Adara Box.
Uses Python standard library only (wave, math, struct) so assets remain
reproducible and require zero external packages.
"""

from __future__ import annotations

import math
import os
import struct
import wave

SAMPLE_RATE = 44100


def write_wav(path: str, samples: list[float]) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "w") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(SAMPLE_RATE)
        frames = bytearray()
        for sample in samples:
            clamped = max(-1.0, min(1.0, sample))
            frames.extend(struct.pack("<h", int(clamped * 32767)))
        wf.writeframes(bytes(frames))


def synth_crisp_click() -> list[float]:
    """Short, modern tactile UI click (28ms)."""
    duration_s = 0.028
    n = int(SAMPLE_RATE * duration_s)
    out: list[float] = []
    for i in range(n):
        t = i / SAMPLE_RATE
        # Subtle high-mid transient + warm low body
        attack = 1.0 - math.exp(-t / 0.0005)
        body = math.sin(2.0 * math.pi * (1450.0 * math.exp(-t * 95.0) + 420.0) * t) * math.exp(-t / 0.0055)
        snap = math.sin(2.0 * math.pi * 3200.0 * t) * math.exp(-t / 0.0022) * 0.45
        env = attack * math.exp(-t / 0.008)
        out.append((body * 0.55 + snap * 0.35) * env * 0.75)
    return out


def synth_soft_pop() -> list[float]:
    """Gentle, rounded glass-tap pop (32ms)."""
    duration_s = 0.032
    n = int(SAMPLE_RATE * duration_s)
    out: list[float] = []
    for i in range(n):
        t = i / SAMPLE_RATE
        attack = 1.0 - math.exp(-t / 0.0009)
        freq = 540.0 + 480.0 * math.exp(-t / 0.007)
        tone = math.sin(2.0 * math.pi * freq * t)
        overtone = 0.25 * math.sin(2.0 * math.pi * freq * 2.02 * t) * math.exp(-t / 0.004)
        env = attack * math.exp(-t / 0.009)
        out.append((tone + overtone) * env * 0.55)
    return out


def synth_mechanical_tick() -> list[float]:
    """Clean mechanical key switch click (34ms)."""
    duration_s = 0.034
    n = int(SAMPLE_RATE * duration_s)
    out: list[float] = []
    for i in range(n):
        t = i / SAMPLE_RATE
        attack = 1.0 - math.exp(-t / 0.00035)
        click_a = math.sin(2.0 * math.pi * 2400.0 * t) * math.exp(-t / 0.0028) * 0.6
        thock = math.sin(2.0 * math.pi * 310.0 * t) * math.exp(-t / 0.0085) * 0.5
        # Slight release micro-bump around 9ms
        t2 = max(0.0, t - 0.009)
        click_b = (
            math.sin(2.0 * math.pi * 1950.0 * t2) * math.exp(-t2 / 0.0024) * 0.28
            if t >= 0.009
            else 0.0
        )
        out.append(attack * (click_a + thock + click_b) * 0.72)
    return out


def main() -> None:
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    sounds_dir = os.path.join(root, "public", "sounds")
    presets = {
        "ui-click-crisp.wav": synth_crisp_click(),
        "ui-click-soft.wav": synth_soft_pop(),
        "ui-click-mechanical.wav": synth_mechanical_tick(),
    }
    for filename, samples in presets.items():
        out_path = os.path.join(sounds_dir, filename)
        write_wav(out_path, samples)
        print(f"Generated {out_path} ({len(samples)} samples)")


if __name__ == "__main__":
    main()
