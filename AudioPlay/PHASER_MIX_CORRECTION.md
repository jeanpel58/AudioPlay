# Correction du Mix du Phaser - "Son sous l'eau"

## Le Problème

Le phaser fonctionnait mais le chanteur sonnait "sous l'eau" car :
```vb
' PROBLÈME : Addition pure qui double le volume
output = dry + (wet * mix)
output = output * 0.7F  ' Atténuation globale
```

Cela créait :
- ❌ Volume global augmenté
- ❌ Perte de clarté
- ❌ Son étouffé/filtré
- ❌ Effet "sous l'eau"

## La Solution : Mix par Différence de Phase

### Formule Correcte
```vb
phasedSignal = wet - dry        ' Isoler la DIFFÉRENCE de phase
output = dry + (phasedSignal * mix)
```

Développée, cela donne :
```vb
output = dry + (wet - dry) * mix
output = dry * (1 - mix) + wet * mix
```

C'est la **formule standard** d'un wet/dry mix !

## Pourquoi Ça Fonctionne

### Avec Mix = 0.0 (0%)
```
output = dry * 1.0 + wet * 0.0 = dry
```
→ Signal original uniquement

### Avec Mix = 0.5 (50%)
```
output = dry * 0.5 + wet * 0.5
```
→ Moitié dry, moitié wet (équilibré)

### Avec Mix = 1.0 (100%)
```
output = dry * 0.0 + wet * 1.0 = wet
```
→ Signal phasé uniquement

## L'Effet Phaser Expliqué

1. **Signal original (dry)** : Voix normale
2. **Signal phasé (wet)** : Voix avec déphasage
3. **Différence (wet - dry)** : SEULEMENT le déphasage
4. **Mix** : Combien de déphasage ajouter

Le déphasage variable crée les notches caractéristiques du phaser **sans noyer le signal** !

## Comparaison Avant/Après

### Version Incorrecte (sous l'eau)
```vb
output = dry + (wet * mix)
output *= 0.7F
```
- ❌ Volume augmenté puis atténué
- ❌ Signal dry toujours à 100%
- ❌ Wet s'additionne par-dessus
- ❌ Perte de dynamique

### Version Correcte (claire)
```vb
phasedSignal = wet - dry
output = dry + (phasedSignal * mix)
```
- ✅ Volume constant
- ✅ Équilibre dry/wet contrôlé
- ✅ Effet audible sans étouffer
- ✅ Clarté préservée

## Ajustement du Mix par Défaut

**Ancien** : Mix = 0.5 (50%)  
**Nouveau** : Mix = 1.0 (100%)

Pourquoi ?
- Avec la nouvelle formule, Mix = 1.0 donne l'effet phaser complet
- Mix = 0.5 donnerait un effet trop subtil
- L'utilisateur peut réduire pour un effet plus doux

## Test d'Écoute

Maintenant vous devriez entendre :
- ✅ Voix claire et intelligible
- ✅ Effet phaser bien présent
- ✅ Notches audibles qui se promènent
- ✅ Pas d'effet "sous l'eau"
- ✅ Volume constant
- ✅ Son professionnel

## Paramètres Recommandés

### Effet Standard (par défaut)
- **Mix** : 1.0 (100% - effet complet)
- **Depth** : 1.0 (balayage complet)
- **Feedback** : 0.5 (résonance moyenne)
- **Rate** : 0.5 Hz (vitesse modérée)

### Effet Subtil
- **Mix** : 0.5 à 0.7
- **Depth** : 0.6
- **Feedback** : 0.3
- **Rate** : 0.3 Hz

### Effet Intense
- **Mix** : 1.0
- **Depth** : 1.0
- **Feedback** : 0.7 à 0.8
- **Rate** : 0.3 Hz (lent) ou 2.0 Hz (rapide)

## Conclusion

La formule `dry + (wet - dry) * mix` est la **formule standard** pour tous les effets audio avec wet/dry mix :
- Reverb
- Delay/Echo
- Chorus
- Flanger
- **Phaser**

Elle garantit :
- Volume constant
- Contrôle précis du dosage
- Pas de distorsion
- Son professionnel

Le phaser devrait maintenant être **clair, musical et professionnel** ! 🎵
