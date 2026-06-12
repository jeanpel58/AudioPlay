# 🔬 DIAGNOSTIC : Pourquoi le SYNC se perd rapidement

## 🎯 Hypothèses à tester

### **Hypothèse 1 : BeatSyncEngine ajoute du drift au lieu de le corriger**
Le BeatSyncEngine détecte un faux drift et applique des corrections qui empirent les choses.

**Test :**
1. Désactiver temporairement les corrections du BeatSyncEngine
2. Garder seulement le snap initial
3. Observer si les beats restent alignés plus longtemps

### **Hypothèse 2 : Pitch de Deck A change pendant la lecture**
Si l'utilisateur ou un autre processus change le pitch de Deck A après SYNC, cela casse la synchronisation.

**Test :**
1. Vérifier dans les logs si `pitchDeckA` change pendant la lecture
2. S'assurer que les BeatGrids sont mises à jour si le pitch change

### **Hypothèse 3 : BPM détecté initial est imprécis**
Le BPM initial (117.000) n'est peut-être pas exact (pourrait être 116.987), ce qui crée un ratio légèrement faux.

**Test :**
1. Afficher le BPM détecté avec plus de décimales (5 au lieu de 3)
2. Recalculer le ratio avec cette précision

### **Hypothèse 4 : TimeStretchProvider de NAudio dérive**
Le `TimeStretchSampleProvider` pourrait avoir une imprécision interne qui accumule du drift.

**Test :**
1. Mesurer la position réelle toutes les secondes
2. Comparer avec la position théorique
3. Calculer l'écart cumulatif

---

## 🧪 Test simple : Désactiver BeatSyncEngine

### **Modification temporaire dans ButtonSyncDeckB_Click**

Commenter les lignes qui activent le BeatSyncEngine :

```vb
' === ÉTAPE 3 : ACTIVER LE BEAT LOCK CONTINU (maintien de la synchronisation) ===
If beatSyncEngine IsNot Nothing Then
	' ⚠️ TEST : DÉSACTIVER les corrections continues
	' beatSyncEngine.InitialiserBeatGrids(...)
	' beatSyncEngine.SyncActifDeckB = True
	Debug.WriteLine($"[TEST] BeatSyncEngine DÉSACTIVÉ - Sync manuel seulement")
End If
```

**Résultat attendu :**
- Si le drift disparaît → Le BeatSyncEngine cause le problème
- Si le drift persiste → Le problème est dans le calcul initial du pitch ou dans TimeStretch

---

## 🔍 Investigation : Vérifier la précision du BPM détecté

### **Question clé :** Le BPM de 117.000 est-il vraiment 117.000 ?

La plupart des détecteurs BPM ont une précision de ±0.1 BPM. Si le vrai BPM est 116.987 :
- Ratio calculé : 120 / 117.000 = 1.025641
- Ratio réel nécessaire : 120 / 116.987 = 1.025752
- **Différence : 0.011% → 10-15 ms de drift sur 10 beats**

### **Solution :** Augmenter la précision du BPM détecté

Dans `DetecterBPMDeckA()` et `DetecterBPMDeckB()` :

```vb
' Au lieu de :
bpmDeckA = CSng(bpm)  ' Single = 7 chiffres significatifs

' Utiliser :
bpmDeckA = bpm  ' Double = 15 chiffres significatifs
```

Et changer les variables :
```vb
Private bpmDeckA As Double = 0.0  ' ✅ Déjà Double
Private bpmDeckB As Double = 0.0  ' ✅ Déjà Double
```

**C'est déjà fait !** Donc ce n'est pas ça.

---

## 🎵 Investigation : TimeStretchProvider précision

### **Le problème potentiel**

`TimeStretchSampleProvider` utilise SoundTouch en interne, qui peut avoir une légère imprécision dans le tempo.

**Test de précision :**

Ajouter dans `timerPosition_Tick` :

```vb
If beatSyncEngine.SyncActifDeckB Then
	' Calculer la position théorique
	Dim tempsEcoule As Double = (DateTime.Now - tempsDepartSync).TotalSeconds
	Dim positionTheorique As Double = positionDepartSyncB + (tempsEcoule * tempoBaseDeckB)

	' Position réelle
	Dim positionReelle As Double = fichierAudioDeckB.CurrentTime.TotalSeconds

	' Écart
	Dim ecart As Double = positionReelle - positionTheorique

	If Math.Abs(ecart) > 0.050 Then ' > 50ms
		Debug.WriteLine($"[DRIFT DÉTECTÉ] Écart={ecart * 1000:F1}ms, Réel={positionReelle:F3}s, Théorique={positionTheorique:F3}s")
	End If
End If
```

---

## 💡 Solution probable : BeatSyncEngine trop agressif

Les paramètres actuels du BeatSyncEngine :

```vb
Private driftTolerance As Double = 0.015        ' 15ms - seuil pour correction
Private driftDeadZone As Double = 0.008         ' 8ms - zone morte
Private driftMinimal As Double = 0.003          ' 3ms - drift minimal
Private Const TEMPO_SMOOTH_FACTOR As Single = 0.4F  ' 40% réactivité
```

**Ces valeurs sont TROP AGRESSIVES pour un BPM sync !**

En effet :
- Une variation de **3 ms** déclenche une détection
- Une correction est appliquée dès **8 ms**
- La correction à **40%** change le tempo rapidement

### **Solution : Paramètres plus tolérants**

```vb
Private driftTolerance As Double = 0.030        ' 30ms - seuil (au lieu de 15ms)
Private driftDeadZone As Double = 0.015         ' 15ms - zone morte (au lieu de 8ms)
Private driftMinimal As Double = 0.008          ' 8ms - drift minimal (au lieu de 3ms)
Private Const TEMPO_SMOOTH_FACTOR As Single = 0.2F  ' 20% réactivité (au lieu de 40%)
```

**Explication :**
- Ignorer les micro-variations < 8ms (bruit de mesure)
- Ne corriger que si drift > 15ms
- Appliquer la correction dès drift > 30ms
- Correction plus douce (20% au lieu de 40%)

---

## 🎯 Plan d'action

1. **Test 1 :** Ajuster les paramètres du BeatSyncEngine (plus tolérant)
2. **Test 2 :** Si ça ne suffit pas, désactiver complètement BeatSyncEngine
3. **Test 3 :** Si le drift persiste sans BeatSyncEngine, le problème est dans le pitch/tempo de base
4. **Test 4 :** Vérifier la précision de TimeStretchProvider avec le test de position théorique

---

Voulez-vous que j'applique le **Test 1** (ajuster les paramètres du BeatSyncEngine) ?
