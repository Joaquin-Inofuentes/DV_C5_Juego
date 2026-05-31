# Instrucciones para Agentes de IA – DV_C5_Juego

Este documento define las directrices y reglas de oro para cualquier agente de IA que trabaje en este repositorio. Su objetivo es maximizar la eficiencia en el uso de tokens y asegurar un desarrollo limpio.

---

## 1. Regla de Oro de Lectura Jerárquica

**NUNCA proceses todo el código fuente del proyecto de golpe.** 
Para entender el proyecto o resolver una tarea, sigue este orden de lectura jerárquico estricto:

1. **Contexto General**: Lee primero [ContextPack.md](file:///Docs/ContextPack.md) para entender el flujo y arquitectura global.
2. **Inventario y Arquitectura**: Consulta [ProjectInventory.md](file:///Docs/ProjectInventory.md) para ver dónde se ubican los archivos y [Architecture.md](file:///Docs/Architecture.md) para ver dependencias.
3. **Sistemas Específicos**: Si vas a modificar una parte del juego, lee el documento del sistema en `Docs/Systems/[NombreSistema].md`.
4. **Ficheros Individuales**: Consulta los resúmenes en `Docs/FileIndex/[Script].md` antes de abrir el código fuente completo.
5. **Código Fuente**: Abre el archivo `.cs` únicamente si necesitas editarlo o analizar una línea exacta de lógica compleja.

---

## 2. Límites de Archivos por Tarea

Para evitar la saturación del contexto y mantener la precisión:
- **Ideal**: Modificar o leer hasta **5 archivos** por tarea.
- **Aceptable**: Modificar o leer entre **6 y 10 archivos**.
- **Máximo permitido**: **20 archivos**. Si superas este límite, debes justificar detalladamente al usuario por qué es necesario.

---

## 3. Evitar Duplicación de Información

- No repliques la lógica del código en la documentación.
- Si actualizas un script o sistema, recuerda refrescar la documentación ejecutando:
  - `powershell -ExecutionPolicy Bypass -File Tools/RefreshDocs.ps1`
  - `powershell -ExecutionPolicy Bypass -File Tools/RefreshArchitecture.ps1`
- Mantén los comentarios de código breves y limpios, delegando las explicaciones largas a los archivos Markdown correspondientes en `Docs/`.
