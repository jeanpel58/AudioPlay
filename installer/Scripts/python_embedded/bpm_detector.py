#!/usr/bin/env python
# -*- coding: utf-8 -*-
import sys
import librosa
import warnings

# Ignorer les avertissements
warnings.filterwarnings('ignore')

def detect_bpm(filepath):
    try:
        # Charger le fichier audio COMPLET (pas de limite de durée)
        y, sr = librosa.load(filepath, duration=None)  # None = fichier entier

        # Détecter le tempo sur tout le fichier
        tempo, beats = librosa.beat.beat_track(y=y, sr=sr)

        # Retourner le BPM avec 2 décimales
        print(f'{tempo:.2f}')
        return 0
    except Exception as e:
        print(f'ERROR: {str(e)}', file=sys.stderr)
        return 1

if __name__ == '__main__':
    if len(sys.argv) != 2:
        print('Usage: python bpm_detector.py <audio_file>', file=sys.stderr)
        sys.exit(1)

    sys.exit(detect_bpm(sys.argv[1]))
