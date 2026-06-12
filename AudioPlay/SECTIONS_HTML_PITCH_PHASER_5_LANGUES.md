# Sections HTML pour Pitch Shift et Phaser - 5 Langues

Ces sections doivent être ajoutées dans les guides AudioPlay dans chaque langue respective, après la section "Changement de tempo" (Time Stretch).

---

## 🇫🇷 Français (fr) - Pour AUDIOPLAY_GUIDE_COMPLET.fr.html

```html
		<h3>🎹 Changement de tonalité (Pitch Shift)</h3>
		<p>Le <strong>Pitch Shift</strong> permet de modifier la tonalité de la piste audio sans changer son tempo.</p>

		<h4>📋 Paramètres</h4>
		<ul>
			<li><strong>☑️ Activer</strong> : Case à cocher pour activer/désactiver l'effet</li>
			<li><strong>🎵 Tonalité (demi-tons)</strong> : De -12 à +12 demi-tons
				<ul>
					<li><strong>Valeurs négatives</strong> : Baisse la tonalité (plus grave)</li>
					<li><strong>0</strong> : Tonalité originale</li>
					<li><strong>Valeurs positives</strong> : Augmente la tonalité (plus aigu)</li>
					<li><strong>±12 demi-tons = ±1 octave</strong></li>
				</ul>
			</li>
			<li><strong>🔄 Bouton Réinitialiser</strong> : Remet la tonalité à 0</li>
		</ul>

		<h4>💡 Utilisations courantes</h4>
		<ul>
			<li>Adapter une chanson à votre tessiture vocale pour le karaoké</li>
			<li>Créer des effets vocaux (chipmunk avec +5, voix grave avec -5)</li>
			<li>Transposer une chanson dans une autre tonalité</li>
			<li>Pratiquer un instrument à différentes hauteurs</li>
		</ul>

		<h4>⚠️ Notes importantes</h4>
		<ul>
			<li>Le changement de tonalité peut légèrement dégrader la qualité audio</li>
			<li>Les changements extrêmes (±10-12 demi-tons) peuvent créer des artefacts</li>
			<li>Le tempo reste inchangé (utilisez Time Stretch pour modifier le tempo)</li>
		</ul>

		<hr>

		<h3>🌊 Phaser (effet spatial)</h3>
		<p>Le <strong>Phaser</strong> est un effet de modulation qui crée un son spatial, "tournoyant" ou "swooshing", très utilisé dans les années 70-80.</p>

		<h4>📋 Paramètres</h4>
		<ul>
			<li><strong>☑️ Activer</strong> : Case à cocher pour activer/désactiver l'effet</li>
			<li><strong>🔄 Vitesse (Hz)</strong> : 0.1 à 10 Hz - Vitesse de modulation
				<ul>
					<li><strong>0.1-0.5 Hz</strong> : Effet lent, subtil</li>
					<li><strong>0.5-2 Hz</strong> : Effet classique, équilibré</li>
					<li><strong>2-10 Hz</strong> : Effet rapide, intense</li>
				</ul>
			</li>
			<li><strong>📊 Profondeur</strong> : 0% à 100% - Intensité de l'effet
				<ul>
					<li><strong>20-40%</strong> : Effet subtil</li>
					<li><strong>50-70%</strong> : Effet notable</li>
					<li><strong>80-100%</strong> : Effet prononcé</li>
				</ul>
			</li>
			<li><strong>🔁 Résonance (Feedback)</strong> : 0% à 95% - Amplification du son traité
				<ul>
					<li><strong>0-30%</strong> : Son doux</li>
					<li><strong>40-70%</strong> : Son "psychédélique" classique</li>
					<li><strong>80-95%</strong> : Son très métallique (attention au volume!)</li>
				</ul>
			</li>
			<li><strong>🎚️ Mélange (Mix)</strong> : 0% à 100% - Balance sec/effet
				<ul>
					<li><strong>0%</strong> : Son original uniquement</li>
					<li><strong>30-50%</strong> : Mix équilibré (recommandé)</li>
					<li><strong>100%</strong> : Effet pur</li>
				</ul>
			</li>
			<li><strong>🎛️ Étages (Stages)</strong> : 2, 4, 6, 8 ou 12 - Complexité de l'effet
				<ul>
					<li><strong>2-4</strong> : Son doux, vintage</li>
					<li><strong>6-8</strong> : Son riche, standard</li>
					<li><strong>12</strong> : Son très complexe</li>
				</ul>
			</li>
			<li><strong>🔄 Bouton Réinitialiser</strong> : Restaure les valeurs par défaut</li>
		</ul>

		<h4>💡 Préréglages suggérés</h4>
		<table border="1" cellpadding="5" style="border-collapse: collapse; width: 100%;">
			<tr style="background-color: #e8f4f8;">
				<th>Style</th>
				<th>Vitesse</th>
				<th>Profondeur</th>
				<th>Feedback</th>
				<th>Mix</th>
				<th>Stages</th>
			</tr>
			<tr>
				<td><strong>Subtil Vintage</strong></td>
				<td>0.3 Hz</td>
				<td>30%</td>
				<td>20%</td>
				<td>30%</td>
				<td>4</td>
			</tr>
			<tr>
				<td><strong>Psychédélique 70s</strong></td>
				<td>0.5 Hz</td>
				<td>70%</td>
				<td>60%</td>
				<td>50%</td>
				<td>6</td>
			</tr>
			<tr>
				<td><strong>Moderne Intensif</strong></td>
				<td>1.0 Hz</td>
				<td>60%</td>
				<td>40%</td>
				<td>40%</td>
				<td>8</td>
			</tr>
			<tr>
				<td><strong>Effet Spatial</strong></td>
				<td>0.2 Hz</td>
				<td>80%</td>
				<td>50%</td>
				<td>45%</td>
				<td>12</td>
			</tr>
		</table>

		<h4>🎵 Utilisations courantes</h4>
		<ul>
			<li>Recréer le son des synthétiseurs analogiques des années 70-80</li>
			<li>Ajouter de la profondeur et du mouvement à une guitare électrique</li>
			<li>Créer des ambiances psychédéliques ou spatiales</li>
			<li>Enrichir des nappes de synthétiseurs</li>
		</ul>

		<h4>⚠️ Notes importantes</h4>
		<ul>
			<li>Un feedback élevé (>80%) peut devenir très intense - commencez doucement</li>
			<li>Combinez avec Reverb pour un effet spatial maximal</li>
			<li>Plus d'étages = son plus complexe mais plus de charge CPU</li>
			<li>Le phaser fonctionne mieux sur des sons riches en harmoniques (guitares, synthés)</li>
		</ul>
```

---

## 🇬🇧 English (en) - For AUDIOPLAY_GUIDE_COMPLET.en.html

```html
		<h3>🎹 Pitch Shift (Change Pitch)</h3>
		<p><strong>Pitch Shift</strong> allows you to modify the pitch of the audio track without changing its tempo.</p>

		<h4>📋 Parameters</h4>
		<ul>
			<li><strong>☑️ Enable</strong>: Checkbox to activate/deactivate the effect</li>
			<li><strong>🎵 Pitch (semitones)</strong>: From -12 to +12 semitones
				<ul>
					<li><strong>Negative values</strong>: Lower the pitch (deeper)</li>
					<li><strong>0</strong>: Original pitch</li>
					<li><strong>Positive values</strong>: Raise the pitch (higher)</li>
					<li><strong>±12 semitones = ±1 octave</strong></li>
				</ul>
			</li>
			<li><strong>🔄 Reset Button</strong>: Resets pitch to 0</li>
		</ul>

		<h4>💡 Common Uses</h4>
		<ul>
			<li>Adapt a song to your vocal range for karaoke</li>
			<li>Create vocal effects (chipmunk with +5, deep voice with -5)</li>
			<li>Transpose a song to another key</li>
			<li>Practice an instrument at different pitches</li>
		</ul>

		<h4>⚠️ Important Notes</h4>
		<ul>
			<li>Pitch shifting may slightly degrade audio quality</li>
			<li>Extreme changes (±10-12 semitones) may create artifacts</li>
			<li>Tempo remains unchanged (use Time Stretch to modify tempo)</li>
		</ul>

		<hr>

		<h3>🌊 Phaser (spatial effect)</h3>
		<p>The <strong>Phaser</strong> is a modulation effect that creates a spatial, "swirling" or "swooshing" sound, widely used in the 70s-80s.</p>

		<h4>📋 Parameters</h4>
		<ul>
			<li><strong>☑️ Enable</strong>: Checkbox to activate/deactivate the effect</li>
			<li><strong>🔄 Rate (Hz)</strong>: 0.1 to 10 Hz - Modulation speed
				<ul>
					<li><strong>0.1-0.5 Hz</strong>: Slow, subtle effect</li>
					<li><strong>0.5-2 Hz</strong>: Classic, balanced effect</li>
					<li><strong>2-10 Hz</strong>: Fast, intense effect</li>
				</ul>
			</li>
			<li><strong>📊 Depth</strong>: 0% to 100% - Effect intensity
				<ul>
					<li><strong>20-40%</strong>: Subtle effect</li>
					<li><strong>50-70%</strong>: Noticeable effect</li>
					<li><strong>80-100%</strong>: Pronounced effect</li>
				</ul>
			</li>
			<li><strong>🔁 Resonance (Feedback)</strong>: 0% to 95% - Processed sound amplification
				<ul>
					<li><strong>0-30%</strong>: Soft sound</li>
					<li><strong>40-70%</strong>: Classic "psychedelic" sound</li>
					<li><strong>80-95%</strong>: Very metallic sound (watch your volume!)</li>
				</ul>
			</li>
			<li><strong>🎚️ Mix</strong>: 0% to 100% - Dry/wet balance
				<ul>
					<li><strong>0%</strong>: Original sound only</li>
					<li><strong>30-50%</strong>: Balanced mix (recommended)</li>
					<li><strong>100%</strong>: Pure effect</li>
				</ul>
			</li>
			<li><strong>🎛️ Stages</strong>: 2, 4, 6, 8 or 12 - Effect complexity
				<ul>
					<li><strong>2-4</strong>: Soft, vintage sound</li>
					<li><strong>6-8</strong>: Rich, standard sound</li>
					<li><strong>12</strong>: Very complex sound</li>
				</ul>
			</li>
			<li><strong>🔄 Reset Button</strong>: Restores default values</li>
		</ul>

		<h4>💡 Suggested Presets</h4>
		<table border="1" cellpadding="5" style="border-collapse: collapse; width: 100%;">
			<tr style="background-color: #e8f4f8;">
				<th>Style</th>
				<th>Rate</th>
				<th>Depth</th>
				<th>Feedback</th>
				<th>Mix</th>
				<th>Stages</th>
			</tr>
			<tr>
				<td><strong>Subtle Vintage</strong></td>
				<td>0.3 Hz</td>
				<td>30%</td>
				<td>20%</td>
				<td>30%</td>
				<td>4</td>
			</tr>
			<tr>
				<td><strong>Psychedelic 70s</strong></td>
				<td>0.5 Hz</td>
				<td>70%</td>
				<td>60%</td>
				<td>50%</td>
				<td>6</td>
			</tr>
			<tr>
				<td><strong>Modern Intensive</strong></td>
				<td>1.0 Hz</td>
				<td>60%</td>
				<td>40%</td>
				<td>40%</td>
				<td>8</td>
			</tr>
			<tr>
				<td><strong>Spatial Effect</strong></td>
				<td>0.2 Hz</td>
				<td>80%</td>
				<td>50%</td>
				<td>45%</td>
				<td>12</td>
			</tr>
		</table>

		<h4>🎵 Common Uses</h4>
		<ul>
			<li>Recreate the sound of 70s-80s analog synthesizers</li>
			<li>Add depth and movement to an electric guitar</li>
			<li>Create psychedelic or spatial atmospheres</li>
			<li>Enrich synthesizer pads</li>
		</ul>

		<h4>⚠️ Important Notes</h4>
		<ul>
			<li>High feedback (>80%) can become very intense - start gently</li>
			<li>Combine with Reverb for maximum spatial effect</li>
			<li>More stages = more complex sound but higher CPU load</li>
			<li>Phaser works best on harmonic-rich sounds (guitars, synths)</li>
		</ul>
```

---

## 🇪🇸 Español (es) - Para AUDIOPLAY_GUIDE_COMPLET.es.html

```html
		<h3>🎹 Cambio de Tono (Pitch Shift)</h3>
		<p>El <strong>Pitch Shift</strong> permite modificar el tono de la pista de audio sin cambiar su tempo.</p>

		<h4>📋 Parámetros</h4>
		<ul>
			<li><strong>☑️ Activar</strong>: Casilla para activar/desactivar el efecto</li>
			<li><strong>🎵 Tono (semitonos)</strong>: De -12 a +12 semitonos
				<ul>
					<li><strong>Valores negativos</strong>: Bajan el tono (más grave)</li>
					<li><strong>0</strong>: Tono original</li>
					<li><strong>Valores positivos</strong>: Suben el tono (más agudo)</li>
					<li><strong>±12 semitonos = ±1 octava</strong></li>
				</ul>
			</li>
			<li><strong>🔄 Botón Restablecer</strong>: Restablece el tono a 0</li>
		</ul>

		<h4>💡 Usos Comunes</h4>
		<ul>
			<li>Adaptar una canción a tu rango vocal para karaoke</li>
			<li>Crear efectos vocales (chipmunk con +5, voz grave con -5)</li>
			<li>Transponer una canción a otra tonalidad</li>
			<li>Practicar un instrumento en diferentes tonos</li>
		</ul>

		<h4>⚠️ Notas Importantes</h4>
		<ul>
			<li>El cambio de tono puede degradar ligeramente la calidad de audio</li>
			<li>Los cambios extremos (±10-12 semitonos) pueden crear artefactos</li>
			<li>El tempo permanece sin cambios (usa Time Stretch para modificar el tempo)</li>
		</ul>

		<hr>

		<h3>🌊 Phaser (efecto espacial)</h3>
		<p>El <strong>Phaser</strong> es un efecto de modulación que crea un sonido espacial, "giratorio" o "swooshing", muy utilizado en los años 70-80.</p>

		<h4>📋 Parámetros</h4>
		<ul>
			<li><strong>☑️ Activar</strong>: Casilla para activar/desactivar el efecto</li>
			<li><strong>🔄 Velocidad (Hz)</strong>: 0.1 a 10 Hz - Velocidad de modulación
				<ul>
					<li><strong>0.1-0.5 Hz</strong>: Efecto lento, sutil</li>
					<li><strong>0.5-2 Hz</strong>: Efecto clásico, equilibrado</li>
					<li><strong>2-10 Hz</strong>: Efecto rápido, intenso</li>
				</ul>
			</li>
			<li><strong>📊 Profundidad</strong>: 0% a 100% - Intensidad del efecto
				<ul>
					<li><strong>20-40%</strong>: Efecto sutil</li>
					<li><strong>50-70%</strong>: Efecto notable</li>
					<li><strong>80-100%</strong>: Efecto pronunciado</li>
				</ul>
			</li>
			<li><strong>🔁 Resonancia (Feedback)</strong>: 0% a 95% - Amplificación del sonido procesado
				<ul>
					<li><strong>0-30%</strong>: Sonido suave</li>
					<li><strong>40-70%</strong>: Sonido "psicodélico" clásico</li>
					<li><strong>80-95%</strong>: Sonido muy metálico (¡cuidado con el volumen!)</li>
				</ul>
			</li>
			<li><strong>🎚️ Mezcla (Mix)</strong>: 0% a 100% - Balance seco/efecto
				<ul>
					<li><strong>0%</strong>: Solo sonido original</li>
					<li><strong>30-50%</strong>: Mezcla equilibrada (recomendado)</li>
					<li><strong>100%</strong>: Efecto puro</li>
				</ul>
			</li>
			<li><strong>🎛️ Etapas (Stages)</strong>: 2, 4, 6, 8 o 12 - Complejidad del efecto
				<ul>
					<li><strong>2-4</strong>: Sonido suave, vintage</li>
					<li><strong>6-8</strong>: Sonido rico, estándar</li>
					<li><strong>12</strong>: Sonido muy complejo</li>
				</ul>
			</li>
			<li><strong>🔄 Botón Restablecer</strong>: Restaura los valores predeterminados</li>
		</ul>

		<h4>💡 Preajustes Sugeridos</h4>
		<table border="1" cellpadding="5" style="border-collapse: collapse; width: 100%;">
			<tr style="background-color: #e8f4f8;">
				<th>Estilo</th>
				<th>Velocidad</th>
				<th>Profundidad</th>
				<th>Feedback</th>
				<th>Mix</th>
				<th>Stages</th>
			</tr>
			<tr>
				<td><strong>Sutil Vintage</strong></td>
				<td>0.3 Hz</td>
				<td>30%</td>
				<td>20%</td>
				<td>30%</td>
				<td>4</td>
			</tr>
			<tr>
				<td><strong>Psicodélico 70s</strong></td>
				<td>0.5 Hz</td>
				<td>70%</td>
				<td>60%</td>
				<td>50%</td>
				<td>6</td>
			</tr>
			<tr>
				<td><strong>Moderno Intensivo</strong></td>
				<td>1.0 Hz</td>
				<td>60%</td>
				<td>40%</td>
				<td>40%</td>
				<td>8</td>
			</tr>
			<tr>
				<td><strong>Efecto Espacial</strong></td>
				<td>0.2 Hz</td>
				<td>80%</td>
				<td>50%</td>
				<td>45%</td>
				<td>12</td>
			</tr>
		</table>

		<h4>🎵 Usos Comunes</h4>
		<ul>
			<li>Recrear el sonido de los sintetizadores analógicos de los años 70-80</li>
			<li>Añadir profundidad y movimiento a una guitarra eléctrica</li>
			<li>Crear ambientes psicodélicos o espaciales</li>
			<li>Enriquecer pads de sintetizador</li>
		</ul>

		<h4>⚠️ Notas Importantes</h4>
		<ul>
			<li>Un feedback alto (>80%) puede volverse muy intenso - comienza suavemente</li>
			<li>Combina con Reverb para un efecto espacial máximo</li>
			<li>Más etapas = sonido más complejo pero mayor carga de CPU</li>
			<li>El phaser funciona mejor en sonidos ricos en armónicos (guitarras, sintetizadores)</li>
		</ul>
```

---

## 🇩🇪 Deutsch (de) - Für AUDIOPLAY_GUIDE_COMPLET.de.html

```html
		<h3>🎹 Tonhöhenverschiebung (Pitch Shift)</h3>
		<p>Der <strong>Pitch Shift</strong> ermöglicht es, die Tonhöhe der Audiospur zu ändern, ohne das Tempo zu verändern.</p>

		<h4>📋 Parameter</h4>
		<ul>
			<li><strong>☑️ Aktivieren</strong>: Kontrollkästchen zum Aktivieren/Deaktivieren des Effekts</li>
			<li><strong>🎵 Tonhöhe (Halbtöne)</strong>: Von -12 bis +12 Halbtöne
				<ul>
					<li><strong>Negative Werte</strong>: Senken die Tonhöhe (tiefer)</li>
					<li><strong>0</strong>: Originaltonhöhe</li>
					<li><strong>Positive Werte</strong>: Erhöhen die Tonhöhe (höher)</li>
					<li><strong>±12 Halbtöne = ±1 Oktave</strong></li>
				</ul>
			</li>
			<li><strong>🔄 Zurücksetzen-Taste</strong>: Setzt die Tonhöhe auf 0 zurück</li>
		</ul>

		<h4>💡 Häufige Verwendungen</h4>
		<ul>
			<li>Ein Lied an Ihren Stimmumfang für Karaoke anpassen</li>
			<li>Stimmeffekte erstellen (Chipmunk mit +5, tiefe Stimme mit -5)</li>
			<li>Ein Lied in eine andere Tonart transponieren</li>
			<li>Ein Instrument in verschiedenen Tonhöhen üben</li>
		</ul>

		<h4>⚠️ Wichtige Hinweise</h4>
		<ul>
			<li>Die Tonhöhenverschiebung kann die Audioqualität leicht beeinträchtigen</li>
			<li>Extreme Änderungen (±10-12 Halbtöne) können Artefakte erzeugen</li>
			<li>Das Tempo bleibt unverändert (verwenden Sie Time Stretch, um das Tempo zu ändern)</li>
		</ul>

		<hr>

		<h3>🌊 Phaser (Raumeffekt)</h3>
		<p>Der <strong>Phaser</strong> ist ein Modulationseffekt, der einen räumlichen, "wirbelnden" oder "rauschenden" Klang erzeugt, der in den 70er-80er Jahren weit verbreitet war.</p>

		<h4>📋 Parameter</h4>
		<ul>
			<li><strong>☑️ Aktivieren</strong>: Kontrollkästchen zum Aktivieren/Deaktivieren des Effekts</li>
			<li><strong>🔄 Rate (Hz)</strong>: 0,1 bis 10 Hz - Modulationsgeschwindigkeit
				<ul>
					<li><strong>0,1-0,5 Hz</strong>: Langsamer, subtiler Effekt</li>
					<li><strong>0,5-2 Hz</strong>: Klassischer, ausgewogener Effekt</li>
					<li><strong>2-10 Hz</strong>: Schneller, intensiver Effekt</li>
				</ul>
			</li>
			<li><strong>📊 Tiefe</strong>: 0% bis 100% - Effektintensität
				<ul>
					<li><strong>20-40%</strong>: Subtiler Effekt</li>
					<li><strong>50-70%</strong>: Deutlicher Effekt</li>
					<li><strong>80-100%</strong>: Ausgeprägter Effekt</li>
				</ul>
			</li>
			<li><strong>🔁 Resonanz (Feedback)</strong>: 0% bis 95% - Verstärkung des verarbeiteten Klangs
				<ul>
					<li><strong>0-30%</strong>: Weicher Klang</li>
					<li><strong>40-70%</strong>: Klassischer "psychedelischer" Klang</li>
					<li><strong>80-95%</strong>: Sehr metallischer Klang (Vorsicht mit der Lautstärke!)</li>
				</ul>
			</li>
			<li><strong>🎚️ Mischung (Mix)</strong>: 0% bis 100% - Trocken/Nass-Balance
				<ul>
					<li><strong>0%</strong>: Nur Originalklang</li>
					<li><strong>30-50%</strong>: Ausgewogene Mischung (empfohlen)</li>
					<li><strong>100%</strong>: Reiner Effekt</li>
				</ul>
			</li>
			<li><strong>🎛️ Stufen (Stages)</strong>: 2, 4, 6, 8 oder 12 - Effektkomplexität
				<ul>
					<li><strong>2-4</strong>: Weicher, Vintage-Klang</li>
					<li><strong>6-8</strong>: Reichhaltiger, Standard-Klang</li>
					<li><strong>12</strong>: Sehr komplexer Klang</li>
				</ul>
			</li>
			<li><strong>🔄 Zurücksetzen-Taste</strong>: Stellt Standardwerte wieder her</li>
		</ul>

		<h4>💡 Vorgeschlagene Voreinstellungen</h4>
		<table border="1" cellpadding="5" style="border-collapse: collapse; width: 100%;">
			<tr style="background-color: #e8f4f8;">
				<th>Stil</th>
				<th>Rate</th>
				<th>Tiefe</th>
				<th>Feedback</th>
				<th>Mix</th>
				<th>Stages</th>
			</tr>
			<tr>
				<td><strong>Subtil Vintage</strong></td>
				<td>0,3 Hz</td>
				<td>30%</td>
				<td>20%</td>
				<td>30%</td>
				<td>4</td>
			</tr>
			<tr>
				<td><strong>Psychedelisch 70er</strong></td>
				<td>0,5 Hz</td>
				<td>70%</td>
				<td>60%</td>
				<td>50%</td>
				<td>6</td>
			</tr>
			<tr>
				<td><strong>Modern Intensiv</strong></td>
				<td>1,0 Hz</td>
				<td>60%</td>
				<td>40%</td>
				<td>40%</td>
				<td>8</td>
			</tr>
			<tr>
				<td><strong>Raumeffekt</strong></td>
				<td>0,2 Hz</td>
				<td>80%</td>
				<td>50%</td>
				<td>45%</td>
				<td>12</td>
			</tr>
		</table>

		<h4>🎵 Häufige Verwendungen</h4>
		<ul>
			<li>Den Klang von analogen Synthesizern aus den 70er-80er Jahren nachbilden</li>
			<li>Tiefe und Bewegung zu einer E-Gitarre hinzufügen</li>
			<li>Psychedelische oder räumliche Atmosphären schaffen</li>
			<li>Synthesizer-Pads bereichern</li>
		</ul>

		<h4>⚠️ Wichtige Hinweise</h4>
		<ul>
			<li>Hohes Feedback (>80%) kann sehr intensiv werden - beginnen Sie sanft</li>
			<li>Kombinieren Sie mit Reverb für maximalen Raumeffekt</li>
			<li>Mehr Stufen = komplexerer Klang, aber höhere CPU-Belastung</li>
			<li>Phaser funktioniert am besten bei obertonreichen Klängen (Gitarren, Synthesizer)</li>
		</ul>
```

---

## 🇮🇹 Italiano (it) - Per AUDIOPLAY_GUIDE_COMPLET.it.html

```html
		<h3>🎹 Cambio di Tonalità (Pitch Shift)</h3>
		<p>Il <strong>Pitch Shift</strong> permette di modificare la tonalità della traccia audio senza cambiarne il tempo.</p>

		<h4>📋 Parametri</h4>
		<ul>
			<li><strong>☑️ Attiva</strong>: Casella di controllo per attivare/disattivare l'effetto</li>
			<li><strong>🎵 Tonalità (semitoni)</strong>: Da -12 a +12 semitoni
				<ul>
					<li><strong>Valori negativi</strong>: Abbassano la tonalità (più grave)</li>
					<li><strong>0</strong>: Tonalità originale</li>
					<li><strong>Valori positivi</strong>: Alzano la tonalità (più acuto)</li>
					<li><strong>±12 semitoni = ±1 ottava</strong></li>
				</ul>
			</li>
			<li><strong>🔄 Pulsante Ripristina</strong>: Ripristina la tonalità a 0</li>
		</ul>

		<h4>💡 Usi Comuni</h4>
		<ul>
			<li>Adattare una canzone alla tua estensione vocale per il karaoke</li>
			<li>Creare effetti vocali (chipmunk con +5, voce profonda con -5)</li>
			<li>Trasporre una canzone in un'altra tonalità</li>
			<li>Praticare uno strumento in diverse tonalità</li>
		</ul>

		<h4>⚠️ Note Importanti</h4>
		<ul>
			<li>Il cambio di tonalità può degradare leggermente la qualità audio</li>
			<li>I cambiamenti estremi (±10-12 semitoni) possono creare artefatti</li>
			<li>Il tempo rimane invariato (usa Time Stretch per modificare il tempo)</li>
		</ul>

		<hr>

		<h3>🌊 Phaser (effetto spaziale)</h3>
		<p>Il <strong>Phaser</strong> è un effetto di modulazione che crea un suono spaziale, "vorticoso" o "swooshing", ampiamente utilizzato negli anni 70-80.</p>

		<h4>📋 Parametri</h4>
		<ul>
			<li><strong>☑️ Attiva</strong>: Casella di controllo per attivare/disattivare l'effetto</li>
			<li><strong>🔄 Velocità (Hz)</strong>: 0,1 a 10 Hz - Velocità di modulazione
				<ul>
					<li><strong>0,1-0,5 Hz</strong>: Effetto lento, sottile</li>
					<li><strong>0,5-2 Hz</strong>: Effetto classico, equilibrato</li>
					<li><strong>2-10 Hz</strong>: Effetto veloce, intenso</li>
				</ul>
			</li>
			<li><strong>📊 Profondità</strong>: 0% a 100% - Intensità dell'effetto
				<ul>
					<li><strong>20-40%</strong>: Effetto sottile</li>
					<li><strong>50-70%</strong>: Effetto evidente</li>
					<li><strong>80-100%</strong>: Effetto pronunciato</li>
				</ul>
			</li>
			<li><strong>🔁 Risonanza (Feedback)</strong>: 0% a 95% - Amplificazione del suono elaborato
				<ul>
					<li><strong>0-30%</strong>: Suono morbido</li>
					<li><strong>40-70%</strong>: Suono "psichedelico" classico</li>
					<li><strong>80-95%</strong>: Suono molto metallico (attenzione al volume!)</li>
				</ul>
			</li>
			<li><strong>🎚️ Miscela (Mix)</strong>: 0% a 100% - Bilanciamento secco/effetto
				<ul>
					<li><strong>0%</strong>: Solo suono originale</li>
					<li><strong>30-50%</strong>: Mix equilibrato (consigliato)</li>
					<li><strong>100%</strong>: Effetto puro</li>
				</ul>
			</li>
			<li><strong>🎛️ Stadi (Stages)</strong>: 2, 4, 6, 8 o 12 - Complessità dell'effetto
				<ul>
					<li><strong>2-4</strong>: Suono morbido, vintage</li>
					<li><strong>6-8</strong>: Suono ricco, standard</li>
					<li><strong>12</strong>: Suono molto complesso</li>
				</ul>
			</li>
			<li><strong>🔄 Pulsante Ripristina</strong>: Ripristina i valori predefiniti</li>
		</ul>

		<h4>💡 Preset Suggeriti</h4>
		<table border="1" cellpadding="5" style="border-collapse: collapse; width: 100%;">
			<tr style="background-color: #e8f4f8;">
				<th>Stile</th>
				<th>Velocità</th>
				<th>Profondità</th>
				<th>Feedback</th>
				<th>Mix</th>
				<th>Stages</th>
			</tr>
			<tr>
				<td><strong>Sottile Vintage</strong></td>
				<td>0,3 Hz</td>
				<td>30%</td>
				<td>20%</td>
				<td>30%</td>
				<td>4</td>
			</tr>
			<tr>
				<td><strong>Psichedelico 70s</strong></td>
				<td>0,5 Hz</td>
				<td>70%</td>
				<td>60%</td>
				<td>50%</td>
				<td>6</td>
			</tr>
			<tr>
				<td><strong>Moderno Intensivo</strong></td>
				<td>1,0 Hz</td>
				<td>60%</td>
				<td>40%</td>
				<td>40%</td>
				<td>8</td>
			</tr>
			<tr>
				<td><strong>Effetto Spaziale</strong></td>
				<td>0,2 Hz</td>
				<td>80%</td>
				<td>50%</td>
				<td>45%</td>
				<td>12</td>
			</tr>
		</table>

		<h4>🎵 Usi Comuni</h4>
		<ul>
			<li>Ricreare il suono dei sintetizzatori analogici degli anni 70-80</li>
			<li>Aggiungere profondità e movimento a una chitarra elettrica</li>
			<li>Creare atmosfere psichedeliche o spaziali</li>
			<li>Arricchire pad di sintetizzatore</li>
		</ul>

		<h4>⚠️ Note Importanti</h4>
		<ul>
			<li>Un feedback elevato (>80%) può diventare molto intenso - inizia dolcemente</li>
			<li>Combina con Reverb per un effetto spaziale massimo</li>
			<li>Più stadi = suono più complesso ma maggiore carico CPU</li>
			<li>Il phaser funziona meglio su suoni ricchi di armoniche (chitarre, sintetizzatori)</li>
		</ul>
```

---

## 📝 Instructions d'intégration

Pour chaque fichier HTML (`AUDIOPLAY_GUIDE_COMPLET.{langue}.html`):

1. **Trouver la section "Changement de tempo" / "Time Stretch"** (elle se termine généralement par une balise `<hr>`)

2. **Insérer la section HTML correspondante** (Pitch Shift + Phaser) juste après

3. **Vérifier le menu de navigation** - Ajouter les liens dans le `<nav>` si nécessaire:
   - FR: `<li><a href="#pitch-shift">🎹 Pitch Shift</a></li>` et `<li><a href="#phaser">🌊 Phaser</a></li>`
   - EN: `<li><a href="#pitch-shift">🎹 Pitch Shift</a></li>` et `<li><a href="#phaser">🌊 Phaser</a></li>`
   - ES: `<li><a href="#pitch-shift">🎹 Pitch Shift</a></li>` et `<li><a href="#phaser">🌊 Phaser</a></li>`
   - DE: `<li><a href="#pitch-shift">🎹 Pitch Shift</a></li>` et `<li><a href="#phaser">🌊 Phaser</a></li>`
   - IT: `<li><a href="#pitch-shift">🎹 Pitch Shift</a></li>` et `<li><a href="#phaser">🌊 Phaser</a></li>`

4. **Vérifier la cohérence** du style avec le reste du document HTML

---

## ✅ Récapitulatif complet

### Fichiers .resx modifiés
- ✅ `AudioPlay/Resources.resx` (FR)
- ✅ `AudioPlay/Resources.en.resx` (EN)
- ✅ `AudioPlay/Resources.es.resx` (ES)
- ✅ `AudioPlay/Resources.de.resx` (DE)
- ✅ `AudioPlay/Resources.it.resx` (IT)

### Code VB.NET modifié
- ✅ `AudioPlay/FormParametres.vb` - Méthode `RefreshLanguage()` mise à jour

### Documentation HTML à mettre à jour manuellement
- ⏳ `AudioPlay/AUDIOPLAY_GUIDE_COMPLET.fr.html`
- ⏳ `AudioPlay/AUDIOPLAY_GUIDE_COMPLET.en.html`
- ⏳ `AudioPlay/AUDIOPLAY_GUIDE_COMPLET.es.html`
- ⏳ `AudioPlay/AUDIOPLAY_GUIDE_COMPLET.de.html`
- ⏳ `AudioPlay/AUDIOPLAY_GUIDE_COMPLET.it.html`

Les sections HTML complètes sont fournies ci-dessus pour chaque langue.
