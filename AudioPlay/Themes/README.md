# Distribution de thèmes AudioPlay

## Comment distribuer des thèmes personnalisés

Les thèmes AudioPlay sont stockés sous forme de fichiers `.theme` dans le dossier `Themes` de l'application.

### Structure d'un fichier de thème

Chaque fichier `.theme` contient une liste de propriétés de couleur au format :
```
NomPropriété=CouleurHexadécimale
```

Exemple :
```
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

### Pour inclure de nouveaux thèmes dans la distribution

1. Créez vos fichiers `.theme` dans le dossier `AudioPlay\Themes\` du projet
2. Nommez-les de manière descriptive (ex: `Sombre.theme`, `Océan.theme`)
3. Les fichiers seront automatiquement copiés lors de la compilation

### Pour les utilisateurs finaux

Au premier lancement d'AudioPlay, les thèmes préinstallés seront automatiquement copiés depuis le dossier `Themes` de l'application vers :
```
%AppData%\AudioPlay\Themes\
```

Les utilisateurs peuvent :
- Sélectionner un thème existant via le menu Paramètres > Couleurs de l'interface
- Personnaliser les couleurs et enregistrer leur propre thème
- Supprimer des thèmes personnalisés (le thème "Par défaut" ne peut pas être supprimé)

### Thèmes inclus

- **Par défaut** : Thème bleu clair par défaut d'AudioPlay
- **Sombre** : Thème sombre avec fond gris foncé
- **Soleil** : Thème jaune ensoleillé
- **Océan** : Thème bleu clair inspiré de l'océan
- **Automne** : Thème doré automnal

### Partage de thèmes entre utilisateurs

Pour partager un thème avec d'autres utilisateurs :
1. Copiez le fichier `.theme` depuis `%AppData%\AudioPlay\Themes\`
2. L'autre utilisateur place ce fichier dans son propre dossier `%AppData%\AudioPlay\Themes\`
3. Le thème apparaîtra dans la liste au prochain lancement d'AudioPlay
