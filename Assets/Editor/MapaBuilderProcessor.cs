using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;

public class MapaBuilderProcessor
{
    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.WebGL)
            return;

        string outputFileName = "mapa.json";
        string outputPath = Path.Combine(pathToBuiltProject, outputFileName);

        // Lista de exclusiones (archivos o carpetas que no queremos en el mapa)
        string[] excluir = { ".git", ".vs", ".vscode", "node_modules", ".meta", ".bat", outputFileName, ".DS_Store" };

        Debug.Log("▶️ [MapaBuilder] Iniciando escaneo bruto de archivos...");

        try
        {
            // --- NUEVO: GENERAR ARCHIVO DE VERSIÓN ---
            string versionFileName = "Version.txt";
            string versionPath = Path.Combine(pathToBuiltProject, versionFileName);
            string currentVersion = DateTime.Now.ToString("yyyy.MM.dd.HHmm"); // Formato: Año.Mes.Dia.HoraMinuto
            File.WriteAllText(versionPath, currentVersion);
            Debug.Log($"<b>✅ [VersionBuilder] Versión generada: {currentVersion}</b>");

            // Obtener todos los archivos de forma recursiva
            string[] allFiles = Directory.GetFiles(pathToBuiltProject, "*", SearchOption.AllDirectories);
            List<string> relativeFiles = new();
            StringBuilder consoleLog = new("<b>Se registraron estas rutas:</b>\n");

            foreach (string file in allFiles)
            {
                // Convertir ruta absoluta a relativa al proyecto de build
                string relPath = file.Replace(pathToBuiltProject, "").TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // Cambiar barras invertidas \ por / para compatibilidad web
                relPath = relPath.Replace("\\", "/");

                // Lógica de filtrado
                bool skip = false;
                foreach (string pattern in excluir)
                {
                    if (relPath.Contains(pattern))
                    {
                        skip = true;
                        break;
                    }
                }

                if (!skip)
                {
                    relativeFiles.Add(relPath);
                    consoleLog.AppendLine(relPath);
                }
            }

            // Generar el JSON manualmente para asegurar un formato de array simple limpio
            StringBuilder jsonBuilder = new ();
            jsonBuilder.AppendLine("[");
            for (int i = 0; i < relativeFiles.Count; i++)
            {
                jsonBuilder.Append("  \"").Append(relativeFiles[i]).Append("\"");
                if (i < relativeFiles.Count - 1) jsonBuilder.Append(",");
                jsonBuilder.AppendLine();
            }
            jsonBuilder.Append("]");

            // Guardar archivo
            File.WriteAllText(outputPath, jsonBuilder.ToString());

            // Reporte final en consola
            Debug.Log(consoleLog.ToString());
            Debug.Log($"<b>✅ [MapaBuilder] Totales = {relativeFiles.Count}</b>");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ [MapaBuilder] ¡¡¡ ALERTA !!! Error: {e.Message}");
        }
    }
}