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


def synth_startup_rising() -> list[float]:
    """Smooth, warm rising startup chime for Adara Box cold boot (780ms)."""
    duration_s = 0.78
    n = int(SAMPLE_RATE * duration_s)
    out = [0.0] * n

    # Ascending warm major-9th arpeggio notes fading cleanly upward
    # (start_s, freq_hz, weight, decay_s)
    partials = [
        (0.00, 261.63, 0.30, 0.42),  # C4 anchor
        (0.07, 329.63, 0.32, 0.40),  # E4
        (0.14, 392.00, 0.35, 0.38),  # G4
        (0.22, 493.88, 0.34, 0.36),  # B4
        (0.30, 587.33, 0.38, 0.38),  # D5 shimmer
        (0.38, 783.99, 0.24, 0.34),  # G5 airy top
    ]

    for start_s, base_freq, weight, decay_s in partials:
        start_idx = int(start_s * SAMPLE_RATE)
        for i in range(start_idx, n):
            t = (i - start_idx) / SAMPLE_RATE
            attack = 1.0 - math.exp(-t / 0.018)
            # Subtle upward pitch glide in the first 90ms for a rising feel
            glide = base_freq * (0.97 + 0.03 * (1.0 - math.exp(-t / 0.045)))
            fundamental = math.sin(2.0 * math.pi * glide * t)
            octave_halo = 0.22 * math.sin(2.0 * math.pi * glide * 2.0 * t) * math.exp(-t / 0.20)
            env = attack * math.exp(-t / decay_s)
            out[i] += weight * env * (fundamental + octave_halo)

    # Gentle master tail fade-out in final 120ms to guarantee zero click at EOF
    tail_samples = int(0.12 * SAMPLE_RATE)
    for k in range(tail_samples):
        idx = n - tail_samples + k
        if 0 <= idx < n:
            out[idx] *= 0.5 * (1.0 + math.cos(math.pi * k / tail_samples))

    peak = max((abs(s) for s in out), default=1.0)
    if peak > 0.0:
        norm = 0.72 / peak
        out = [s * norm for s in out]
    return out


def main() -> None:
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    sounds_dir = os.path.join(root, "public", "sounds")
    presets = {
        "ui-click-crisp.wav": synth_crisp_click(),
        "ui-click-soft.wav": synth_soft_pop(),
        "ui-click-mechanical.wav": synth_mechanical_tick(),
        "ui-startup-rising.wav": synth_startup_rising(),
    }
    for filename, samples in presets.items():
        out_path = os.path.join(sounds_dir, filename)
        write_wav(out_path, samples)
        print(f"Generated {out_path} ({len(samples)} samples)")


if __name__ == "__main__":
    main()
