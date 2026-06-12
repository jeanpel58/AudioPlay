# AudioPlay - Guide des thèmes de couleurs

## Vue d'ensemble

AudioPlay intègre maintenant un système complet de gestion des thèmes de couleurs qui vous permet de :

✅ **Sélectionner** parmi plusieurs thèmes préinstallés  
✅ **Créer** vos propres thèmes personnalisés  
✅ **Enregistrer** vos thèmes avec un titre descriptif  
✅ **Partager** vos thèmes avec d'autres utilisateurs  
✅ **Supprimer** les thèmes dont vous n'avez plus besoin  
✅ **Distribuer** l'application avec des thèmes préconfigurés  

---

## Accès rapide

### Pour l'utilisateur final
👉 Ouvrez **Paramètres** → **Couleurs de l'interface**

### Pour le développeur/distributeur
👉 Consultez `AudioPlay\Themes\README.md` pour la distribution de thèmes

---

## Architecture du système

### Stockage des thèmes

Les thèmes sont stockés à deux endroits :

1. **Thèmes préinstallés** (distribution) :
   ```
   [Répertoire de l'application]\Themes\*.theme
   ```

2. **Thèmes utilisateur** (runtime) :
   ```
   %AppData%\AudioPlay\Themes\*.theme
   ```

### Premier lancement

Au premier démarrage d'AudioPlay :
1. Le thème **"Par défaut"** est automatiquement créé et activé
2. Les thèmes préinstallés sont copiés depuis `[App]\Themes\` vers `%AppData%\AudioPlay\Themes\`
3. L'utilisateur peut immédiatement choisir parmi tous les thèmes disponibles

### Format des fichiers `.theme`

Chaque thème est un simple fichier texte contenant des paires clé=valeur :

```ini
FormBackColor=#ADD8E6
ControlBackColor=#ADD8E6
ControlForeColor=#000000
ButtonBackColor=#ADD8E6
ButtonForeColor=#000000
ListViewBackColor=#E0FFFF
ListViewForeColor=#000000
ListViewHeaderBackColor=#07C0FE
ListViewSelectionBackColor=#0078D7
ListViewSelectionForeColor=#FFFFFF
TextBoxBackColor=#E0FFFF
TextBoxForeColor=#000000
GroupBoxForeColor=#000000
TrackBarBackColor=#E0FFFF
```

---

## Interface utilisateur

### Contrôles disponibles dans FormParametres

| Contrôle | Fonction |
|----------|----------|
| **ComboBox "Sélectionner un thème"** | Liste déroulante de tous les thèmes disponibles |
| **Bouton "Enr. sous..."** | Enregistre le thème actuel sous un nouveau nom |
| **Bouton "Supprimer"** | Supprime le thème sélectionné (sauf "Par défaut") |
| **Bouton "Personnaliser..."** | Ouvre l'éditeur de couleurs détaillé |
| **Bouton "Réinitialiser"** | Restaure le thème "Par défaut" |

### Workflow de création de thème

```
1. Ouvrir Paramètres
   ↓
2. Cliquer "Personnaliser..."
   ↓
3. Modifier les couleurs élément par élément
   ↓
4. Cliquer "Appliquer" pour prévisualiser
   ↓
5. Cliquer "OK" pour valider
   ↓
6. Cliquer "Enr. sous..." pour sauvegarder
   ↓
7. Entrer un nom de thème
   ↓
8. Le thème est ajouté à la liste
```

---

## Couleurs par défaut

Le thème **"Par défaut"** utilise les couleurs suivantes :

| Élément | Couleur | Hex |
|---------|---------|-----|
| Fond formulaires | LightBlue | `#ADD8E6` |
| Fond contrôles | LightBlue | `#ADD8E6` |
| Fond boutons | LightBlue | `#ADD8E6` |
| Fond ListView | LightCyan | `#E0FFFF` |
| Fond TextBox | LightCyan | `#E0FFFF` |
| Fond TrackBar | LightCyan | `#E0FFFF` |
| En-tête ListView | Cyan personnalisé | `#07C0FE` (RGB: 7, 192, 254) |

Ces couleurs s'appliquent automatiquement :
- À **toutes les fenêtres** de l'application
- Aux **formulaires dynamiques** (métadonnées, à propos, etc.)
- Aux **contrôles propriétaires** (ListView en-têtes)

---

## Thèmes préinstallés

### 1. Par défaut
🎨 **Bleu clair apaisant**  
Le thème original d'AudioPlay, conçu pour être agréable et reposant.

### 2. Sombre
🌙 **Mode nuit**  
Fond gris foncé (#2C2C2C) avec texte blanc, parfait pour les environnements sombres.

### 3. Soleil
☀️ **Jaune vif**  
Tons jaunes et dorés (#FFFACD, #FFD700) pour une interface énergique.

### 4. Océan
🌊 **Bleu ciel**  
Nuances de bleu clair (#E8F4F8, #87CEEB) inspirées de l'océan.

### 5. Automne
🍂 **Doré chaud**  
Couleurs automnales (#F0E68C, #DAA520) évoquant les feuilles d'automne.

---

## API développeur

### ThemeManager - Méthodes principales

```vb
' Obtenir le thème par défaut
Dim theme As ThemeColors = ThemeManager.GetDefaultTheme()

' Obtenir le nom du thème courant
Dim name As String = ThemeManager.GetCurrentThemeName()

' Obtenir tous les thèmes disponibles
Dim themes As List(Of String) = ThemeManager.GetAvailableThemes()

' Charger un thème par son nom
Dim theme As ThemeColors = ThemeManager.LoadNamedTheme("Sombre")

' Sauvegarder un thème
ThemeManager.SaveNamedTheme("Mon thème", theme)

' Définir le thème courant
ThemeManager.SetCurrentTheme("Mon thème", theme)

' Supprimer un thème
ThemeManager.DeleteTheme("Mon thème")

' Réinitialiser au thème par défaut
ThemeManager.ResetThemeToDefault()

' Appliquer un thème à un formulaire
ThemeManager.ApplyThemeToForm(Me)
```

### Structure ThemeColors

```vb
Public Class ThemeColors
	Public Property FormBackColor As Color
	Public Property ControlBackColor As Color
	Public Property ControlForeColor As Color
	Public Property ButtonBackColor As Color
	Public Property ButtonForeColor As Color
	Public Property ListViewBackColor As Color
	Public Property ListViewForeColor As Color
	Public Property ListViewHeaderBackColor As Color
	Public Property ListViewSelectionBackColor As Color
	Public Property ListViewSelectionForeColor As Color
	Public Property TextBoxBackColor As Color
	Public Property TextBoxForeColor As Color
	Public Property GroupBoxForeColor As Color
	Public Property TrackBarBackColor As Color
End Class
```

---

## Distribution et déploiement

### Pour inclure des thèmes personnalisés dans la distribution

1. **Créez vos fichiers `.theme`** dans le dossier `AudioPlay\Themes\` du projet
2. Les fichiers sont **automatiquement copiés** lors de la compilation (configuré dans `.vbproj`)
3. Au premier lancement, les thèmes sont **copiés vers %AppData%**
4. L'utilisateur peut les **utiliser immédiatement**

### Configuration projet (.vbproj)

```xml
<ItemGroup>
  <None Update="Themes\*.theme">
	<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## Protection du thème par défaut

Le thème **"Par défaut"** est protégé et ne peut pas être :
- ❌ Supprimé
- ❌ Remplacé par un thème du même nom
- ✅ Toujours présent en première position dans la liste
- ✅ Automatiquement recréé s'il est manquant

---

## Localisation

Toutes les chaînes de l'interface de gestion des thèmes sont localisables via `Resources.resx` :

| Clé | Français (défaut) |
|-----|-------------------|
| `Theme_Group` | Couleurs de l'interface |
| `Theme_Select` | Sélectionner un thème : |
| `Theme_SaveAs` | Enr. sous... |
| `Theme_Delete` | Supprimer |
| `Theme_Customize` | Personnaliser... |
| `Theme_Reset` | Réinitialiser |
| `Theme_SaveDialog_Title` | Enregistrer le thème |
| `Theme_SaveDialog_Prompt` | Entrez un nom pour ce thème : |
| `Theme_DeleteConfirm` | Voulez-vous vraiment supprimer le thème "{0}" ? |
| `Theme_DeleteConfirm_Title` | Confirmer la suppression |

---

## Compatibilité et migration

### Migration depuis l'ancien système

L'ancien fichier `theme.txt` (sans nom de thème) est automatiquement migré :
1. Au premier lancement avec le nouveau système
2. Le contenu est lu et converti en thème nommé
3. Le thème par défaut est créé
4. L'ancien fichier reste pour compatibilité ascendante

### Rétrocompatibilité

Les applications existantes migreront automatiquement sans perte de données :
- ✅ Ancien `theme.txt` → converti en thème nommé
- ✅ Pas de thème → création automatique du thème par défaut
- ✅ Thèmes préinstallés copiés uniquement s'ils n'existent pas déjà

---

## Dépannage

### Les thèmes ne s'affichent pas dans la liste
- Vérifiez que les fichiers `.theme` sont dans `%AppData%\AudioPlay\Themes\`
- Relancez l'application
- Vérifiez l'extension (`.theme` et non `.theme.txt`)

### Un thème ne s'applique pas correctement
- Ouvrez le fichier `.theme` et vérifiez la syntaxe
- Les couleurs doivent être au format `#RRGGBB`
- Réinitialisez au thème par défaut et recréez le thème

### Impossible de supprimer un thème
- Le thème "Par défaut" ne peut pas être supprimé
- Vérifiez que vous n'essayez pas de supprimer le thème actuellement actif

---

## Fichiers et dossiers

```
AudioPlay/
├── Themes/                      # Thèmes préinstallés (distribution)
│   ├── Sombre.theme
│   ├── Soleil.theme
│   ├── Océan.theme
│   ├── Automne.theme
│   └── README.md
│
├── ThemeManager.vb              # Moteur de gestion des thèmes
├── FormParametres.vb            # Interface de gestion
├── FormParametres.Designer.vb   # Contrôles UI
├── Resources.resx               # Chaînes localisées
└── THEMES_GUIDE.md             # Ce fichier

%AppData%\AudioPlay/
├── Themes/                      # Thèmes utilisateur (runtime)
│   ├── Par défaut.theme         # Créé automatiquement
│   ├── Sombre.theme             # Copié depuis distribution
│   ├── Soleil.theme             # Copié depuis distribution
│   ├── Océan.theme              # Copié depuis distribution
│   ├── Automne.theme            # Copié depuis distribution
│   └── [thèmes personnalisés]   # Créés par l'utilisateur
│
└── current_theme.txt            # Nom du thème actuel
```

---

## Changelog

### Version actuelle (2026-05-29)
✨ **Nouveau** : Système complet de gestion des thèmes nommés  
✨ **Nouveau** : 4 thèmes préinstallés (Sombre, Soleil, Océan, Automne)  
✨ **Nouveau** : Interface de sélection/sauvegarde/suppression dans Paramètres  
✨ **Nouveau** : Protection du thème "Par défaut"  
✨ **Nouveau** : Support de la distribution de thèmes préfabriqués  
✨ **Nouveau** : Migration automatique depuis l'ancien système  
✨ **Nouveau** : Copie automatique des thèmes préinstallés au premier lancement  

---

## Auteur et support

Pour toute question ou suggestion concernant le système de thèmes :
- 📧 Ouvrez une issue sur le dépôt du projet
- 📝 Consultez la documentation complète dans `THEMES_GUIDE.md`
- 🎨 Partagez vos thèmes personnalisés avec la communauté !

---

**Profitez pleinement de votre expérience AudioPlay personnalisée ! 🎵🎨**
