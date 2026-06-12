import codecs

# Lire le fichier
with open('Resources.resx', 'r', encoding='utf-8') as f:
    content = f.read()

# Corrections d'encodage
replacements = {
    'Ã©': 'é',
    'Ã¨': 'è',
    'Ã ': 'à',
    'Ã´': 'ô',
    'Ã®': 'î',
    'Ã»': 'û',
    'Ã§': 'ç',
    'Ã‰': 'É',
    'Ãˆ': 'È',
    'Ã€': 'À'
}

for bad, good in replacements.items():
    content = content.replace(bad, good)

# Sauvegarder avec UTF-8 sans BOM
with open('Resources.resx', 'w', encoding='utf-8') as f:
    f.write(content)

print("✅ Resources.resx corrigé")
