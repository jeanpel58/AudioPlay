# 🚀 Améliorations de l'Installateur AudioPlay

## 📋 Résumé des modifications apportées au script Inno Setup

### ✅ 1. Installation automatique du .NET Desktop Runtime 8.0

**Problème résolu :** L'utilisateur recevait le message "You must install .NET Desktop Runtime to run this application"

**Solution :**
- Détection automatique de .NET Desktop Runtime 8.0+
- Téléchargement depuis les serveurs Microsoft (x64 et x86)
- Installation silencieuse avec `/install /quiet /norestart`
- Support multilingue pour les messages de progression

**Fichiers concernés :**
- `AudioPlay-2026-06-02-WithDotNet.iss` (nouvelle version)
- Section `[Code]` avec fonctions `IsDotNetInstalled()` et `PrepareToInstall()`

---

### ✅ 2. Détection et gestion des versions existantes

**Problème résolu :** Pas de vérification avant l'installation, risque d'écraser une version sans avertir

**Solution :**
- Détection de la version installée via le registre Windows
- Trois scénarios gérés :
  1. **Même version** → Message de confirmation pour réinstallation/réparation
  2. **Version plus récente** → Avertissement de downgrade
  3. **Version plus ancienne** → Confirmation de mise à jour

**Messages multilingues :**
- 🇫🇷 Français
- 🇬🇧 English
- 🇪🇸 Español
- 🇩🇪 Deutsch
- 🇮🇹 Italiano

---

### ✅ 3. Fermeture automatique d'AudioPlay avant installation

**Problème résolu :** Plantage lors de l'extraction des fichiers Python si AudioPlay est en cours d'exécution

**Solution :**
- Détection d'AudioPlay en cours d'exécution via `tasklist.exe`
- Message d'avertissement multilingue
- Fermeture automatique avec `taskkill.exe /F /IM AudioPlay.exe`
- Délai de 2 secondes pour laisser le processus se terminer proprement
- Possibilité d'annuler l'installation si l'utilisateur refuse

**Paramètres ajoutés dans `[Setup]` :**
```ini
CloseApplications=yes
CloseApplicationsFilter=*.exe,*.dll
RestartApplications=no
```

---

### ✅ 4. Protection des fichiers Python embarqués

**Problème résolu :** Erreur "Extraction des fichiers... verify.py" lors de l'installation

**Solution :**
- Ajout du flag `ignoreversion` pour forcer le remplacement des fichiers Python
- Ajout du flag `uninsneveruninstall` pour préserver Python lors de la désinstallation
- Fermeture préventive d'AudioPlay avant extraction

**Ligne modifiée dans `[Files]` :**
```ini
Source: "python_embedded\*"; 
DestDir: "{userappdata}\AudioPlay\python_embedded"; 
Flags: recursesubdirs createallsubdirs ignoreversion uninsneveruninstall
```

---

## 🔧 Détails techniques

### Ordre d'exécution lors de l'installation

1. **InitializeSetup()** - Avant tout
   - Fermeture d'AudioPlay s'il est en cours d'exécution
   - Détection de la version installée
   - Messages de confirmation selon le scénario
   - Vérification de .NET Desktop Runtime

2. **PrepareToInstall()** - Avant extraction des fichiers
   - Téléchargement de .NET si nécessaire
   - Installation silencieuse de .NET
   - Gestion des codes de retour (0=OK, 1638=déjà installé, 3010=redémarrage requis)

3. **Extraction des fichiers**
   - Remplacement forcé des fichiers avec `ignoreversion`
   - Aucun conflit grâce à la fermeture préalable d'AudioPlay

4. **Post-installation**
   - Création des associations de fichiers
   - Lancement optionnel d'AudioPlay

---

## 📦 Fichiers de l'installateur

### Version originale (conservée)
📄 `AudioPlay 2026-06-02.iss`
- Version simple sans vérifications
- Nécessite .NET préinstallé
- Pas de détection de version

### Nouvelle version (recommandée)
📄 `AudioPlay-2026-06-02-WithDotNet.iss`
- ✅ Installation .NET automatique
- ✅ Détection de version
- ✅ Fermeture automatique
- ✅ Protection des fichiers Python
- ✅ Support multilingue complet

---

## 🧪 Tests recommandés

### Test 1 : Installation sur machine sans .NET
1. Préparer une VM Windows sans .NET 8.0
2. Lancer l'installateur
3. Vérifier que .NET se télécharge et s'installe automatiquement
4. Vérifier qu'AudioPlay se lance correctement

### Test 2 : Installation avec AudioPlay en cours d'exécution
1. Lancer AudioPlay
2. Lancer l'installateur
3. Vérifier le message de fermeture automatique
4. Cliquer OK et vérifier qu'AudioPlay se ferme
5. Vérifier que l'installation se termine sans erreur

### Test 3 : Réinstallation de la même version
1. Installer AudioPlay 1.26.06.02
2. Relancer l'installateur
3. Vérifier le message "version déjà installée"
4. Choisir "Oui" et vérifier la réinstallation
5. Vérifier que les paramètres sont préservés

### Test 4 : Mise à jour depuis une version antérieure
1. Installer une version plus ancienne (simuler en changeant MyAppVersion)
2. Lancer le nouvel installateur
3. Vérifier le message de mise à jour
4. Confirmer et vérifier que l'installation se fait par-dessus

### Test 5 : Tentative de downgrade
1. Installer la version actuelle
2. Modifier MyAppVersion pour simuler une version antérieure
3. Vérifier l'avertissement de downgrade
4. Tester l'annulation et la continuation

---

## 🌐 Messages multilingues ajoutés

### Français
- "AudioPlay est actuellement en cours d'exécution..."
- "AudioPlay version X est déjà installé. Voulez-vous continuer..."
- "Téléchargement et installation de .NET 8.0 Desktop Runtime..."

### English
- "AudioPlay is currently running..."
- "AudioPlay version X is already installed. Do you want to continue..."
- "Downloading and installing .NET 8.0 Desktop Runtime..."

### Español
- "AudioPlay se está ejecutando actualmente..."
- "AudioPlay versión X ya está instalado. ¿Desea continuar..."
- "Descargando e instalando .NET 8.0 Desktop Runtime..."

### Deutsch
- "AudioPlay wird derzeit ausgeführt..."
- "AudioPlay Version X ist bereits installiert. Möchten Sie fortfahren..."
- "Herunterladen und Installieren von .NET 8.0 Desktop Runtime..."

### Italiano
- "AudioPlay è attualmente in esecuzione..."
- "AudioPlay versione X è già installato. Vuoi continuare..."
- "Download e installazione di .NET 8.0 Desktop Runtime..."

---

## ⚠️ Points d'attention

### Privilèges administrateur
L'installation nécessite des privilèges administrateur pour :
- Installer .NET Desktop Runtime
- Écrire dans `Program Files`
- Créer les associations de fichiers dans HKCU

### Connexion Internet
Le téléchargement de .NET nécessite une connexion Internet active.
Si pas de connexion, l'utilisateur recevra une erreur explicite.

### Taille du téléchargement
- .NET Desktop Runtime x64 : ~55 MB
- .NET Desktop Runtime x86 : ~48 MB

### Temps d'installation
- Installation normale : 1-2 minutes
- Avec téléchargement .NET : 5-10 minutes (selon connexion)

---

## 📝 Notes de version

**Version du script :** 1.26.06.02 (2024)

**Compatibilité :**
- Windows 10/11 (x64 et x86)
- .NET Desktop Runtime 8.0+
- Inno Setup 6.x

**Auteur :** Jean Pelletier

**Dernière mise à jour :** 2024

---

## 🔄 Prochaines améliorations possibles

1. **Mode offline** : Inclure .NET dans l'installateur pour installation sans Internet
2. **Détection de langue automatique** : Détecter la langue Windows pour pré-sélectionner
3. **Journal d'installation** : Créer un log détaillé dans `%TEMP%`
4. **Sauvegarde des paramètres** : Exporter les paramètres avant mise à jour
5. **Rollback automatique** : Restaurer l'ancienne version en cas d'échec

---

## 📞 Support

Pour tout problème d'installation :
1. Vérifier les logs Inno Setup dans `%TEMP%`
2. Vérifier que .NET 8.0+ est bien installé
3. Essayer de fermer manuellement AudioPlay avant installation
4. Exécuter l'installateur en mode administrateur

