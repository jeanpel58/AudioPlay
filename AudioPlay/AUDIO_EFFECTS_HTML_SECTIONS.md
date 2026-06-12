# Sections Effets Audio à ajouter aux guides HTML

## FRANÇAIS (AUDIOPLAY_GUIDE_COMPLET.fr.html)

### Ajouter dans le <nav> après la ligne des paramètres :
```html
<li><a href="#effets-audio">🎚️ Effets Audio</a></li>
```

### Ajouter après la section paramètres (avant fonctionnalités) :
```html
<!-- SECTION EFFETS AUDIO -->
<section id="effets-audio">
	<h2>🎚️ Effets Audio</h2>

	<div class="highlight">
		<h3>🎵 Modification en Temps Réel</h3>
		<p>Tous les effets audio peuvent être activés et ajustés <strong>pendant la lecture</strong> sans avoir besoin de redémarrer la chanson.</p>
	</div>

	<h3>🌊 Réverbération (Reverb)</h3>
	<p><strong>Description :</strong> Ajoute un effet d'écho spatial qui simule l'acoustique d'une salle.</p>
	<ul>
		<li><strong>Mix (0-100%) :</strong> Contrôle l'intensité de l'effet
			<ul>
				<li>0% = son original uniquement</li>
				<li>30% = équilibre recommandé</li>
				<li>100% = réverbération maximale</li>
			</ul>
		</li>
	</ul>
	<p><strong>Utilisations :</strong> Simuler un concert, donner de la profondeur, créer une ambiance de cathédrale.</p>

	<h3>🔊 Écho</h3>
	<p><strong>Description :</strong> Crée des répétitions du son avec des paramètres ajustables.</p>
	<ul>
		<li><strong>Mix (0-100%) :</strong> Volume de l'écho par rapport au signal original</li>
		<li><strong>Délai (50-2000 ms) :</strong> Temps entre le son original et sa répétition
			<ul>
				<li>50-200 ms = écho court, effet "slapback"</li>
				<li>300-500 ms = écho moyen, style rock classique</li>
				<li>800-2000 ms = écho long, effet spatial</li>
			</ul>
		</li>
		<li><strong>Feedback (0-100%) :</strong> Nombre de répétitions
			<ul>
				<li>0% = une seule répétition</li>
				<li>40% = quelques répétitions qui s'estompent</li>
				<li>90% = très nombreuses répétitions (attention!)</li>
			</ul>
		</li>
	</ul>
	<p><strong>Utilisations :</strong> Effet vocal, ambiance dub/reggae, créer de la profondeur.</p>

	<h3>🎹 Changement de Tonalité (Pitch Shift)</h3>
	<p><strong>Description :</strong> Modifie la hauteur tonale sans affecter la vitesse de lecture.</p>
	<ul>
		<li><strong>Demi-tons (-12 à +12) :</strong> Décalage en demi-tons
			<ul>
				<li>-12 = une octave plus bas</li>
				<li>0 = tonalité originale</li>
				<li>+12 = une octave plus haut</li>
				<li>±1 ou ±2 = correction de tonalité pour chanter</li>
			</ul>
		</li>
	</ul>
	<p><strong>Utilisations :</strong> Transposer pour s'adapter à sa voix, créer des effets "voix de souris" ou "voix grave".</p>
	<p><em>Note : L'implémentation actuelle est un placeholder simplifié. Un algorithme DSP avancé sera ajouté dans une version future.</em></p>

	<h3>⏱️ Changement de Tempo (Time Stretch)</h3>
	<p><strong>Description :</strong> Modifie la vitesse de lecture sans affecter la tonalité.</p>
	<ul>
		<li><strong>Vitesse (0.5x - 2.0x) :</strong> Ratio de vitesse
			<ul>
				<li>0.5x = deux fois plus lent</li>
				<li>1.0x = vitesse originale</li>
				<li>1.5x = 50% plus rapide</li>
				<li>2.0x = deux fois plus rapide</li>
			</ul>
		</li>
	</ul>
	<p><strong>Utilisations :</strong> Apprendre une chanson lentement, pratiquer un instrument, créer des remixes.</p>
	<p><em>Note : L'implémentation actuelle est un placeholder simplifié. Un algorithme DSP avancé sera ajouté dans une version future.</em></p>

	<h3>🔄 Workflow de Sauvegarde</h3>
	<div class="warning-box">
		<h4>💾 Bouton "Sauvegarder"</h4>
		<ul>
			<li>✅ Tous les effets modifiés restent actifs</li>
			<li>✅ Les paramètres sont enregistrés dans la configuration</li>
			<li>✅ Les effets seront réappliqués au prochain démarrage</li>
		</ul>

		<h4>❌ Bouton "Annuler"</h4>
		<ul>
			<li>🔄 Tous les effets reviennent à leur état initial (avant ouverture du formulaire)</li>
			<li>🔄 Aucune modification n'est sauvegardée</li>
			<li>🔄 Les changements en cours de session sont perdus</li>
		</ul>
	</div>

	<h3>🎛️ Réinitialiser les Effets</h3>
	<p>Le bouton <strong>"Réinitialiser les effets"</strong> désactive tous les effets et rétablit les valeurs par défaut :</p>
	<ul>
		<li>Tous les effets désactivés</li>
		<li>Mix : 30%</li>
		<li>Délai : 300 ms</li>
		<li>Feedback : 40%</li>
		<li>Pitch : 0 demi-tons</li>
		<li>Vitesse : 1.0x</li>
	</ul>
	<p><strong>Attention :</strong> N'oubliez pas de cliquer "Sauvegarder" si vous voulez conserver cette réinitialisation !</p>

	<h3>💡 Conseils d'Utilisation</h3>
	<ul>
		<li><strong>Expérimentez en direct :</strong> Ajustez les paramètres pendant qu'une chanson joue pour entendre immédiatement les changements</li>
		<li><strong>Combinaisons :</strong> Vous pouvez activer plusieurs effets simultanément (ex: Reverb + Echo)</li>
		<li><strong>Modération :</strong> Des valeurs trop élevées peuvent saturer le son. Commencez avec des valeurs moyennes</li>
		<li><strong>Annuler sans risque :</strong> Si le résultat ne vous plaît pas, cliquez "Annuler" pour revenir en arrière</li>
		<li><strong>Persistance :</strong> Les effets sauvegardés s'appliquent automatiquement à toutes les chansons jusqu'à ce que vous les modifiiez</li>
	</ul>

	<h3>⚠️ Limitations Actuelles</h3>
	<ul>
		<li><strong>Pitch Shift et Time Stretch :</strong> Implémentations simplifiées. Des algorithmes DSP avancés seront ajoutés dans une version future pour une meilleure qualité audio</li>
		<li><strong>Performance :</strong> L'activation de plusieurs effets simultanément peut augmenter l'utilisation du CPU</li>
	</ul>
</section>
```

---

## ENGLISH (AUDIOPLAY_GUIDE_COMPLET.en.html)

### Add to <nav> after parameters line:
```html
<li><a href="#audio-effects">🎚️ Audio Effects</a></li>
```

### Add after parameters section (before features):
```html
<!-- AUDIO EFFECTS SECTION -->
<section id="audio-effects">
	<h2>🎚️ Audio Effects</h2>

	<div class="highlight">
		<h3>🎵 Real-Time Editing</h3>
		<p>All audio effects can be enabled and adjusted <strong>during playback</strong> without needing to restart the song.</p>
	</div>

	<h3>🌊 Reverb</h3>
	<p><strong>Description:</strong> Adds a spatial echo effect that simulates room acoustics.</p>
	<ul>
		<li><strong>Mix (0-100%):</strong> Controls effect intensity
			<ul>
				<li>0% = original sound only</li>
				<li>30% = recommended balance</li>
				<li>100% = maximum reverb</li>
			</ul>
		</li>
	</ul>
	<p><strong>Uses:</strong> Simulate a concert, add depth, create cathedral ambiance.</p>

	<h3>🔊 Echo</h3>
	<p><strong>Description:</strong> Creates sound repetitions with adjustable parameters.</p>
	<ul>
		<li><strong>Mix (0-100%):</strong> Echo volume relative to original signal</li>
		<li><strong>Delay (50-2000 ms):</strong> Time between original sound and its repetition
			<ul>
				<li>50-200 ms = short echo, "slapback" effect</li>
				<li>300-500 ms = medium echo, classic rock style</li>
				<li>800-2000 ms = long echo, spatial effect</li>
			</ul>
		</li>
		<li><strong>Feedback (0-100%):</strong> Number of repetitions
			<ul>
				<li>0% = single repetition</li>
				<li>40% = few repetitions that fade out</li>
				<li>90% = very many repetitions (careful!)</li>
			</ul>
		</li>
	</ul>
	<p><strong>Uses:</strong> Vocal effect, dub/reggae ambiance, create depth.</p>

	<h3>🎹 Pitch Shift</h3>
	<p><strong>Description:</strong> Changes pitch without affecting playback speed.</p>
	<ul>
		<li><strong>Semitones (-12 to +12):</strong> Pitch shift in semitones
			<ul>
				<li>-12 = one octave lower</li>
				<li>0 = original pitch</li>
				<li>+12 = one octave higher</li>
				<li>±1 or ±2 = pitch correction for singing</li>
			</ul>
		</li>
	</ul>
	<p><strong>Uses:</strong> Transpose to match your voice, create "chipmunk" or "deep voice" effects.</p>
	<p><em>Note: Current implementation is a simplified placeholder. Advanced DSP algorithm will be added in a future version.</em></p>

	<h3>⏱️ Time Stretch</h3>
	<p><strong>Description:</strong> Changes playback speed without affecting pitch.</p>
	<ul>
		<li><strong>Speed (0.5x - 2.0x):</strong> Speed ratio
			<ul>
				<li>0.5x = twice as slow</li>
				<li>1.0x = original speed</li>
				<li>1.5x = 50% faster</li>
				<li>2.0x = twice as fast</li>
			</ul>
		</li>
	</ul>
	<p><strong>Uses:</strong> Learn a song slowly, practice an instrument, create remixes.</p>
	<p><em>Note: Current implementation is a simplified placeholder. Advanced DSP algorithm will be added in a future version.</em></p>

	<h3>🔄 Save Workflow</h3>
	<div class="warning-box">
		<h4>💾 "Save" Button</h4>
		<ul>
			<li>✅ All modified effects stay active</li>
			<li>✅ Parameters are saved in configuration</li>
			<li>✅ Effects will be reapplied on next startup</li>
		</ul>

		<h4>❌ "Cancel" Button</h4>
		<ul>
			<li>🔄 All effects revert to their initial state (before form opening)</li>
			<li>🔄 No changes are saved</li>
			<li>🔄 Session changes are lost</li>
		</ul>
	</div>

	<h3>🎛️ Reset Effects</h3>
	<p>The <strong>"Reset Effects"</strong> button disables all effects and restores default values:</p>
	<ul>
		<li>All effects disabled</li>
		<li>Mix: 30%</li>
		<li>Delay: 300 ms</li>
		<li>Feedback: 40%</li>
		<li>Pitch: 0 semitones</li>
		<li>Speed: 1.0x</li>
	</ul>
	<p><strong>Warning:</strong> Don't forget to click "Save" if you want to keep this reset!</p>

	<h3>💡 Usage Tips</h3>
	<ul>
		<li><strong>Experiment live:</strong> Adjust parameters while a song plays to hear changes immediately</li>
		<li><strong>Combinations:</strong> You can enable multiple effects simultaneously (e.g., Reverb + Echo)</li>
		<li><strong>Moderation:</strong> Too high values can saturate the sound. Start with medium values</li>
		<li><strong>Cancel safely:</strong> If you don't like the result, click "Cancel" to go back</li>
		<li><strong>Persistence:</strong> Saved effects automatically apply to all songs until you modify them</li>
	</ul>

	<h3>⚠️ Current Limitations</h3>
	<ul>
		<li><strong>Pitch Shift and Time Stretch:</strong> Simplified implementations. Advanced DSP algorithms will be added in a future version for better audio quality</li>
		<li><strong>Performance:</strong> Enabling multiple effects simultaneously may increase CPU usage</li>
	</ul>
</section>
```

---

## ESPAÑOL (AUDIOPLAY_GUIDE_COMPLET.es.html)

### Agregar en <nav> después de la línea de parámetros:
```html
<li><a href="#efectos-audio">🎚️ Efectos de Audio</a></li>
```

### Agregar después de la sección de parámetros (antes de funcionalidades):
```html
<!-- SECCIÓN EFECTOS DE AUDIO -->
<section id="efectos-audio">
	<h2>🎚️ Efectos de Audio</h2>

	<div class="highlight">
		<h3>🎵 Edición en Tiempo Real</h3>
		<p>Todos los efectos de audio se pueden activar y ajustar <strong>durante la reproducción</strong> sin necesidad de reiniciar la canción.</p>
	</div>

	<h3>🌊 Reverberación</h3>
	<p><strong>Descripción:</strong> Agrega un efecto de eco espacial que simula la acústica de una sala.</p>
	<ul>
		<li><strong>Mezcla (0-100%):</strong> Controla la intensidad del efecto
			<ul>
				<li>0% = solo sonido original</li>
				<li>30% = equilibrio recomendado</li>
				<li>100% = reverberación máxima</li>
			</ul>
		</li>
	</ul>
	<p><strong>Usos:</strong> Simular un concierto, dar profundidad, crear ambiente de catedral.</p>

	<h3>🔊 Eco</h3>
	<p><strong>Descripción:</strong> Crea repeticiones del sonido con parámetros ajustables.</p>
	<ul>
		<li><strong>Mezcla (0-100%):</strong> Volumen del eco relativo a la señal original</li>
		<li><strong>Retraso (50-2000 ms):</strong> Tiempo entre el sonido original y su repetición
			<ul>
				<li>50-200 ms = eco corto, efecto "slapback"</li>
				<li>300-500 ms = eco medio, estilo rock clásico</li>
				<li>800-2000 ms = eco largo, efecto espacial</li>
			</ul>
		</li>
		<li><strong>Retroalimentación (0-100%):</strong> Número de repeticiones
			<ul>
				<li>0% = una sola repetición</li>
				<li>40% = algunas repeticiones que se desvanecen</li>
				<li>90% = muchas repeticiones (¡cuidado!)</li>
			</ul>
		</li>
	</ul>
	<p><strong>Usos:</strong> Efecto vocal, ambiente dub/reggae, crear profundidad.</p>

	<h3>🎹 Cambio de Tono</h3>
	<p><strong>Descripción:</strong> Modifica la altura tonal sin afectar la velocidad de reproducción.</p>
	<ul>
		<li><strong>Semitonos (-12 a +12):</strong> Desplazamiento en semitonos
			<ul>
				<li>-12 = una octava más bajo</li>
				<li>0 = tono original</li>
				<li>+12 = una octava más alto</li>
				<li>±1 o ±2 = corrección de tono para cantar</li>
			</ul>
		</li>
	</ul>
	<p><strong>Usos:</strong> Transponer para adaptarse a tu voz, crear efectos de "voz de ardilla" o "voz grave".</p>
	<p><em>Nota: La implementación actual es un marcador de posición simplificado. Se agregará un algoritmo DSP avanzado en una versión futura.</em></p>

	<h3>⏱️ Cambio de Tempo</h3>
	<p><strong>Descripción:</strong> Modifica la velocidad de reproducción sin afectar el tono.</p>
	<ul>
		<li><strong>Velocidad (0.5x - 2.0x):</strong> Relación de velocidad
			<ul>
				<li>0.5x = dos veces más lento</li>
				<li>1.0x = velocidad original</li>
				<li>1.5x = 50% más rápido</li>
				<li>2.0x = dos veces más rápido</li>
			</ul>
		</li>
	</ul>
	<p><strong>Usos:</strong> Aprender una canción lentamente, practicar un instrumento, crear remixes.</p>
	<p><em>Nota: La implementación actual es un marcador de posición simplificado. Se agregará un algoritmo DSP avanzado en una versión futura.</em></p>

	<h3>🔄 Flujo de Guardado</h3>
	<div class="warning-box">
		<h4>💾 Botón "Guardar"</h4>
		<ul>
			<li>✅ Todos los efectos modificados permanecen activos</li>
			<li>✅ Los parámetros se guardan en la configuración</li>
			<li>✅ Los efectos se volverán a aplicar en el próximo inicio</li>
		</ul>

		<h4>❌ Botón "Cancelar"</h4>
		<ul>
			<li>🔄 Todos los efectos vuelven a su estado inicial (antes de abrir el formulario)</li>
			<li>🔄 No se guarda ningún cambio</li>
			<li>🔄 Los cambios de la sesión se pierden</li>
		</ul>
	</div>

	<h3>🎛️ Restablecer Efectos</h3>
	<p>El botón <strong>"Restablecer Efectos"</strong> desactiva todos los efectos y restablece los valores predeterminados:</p>
	<ul>
		<li>Todos los efectos desactivados</li>
		<li>Mezcla: 30%</li>
		<li>Retraso: 300 ms</li>
		<li>Retroalimentación: 40%</li>
		<li>Tono: 0 semitonos</li>
		<li>Velocidad: 1.0x</li>
	</ul>
	<p><strong>Advertencia:</strong> ¡No olvides hacer clic en "Guardar" si quieres mantener este restablecimiento!</p>

	<h3>💡 Consejos de Uso</h3>
	<ul>
		<li><strong>Experimenta en vivo:</strong> Ajusta los parámetros mientras se reproduce una canción para escuchar los cambios inmediatamente</li>
		<li><strong>Combinaciones:</strong> Puedes activar varios efectos simultáneamente (ej: Reverberación + Eco)</li>
		<li><strong>Moderación:</strong> Valores demasiado altos pueden saturar el sonido. Comienza con valores medios</li>
		<li><strong>Cancelar con seguridad:</strong> Si no te gusta el resultado, haz clic en "Cancelar" para volver atrás</li>
		<li><strong>Persistencia:</strong> Los efectos guardados se aplican automáticamente a todas las canciones hasta que los modifiques</li>
	</ul>

	<h3>⚠️ Limitaciones Actuales</h3>
	<ul>
		<li><strong>Cambio de Tono y Tempo:</strong> Implementaciones simplificadas. Se agregarán algoritmos DSP avanzados en una versión futura para mejor calidad de audio</li>
		<li><strong>Rendimiento:</strong> Activar múltiples efectos simultáneamente puede aumentar el uso de la CPU</li>
	</ul>
</section>
```

---

## DEUTSCH (AUDIOPLAY_GUIDE_COMPLET.de.html)

### In <nav> nach der Parameterzeile hinzufügen:
```html
<li><a href="#audioeffekte">🎚️ Audioeffekte</a></li>
```

### Nach dem Abschnitt Parameter hinzufügen (vor Funktionen):
```html
<!-- AUDIOEFFEKTE ABSCHNITT -->
<section id="audioeffekte">
	<h2>🎚️ Audioeffekte</h2>

	<div class="highlight">
		<h3>🎵 Echtzeitbearbeitung</h3>
		<p>Alle Audioeffekte können <strong>während der Wiedergabe</strong> aktiviert und angepasst werden, ohne das Lied neu starten zu müssen.</p>
	</div>

	<h3>🌊 Hall (Reverb)</h3>
	<p><strong>Beschreibung:</strong> Fügt einen räumlichen Echoeffekt hinzu, der die Raumakustik simuliert.</p>
	<ul>
		<li><strong>Mischung (0-100%):</strong> Steuert die Effektintensität
			<ul>
				<li>0% = nur Originalsound</li>
				<li>30% = empfohlenes Gleichgewicht</li>
				<li>100% = maximaler Hall</li>
			</ul>
		</li>
	</ul>
	<p><strong>Verwendungen:</strong> Konzert simulieren, Tiefe hinzufügen, Kathedralenatmosphäre schaffen.</p>

	<h3>🔊 Echo</h3>
	<p><strong>Beschreibung:</strong> Erzeugt Wiederholungen des Sounds mit einstellbaren Parametern.</p>
	<ul>
		<li><strong>Mischung (0-100%):</strong> Echo-Lautstärke relativ zum Originalsignal</li>
		<li><strong>Verzögerung (50-2000 ms):</strong> Zeit zwischen Originalsound und seiner Wiederholung
			<ul>
				<li>50-200 ms = kurzes Echo, "Slapback"-Effekt</li>
				<li>300-500 ms = mittleres Echo, klassischer Rock-Stil</li>
				<li>800-2000 ms = langes Echo, räumlicher Effekt</li>
			</ul>
		</li>
		<li><strong>Rückkopplung (0-100%):</strong> Anzahl der Wiederholungen
			<ul>
				<li>0% = einzelne Wiederholung</li>
				<li>40% = einige Wiederholungen, die ausblenden</li>
				<li>90% = sehr viele Wiederholungen (Vorsicht!)</li>
			</ul>
		</li>
	</ul>
	<p><strong>Verwendungen:</strong> Vokaleffekt, Dub/Reggae-Atmosphäre, Tiefe schaffen.</p>

	<h3>🎹 Tonhöhenänderung (Pitch Shift)</h3>
	<p><strong>Beschreibung:</strong> Ändert die Tonhöhe ohne die Wiedergabegeschwindigkeit zu beeinflussen.</p>
	<ul>
		<li><strong>Halbtöne (-12 bis +12):</strong> Tonhöhenverschiebung in Halbtönen
			<ul>
				<li>-12 = eine Oktave tiefer</li>
				<li>0 = Originaltonhöhe</li>
				<li>+12 = eine Oktave höher</li>
				<li>±1 oder ±2 = Tonhöhenkorrektur zum Singen</li>
			</ul>
		</li>
	</ul>
	<p><strong>Verwendungen:</strong> Transponieren, um zur Stimme zu passen, "Chipmunk"- oder "tiefe Stimme"-Effekte erzeugen.</p>
	<p><em>Hinweis: Die aktuelle Implementierung ist ein vereinfachter Platzhalter. Ein fortgeschrittener DSP-Algorithmus wird in einer zukünftigen Version hinzugefügt.</em></p>

	<h3>⏱️ Tempoänderung (Time Stretch)</h3>
	<p><strong>Beschreibung:</strong> Ändert die Wiedergabegeschwindigkeit ohne die Tonhöhe zu beeinflussen.</p>
	<ul>
		<li><strong>Geschwindigkeit (0.5x - 2.0x):</strong> Geschwindigkeitsverhältnis
			<ul>
				<li>0.5x = doppelt so langsam</li>
				<li>1.0x = Originalgeschwindigkeit</li>
				<li>1.5x = 50% schneller</li>
				<li>2.0x = doppelt so schnell</li>
			</ul>
		</li>
	</ul>
	<p><strong>Verwendungen:</strong> Ein Lied langsam lernen, ein Instrument üben, Remixe erstellen.</p>
	<p><em>Hinweis: Die aktuelle Implementierung ist ein vereinfachter Platzhalter. Ein fortgeschrittener DSP-Algorithmus wird in einer zukünftigen Version hinzugefügt.</em></p>

	<h3>🔄 Speicher-Workflow</h3>
	<div class="warning-box">
		<h4>💾 Schaltfläche "Speichern"</h4>
		<ul>
			<li>✅ Alle geänderten Effekte bleiben aktiv</li>
			<li>✅ Parameter werden in der Konfiguration gespeichert</li>
			<li>✅ Effekte werden beim nächsten Start erneut angewendet</li>
		</ul>

		<h4>❌ Schaltfläche "Abbrechen"</h4>
		<ul>
			<li>🔄 Alle Effekte kehren zu ihrem ursprünglichen Zustand zurück (vor dem Öffnen des Formulars)</li>
			<li>🔄 Keine Änderungen werden gespeichert</li>
			<li>🔄 Sitzungsänderungen gehen verloren</li>
		</ul>
	</div>

	<h3>🎛️ Effekte zurücksetzen</h3>
	<p>Die Schaltfläche <strong>"Effekte zurücksetzen"</strong> deaktiviert alle Effekte und stellt Standardwerte wieder her:</p>
	<ul>
		<li>Alle Effekte deaktiviert</li>
		<li>Mischung: 30%</li>
		<li>Verzögerung: 300 ms</li>
		<li>Rückkopplung: 40%</li>
		<li>Tonhöhe: 0 Halbtöne</li>
		<li>Geschwindigkeit: 1.0x</li>
	</ul>
	<p><strong>Warnung:</strong> Vergessen Sie nicht, auf "Speichern" zu klicken, wenn Sie dieses Zurücksetzen behalten möchten!</p>

	<h3>💡 Verwendungstipps</h3>
	<ul>
		<li><strong>Experiment live:</strong> Passen Sie Parameter an, während ein Lied abgespielt wird, um Änderungen sofort zu hören</li>
		<li><strong>Kombinationen:</strong> Sie können mehrere Effekte gleichzeitig aktivieren (z.B. Hall + Echo)</li>
		<li><strong>Mäßigung:</strong> Zu hohe Werte können den Klang sättigen. Beginnen Sie mit mittleren Werten</li>
		<li><strong>Sicher abbrechen:</strong> Wenn Ihnen das Ergebnis nicht gefällt, klicken Sie auf "Abbrechen", um zurückzugehen</li>
		<li><strong>Persistenz:</strong> Gespeicherte Effekte werden automatisch auf alle Lieder angewendet, bis Sie sie ändern</li>
	</ul>

	<h3>⚠️ Aktuelle Einschränkungen</h3>
	<ul>
		<li><strong>Tonhöhen- und Tempoänderung:</strong> Vereinfachte Implementierungen. Fortgeschrittene DSP-Algorithmen werden in einer zukünftigen Version für bessere Audioqualität hinzugefügt</li>
		<li><strong>Leistung:</strong> Das gleichzeitige Aktivieren mehrerer Effekte kann die CPU-Auslastung erhöhen</li>
	</ul>
</section>
```

---

## ITALIANO (AUDIOPLAY_GUIDE_COMPLET.it.html)

### Aggiungere in <nav> dopo la riga dei parametri:
```html
<li><a href="#effetti-audio">🎚️ Effetti Audio</a></li>
```

### Aggiungere dopo la sezione parametri (prima delle funzionalità):
```html
<!-- SEZIONE EFFETTI AUDIO -->
<section id="effetti-audio">
	<h2>🎚️ Effetti Audio</h2>

	<div class="highlight">
		<h3>🎵 Modifica in Tempo Reale</h3>
		<p>Tutti gli effetti audio possono essere attivati e regolati <strong>durante la riproduzione</strong> senza dover riavviare la canzone.</p>
	</div>

	<h3>🌊 Riverbero</h3>
	<p><strong>Descrizione:</strong> Aggiunge un effetto eco spaziale che simula l'acustica di una sala.</p>
	<ul>
		<li><strong>Mix (0-100%):</strong> Controlla l'intensità dell'effetto
			<ul>
				<li>0% = solo suono originale</li>
				<li>30% = equilibrio consigliato</li>
				<li>100% = riverbero massimo</li>
			</ul>
		</li>
	</ul>
	<p><strong>Usi:</strong> Simulare un concerto, aggiungere profondità, creare un'atmosfera da cattedrale.</p>

	<h3>🔊 Eco</h3>
	<p><strong>Descrizione:</strong> Crea ripetizioni del suono con parametri regolabili.</p>
	<ul>
		<li><strong>Mix (0-100%):</strong> Volume dell'eco relativo al segnale originale</li>
		<li><strong>Ritardo (50-2000 ms):</strong> Tempo tra il suono originale e la sua ripetizione
			<ul>
				<li>50-200 ms = eco breve, effetto "slapback"</li>
				<li>300-500 ms = eco medio, stile rock classico</li>
				<li>800-2000 ms = eco lungo, effetto spaziale</li>
			</ul>
		</li>
		<li><strong>Feedback (0-100%):</strong> Numero di ripetizioni
			<ul>
				<li>0% = singola ripetizione</li>
				<li>40% = alcune ripetizioni che svaniscono</li>
				<li>90% = molte ripetizioni (attenzione!)</li>
			</ul>
		</li>
	</ul>
	<p><strong>Usi:</strong> Effetto vocale, atmosfera dub/reggae, creare profondità.</p>

	<h3>🎹 Cambio di Tonalità</h3>
	<p><strong>Descrizione:</strong> Modifica l'altezza tonale senza influire sulla velocità di riproduzione.</p>
	<ul>
		<li><strong>Semitoni (-12 a +12):</strong> Spostamento in semitoni
			<ul>
				<li>-12 = un'ottava più basso</li>
				<li>0 = tonalità originale</li>
				<li>+12 = un'ottava più alto</li>
				<li>±1 o ±2 = correzione di tonalità per cantare</li>
			</ul>
		</li>
	</ul>
	<p><strong>Usi:</strong> Trasporre per adattarsi alla propria voce, creare effetti "voce da scoiattolo" o "voce grave".</p>
	<p><em>Nota: L'implementazione attuale è un segnaposto semplificato. Un algoritmo DSP avanzato sarà aggiunto in una versione futura.</em></p>

	<h3>⏱️ Cambio di Tempo</h3>
	<p><strong>Descrizione:</strong> Modifica la velocità di riproduzione senza influire sulla tonalità.</p>
	<ul>
		<li><strong>Velocità (0.5x - 2.0x):</strong> Rapporto di velocità
			<ul>
				<li>0.5x = due volte più lento</li>
				<li>1.0x = velocità originale</li>
				<li>1.5x = 50% più veloce</li>
				<li>2.0x = due volte più veloce</li>
			</ul>
		</li>
	</ul>
	<p><strong>Usi:</strong> Imparare una canzone lentamente, praticare uno strumento, creare remix.</p>
	<p><em>Nota: L'implementazione attuale è un segnaposto semplificato. Un algoritmo DSP avanzato sarà aggiunto in una versione futura.</em></p>

	<h3>🔄 Flusso di Salvataggio</h3>
	<div class="warning-box">
		<h4>💾 Pulsante "Salva"</h4>
		<ul>
			<li>✅ Tutti gli effetti modificati rimangono attivi</li>
			<li>✅ I parametri vengono salvati nella configurazione</li>
			<li>✅ Gli effetti verranno riapplicati al prossimo avvio</li>
		</ul>

		<h4>❌ Pulsante "Annulla"</h4>
		<ul>
			<li>🔄 Tutti gli effetti tornano al loro stato iniziale (prima dell'apertura del modulo)</li>
			<li>🔄 Nessuna modifica viene salvata</li>
			<li>🔄 Le modifiche della sessione vengono perse</li>
		</ul>
	</div>

	<h3>🎛️ Ripristina Effetti</h3>
	<p>Il pulsante <strong>"Ripristina Effetti"</strong> disattiva tutti gli effetti e ripristina i valori predefiniti:</p>
	<ul>
		<li>Tutti gli effetti disattivati</li>
		<li>Mix: 30%</li>
		<li>Ritardo: 300 ms</li>
		<li>Feedback: 40%</li>
		<li>Tonalità: 0 semitoni</li>
		<li>Velocità: 1.0x</li>
	</ul>
	<p><strong>Attenzione:</strong> Non dimenticare di fare clic su "Salva" se vuoi mantenere questo ripristino!</p>

	<h3>💡 Suggerimenti per l'Uso</h3>
	<ul>
		<li><strong>Sperimenta in diretta:</strong> Regola i parametri mentre una canzone viene riprodotta per sentire immediatamente i cambiamenti</li>
		<li><strong>Combinazioni:</strong> Puoi attivare più effetti simultaneamente (es: Riverbero + Eco)</li>
		<li><strong>Moderazione:</strong> Valori troppo alti possono saturare il suono. Inizia con valori medi</li>
		<li><strong>Annulla in sicurezza:</strong> Se non ti piace il risultato, fai clic su "Annulla" per tornare indietro</li>
		<li><strong>Persistenza:</strong> Gli effetti salvati si applicano automaticamente a tutte le canzoni finché non li modifichi</li>
	</ul>

	<h3>⚠️ Limitazioni Attuali</h3>
	<ul>
		<li><strong>Cambio di Tonalità e Tempo:</strong> Implementazioni semplificate. Algoritmi DSP avanzati saranno aggiunti in una versione futura per una migliore qualità audio</li>
		<li><strong>Prestazioni:</strong> L'attivazione di più effetti simultaneamente può aumentare l'utilizzo della CPU</li>
	</ul>
</section>
```
