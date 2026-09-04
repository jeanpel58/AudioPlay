#!/usr/bin/env python3
"""
Simple beat detector using librosa. Input: audio file path. Output: JSON to stdout:
{ "beats": [t1, t2, ...], "tempo": 123.45 }
Times are in seconds.

This script is intentionally minimal: it uses librosa.beat.beat_track and librosa.frames_to_time.
"""
import sys
import json

try:
    import librosa
except Exception as e:
    print(json.dumps({"error": "librosa not available", "detail": str(e)}))
    sys.exit(1)

if len(sys.argv) < 2:
    print(json.dumps({"error": "no input file"}))
    sys.exit(1)

audio_path = sys.argv[1]
try:
    y, sr = librosa.load(audio_path, sr=None, mono=True)
    tempo, beat_frames = librosa.beat.beat_track(y=y, sr=sr)
    beat_times = librosa.frames_to_time(beat_frames, sr=sr).tolist()
    out = {"beats": beat_times, "tempo": float(tempo)}
    print(json.dumps(out))
except Exception as e:
    print(json.dumps({"error": "processing failed", "detail": str(e)}))
    sys.exit(1)
