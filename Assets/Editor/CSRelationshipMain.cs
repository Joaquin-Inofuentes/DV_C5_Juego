// Assets/Editor/CSRelationshipMain.cs
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public static class CSRelationshipMain
{
    // ✅ MAIN: solo da órdenes (sin lógica)
    [MenuItem("Tools/UML Simple/1) Cachear CS en RAM + Tabla (chars/lines)")]
    public static void Main_CacheYTabla()
    {
        var rutas = CSFileService.ObtenerRutasCS();
        var scripts = CSContentService.CargarScriptsEnRAM(rutas);

        Debug.Log(CSReportService.GenerarTablaCharsLineas(scripts));
    }

    // ✅ MAIN: solo da órdenes (sin lógica)
    [MenuItem("Tools/UML Simple/2) Buscar referencias por NOMBRE (A --> B)")]
    public static void Main_RelacionesPorNombre()
    {
        var rutas = CSFileService.ObtenerRutasCS();
        var scripts = CSContentService.CargarScriptsEnRAM(rutas);

        var nombres = scripts.Select(s => s.Name).ToList();
        var contenidos = scripts.Select(s => s.Content).ToList();

        var relaciones = CSRelationService.CrearRelacionesPorContains(nombres, contenidos);

        Debug.Log(string.Join("\n", relaciones));
    }

    // ✅ MAIN: solo da órdenes (sin lógica)
    [MenuItem("Tools/UML Simple/3) Exportar 4 archivos (Mermaid + PlantUML x3) a C:\\PlantUML")]
    public static void Main_Exportar4Archivos_SinFiltroCantidad()
    {
        var rutas = CSFileService.ObtenerRutasCS();
        var scripts = CSContentService.CargarScriptsEnRAM(rutas);

        var nombres = scripts.Select(s => s.Name).ToList();
        var contenidos = scripts.Select(s => s.Content).ToList();

        var relaciones = CSRelationService.CrearRelacionesPorContains(nombres, contenidos);

        // ✅ ÚNICO FILTRO: por keywords (si lista vacía => no filtra nada)
        // Editá estas palabras como quieras:
        var keywords = new List<string>
        {
            // "Player",
            // "UI",
            // "Inventory"
        };

        var relacionesFiltradas = CSKeywordFilterService.FiltrarSoloDependenciasDeTargets(relaciones, keywords);

        var paths = CSUmlMultiExportService.Guardar4Archivos(relacionesFiltradas);
        Debug.Log("✅ Exportados en C:\\PlantUML:\n" + string.Join("\n", paths));
    }

    [MenuItem("Tools/UML Simple/5) Top Ruido (Total/Out/In)")]
    public static void Main_TopRuido()
    {
        var rutas = CSFileService.ObtenerRutasCS();
        var scripts = CSContentService.CargarScriptsEnRAM(rutas);

        var nombres = scripts.Select(s => s.Name).ToList();
        var contenidos = scripts.Select(s => s.Content).ToList();

        var relaciones = CSRelationService.CrearRelacionesPorContains(nombres, contenidos);

        Debug.Log(CSNoiseService.ReportTop(relaciones, top: 30));

        var keywords = CSNoiseService.KeywordsTop(relaciones, top: 30);
        Debug.Log("KEYWORDS TOP:\n" + string.Join("\n", keywords));
    }


    [MenuItem("Tools/UML Simple/3) Exportar 4 archivos (Mermaid + PlantUML x3) SOLO deps de ruidosos")]
    public static void Main_Exportar4Archivos_SoloDepsDeRuidosos()
    {
        var rutas = CSFileService.ObtenerRutasCS();
        var scripts = CSContentService.CargarScriptsEnRAM(rutas);

        var nombres = scripts.Select(s => s.Name).ToList();
        var contenidos = scripts.Select(s => s.Content).ToList();

        var relaciones = CSRelationService.CrearRelacionesPorContains(nombres, contenidos);

        // ✅ Lista ruidosos (NO mostrar quién los usa, solo lo que necesitan)
        var ruidosos = new List<string>
    {
        "GameManager",
        "GEN_ParametrosGlobales",
        "Visor_DetectarElementoApuntado",
        "CDPIControladorDePuntosInteractivos",
        "DATA_Cambios",
        "DrawProperties",
        "PropertyValues",
        "Android",
        "Drive_Cache",
        "GestorCSV"
    };

        var relacionesFiltradas = CSKeywordFilterService.FiltrarSoloDependenciasDeTargets(relaciones, ruidosos);

        var paths = CSUmlMultiExportService.Guardar4Archivos(relacionesFiltradas);
        Debug.Log("✅ Exportados SOLO deps de ruidosos:\n" + string.Join("\n", paths));
    }



    [MenuItem("Tools/UML Simple/4) Exportar FILTRADO (solo deps de ruidosos) a C:\\PlantUML")]
    public static void Main_ExportarFiltradoSoloDepsDeRuidosos()
    {
        var rutas = CSFileService.ObtenerRutasCS();
        var scripts = CSContentService.CargarScriptsEnRAM(rutas);

        var nombres = scripts.Select(s => s.Name).ToList();
        var contenidos = scripts.Select(s => s.Content).ToList();

        var relaciones = CSRelationService.CrearRelacionesPorContains(nombres, contenidos);

        var ruidosos = new List<string>
    {
        "GameManager",
        "GEN_ParametrosGlobales",
        "Visor_DetectarElementoApuntado",
        "CDPIControladorDePuntosInteractivos",
        "DATA_Cambios",
        "DrawProperties",
        "PropertyValues",
        "Android",
        "Drive_Cache",
        "GestorCSV"
    };

        // ✅ ACA se filtra
        var filtradas = CSKeywordFilterService.FiltrarSoloDependenciasDeTargets(relaciones, ruidosos);

        // ✅ ACA exporta lo filtrado (NO relaciones)
        var paths = CSUmlMultiExportService.Guardar4Archivos(filtradas);

        Debug.Log("✅ Exportados FILTRADOS en C:\\PlantUML:\n" + string.Join("\n", paths)
            + "\n\nRelaciones filtradas: " + filtradas.Count
            + "\nRelaciones totales: " + relaciones.Count);
    }


    [MenuItem("Tools/UML Simple/6) Exportar TODO menos excluidos + Ignorar carpetas")]
    public static void Main_ExportarTodo_MenosExcluidos_IgnorarCarpetas()
    {
        var rutas = CSFileService.ObtenerRutasCS();

        // ==============================
        // 1) EXCLUIR POR NOMBRE
        // ==============================
        var excluirNombres = new HashSet<string>(StringComparer.Ordinal)
    {
        "AppScriptLlamados",
        "NavegadorDeArchivosScrollView",
        "PropertyValues",
        "DrawProperties",
        "PropertyConversions",
        "GameManager",
        "GEN_ParametrosGlobales",
        "GestorCSV",
        "Android",
        "CDPIControladorDePuntosInteractivos",
        "Anotaciones",
        "AnotacionesController",
        "Limpieza",
        "Visor_DetectarElementoApuntado",
        "Limpieza",
        "Visor_DetectarElementoApuntado",
        "ControladorDelPunto",
        "DATA_Cambios"
    };

        // ==============================
        // 2) EXCLUIR POR CARPETA
        // ==============================
        var excluirCarpetas = new List<string>
    {
        "Assets/Custom Inspector",
        "Assets/StandaloneFileBrowser",
        "Assets/TextMesh Pro"
    };

        rutas = rutas
            .Where(path =>
            {
                var normalized = path.Replace("\\", "/");

                // ❌ Ignorar si pertenece a carpeta prohibida
                foreach (var carpeta in excluirCarpetas)
                {
                    if (normalized.Contains(carpeta))
                        return false;
                }

                // ❌ Ignorar por nombre exacto
                var name = System.IO.Path.GetFileNameWithoutExtension(normalized);
                if (excluirNombres.Contains(name))
                    return false;

                return true;
            })
            .ToList();

        // ==============================
        // DEBUG
        // ==============================
        Debug.Log("📁 Carpetas usadas:\n" +
            string.Join("\n",
                rutas.Select(p => System.IO.Path.GetDirectoryName(p)?.Replace("\\", "/"))
                     .Distinct()
                     .OrderBy(x => x)
            )
        );

        Debug.Log($"🧮 Total final de scripts analizados: {rutas.Count}");

        // ==============================
        // ANALISIS NORMAL
        // ==============================
        var scripts = CSContentService.CargarScriptsEnRAM(rutas);

        var nombres = scripts.Select(s => s.Name).ToList();
        var contenidos = scripts.Select(s => s.Content).ToList();

        var relaciones = CSRelationService.CrearRelacionesPorContains(nombres, contenidos);

        var paths = CSUmlMultiExportService.Guardar4Archivos(relaciones);

        Debug.Log("✅ Exportados SIN excluidos:\n" + string.Join("\n", paths));
    }




}

// =========================
// 1) RUTAS .CS (filtro)
// =========================
public static class CSFileService
{
    public static List<string> ObtenerRutasCS()
    {
        string rootPath = Application.dataPath;
        var archivos = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories);

        return archivos
            .Where(path => !EsRutaIgnorada(path))
            .Select(path => path.Replace("\\", "/"))
            .Distinct()
            .OrderBy(p => p)
            .ToList();
    }

    private static bool EsRutaIgnorada(string path)
    {
        string p = path.Replace("\\", "/");

        return p.Contains("/Editor Default Resources/")
            || p.Contains("/Gizmos/")
            || p.Contains("/Plugins/")
            || p.Contains("/Packages/")
            || p.Contains("/Library/")
            || p.Contains("/Tests/")
            || p.Contains("TextMeshPro")
            || p.Contains("/Standard Assets/");
    }
}

// ==================================
// 2) CARGA EN RAM (Name + Content)
// ==================================
public static class CSContentService
{
    public sealed class ScriptData
    {
        public string Name;
        public string Path;
        public string Content;
        public int CharCount;
        public int LineCount;
    }

    public static List<ScriptData> CargarScriptsEnRAM(List<string> rutasCS)
    {
        var list = new List<ScriptData>(rutasCS.Count);

        foreach (var path in rutasCS)
        {
            string content = SafeReadAllText(path);
            int chars = content.Length;
            int lines = CountLines(content);

            list.Add(new ScriptData
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(path),
                Path = path,
                Content = content,
                CharCount = chars,
                LineCount = lines
            });
        }

        return list;
    }

    private static string SafeReadAllText(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception e)
        {
            return $"// ERROR leyendo archivo: {path}\n// {e.GetType().Name}: {e.Message}\n";
        }
    }

    private static int CountLines(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;

        int count = 0;
        for (int i = 0; i < s.Length; i++)
            if (s[i] == '\n') count++;

        return s.EndsWith("\n", StringComparison.Ordinal) ? count : count + 1;
    }
}

// =========================
// 3) REPORTE (tabla \t)
// =========================
public static class CSReportService
{
    public static string GenerarTablaCharsLineas(List<CSContentService.ScriptData> scripts)
    {
        var lines = new List<string>(scripts.Count + 5)
        {
            "Script\tChars\tLines"
        };

        long totalChars = 0;
        long totalLines = 0;

        foreach (var s in scripts.OrderBy(x => x.Name))
        {
            totalChars += s.CharCount;
            totalLines += s.LineCount;

            lines.Add($"{s.Name}\t{s.CharCount}\t{s.LineCount}");
        }

        lines.Add("--------------------------------");
        lines.Add($"TOTAL\t{totalChars}\t{totalLines}");

        return string.Join("\n", lines);
    }
}

// =======================================
// 4) RELACIONES POR "CONTAINS" INTELIGENTE
// =======================================
public static class CSRelationService
{
    // Salida: "Buscado --> Contenedor"
    public static List<string> CrearRelacionesPorContains(List<string> nombresScripts, List<string> contenidos)
    {
        if (nombresScripts == null || contenidos == null) return new List<string>();
        if (nombresScripts.Count != contenidos.Count) return new List<string>();

        var map = new Dictionary<string, string>(nombresScripts.Count, StringComparer.Ordinal);
        for (int i = 0; i < nombresScripts.Count; i++)
        {
            var name = nombresScripts[i] ?? "";
            if (name.Length == 0) continue;
            if (!map.ContainsKey(name))
                map.Add(name, contenidos[i] ?? "");
        }

        var regexPorNombre = new Dictionary<string, Regex>(map.Count, StringComparer.Ordinal);
        foreach (var name in map.Keys)
            regexPorNombre[name] = BuildReferenceRegex(name);

        var result = new List<string>(map.Count * 2);

        foreach (var buscado in map.Keys.OrderBy(x => x))
        {
            var re = regexPorNombre[buscado];

            foreach (var contenedor in map.Keys)
            {
                if (string.Equals(buscado, contenedor, StringComparison.Ordinal))
                    continue;

                string contenido = map[contenedor];
                if (string.IsNullOrEmpty(contenido)) continue;

                if (re.IsMatch(contenido))
                    result.Add($"{buscado} --> {contenedor}");
            }
        }

        return result.Distinct().OrderBy(x => x).ToList();
    }

    private static Regex BuildReferenceRegex(string scriptName)
    {
        string n = Regex.Escape(scriptName);

        string pattern =
            $@"(?<![A-Za-z0-9_]){n}(?=(\.|=|;|>|[\s,\)\]\}}]))";

        return new Regex(
            pattern,
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );
    }
}

// =========================
// 5) FILTRO POR KEYWORDS (ÚNICO FILTRO)
// =========================
public static class CSKeywordFilterService
{
    /// <summary>
    /// Mantiene SOLO:
    ///   X --> T   donde T está en targets
    /// Eso representa "T depende de X" (lo que T necesita).
    /// Elimina "T --> Y" (quién usa T) = ruido.
    ///
    /// relaciones esperadas: "A --> B" (sin label)
    /// </summary>
    public static List<string> FiltrarSoloDependenciasDeTargets(List<string> relaciones, List<string> targets)
    {
        if (relaciones == null) return new List<string>();
        if (targets == null || targets.Count == 0) return new List<string>(relaciones);

        var targetSet = new HashSet<string>(
            targets.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()),
            StringComparer.Ordinal
        );

        var result = new List<string>();

        foreach (var r in relaciones)
        {
            if (!TryParseRelacion(r, out var a, out var b)) continue;

            // ✅ Solo relaciones donde el "contenedor" (derecha) es uno de los ruidosos
            // (o sea: cosas que ese target usa / necesita)
            if (targetSet.Contains(b))
                result.Add($"{a} --> {b}");
        }

        return result.Distinct().OrderBy(x => x).ToList();
    }

    private static bool TryParseRelacion(string r, out string a, out string b)
    {
        a = ""; b = "";
        if (string.IsNullOrWhiteSpace(r)) return false;

        // Permite "A --> B" o "A --> B : usa"
        var parts = r.Split(new[] { "-->" }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;

        a = parts[0].Trim();

        // Parte derecha puede venir con ": usa"
        b = parts[1].Trim();
        int idx = b.IndexOf(':');
        if (idx >= 0) b = b.Substring(0, idx).Trim();

        return a.Length > 0 && b.Length > 0;
    }
}


// =========================
// 6) MERMAID (classDiagram)  (SIN filtro)
// =========================
public static class CSMermaidService
{
    public static string GenerarMermaidClassDiagram(List<string> relaciones)
    {
        var lines = new List<string>();
        lines.Add("classDiagram");
        lines.Add("");

        var clases = new HashSet<string>(StringComparer.Ordinal);

        foreach (var r in relaciones)
        {
            var parts = r.Split(new[] { "-->" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) continue;

            string a = parts[0].Trim();
            string b = parts[1].Trim();

            if (a.Length > 0) clases.Add(a);
            if (b.Length > 0) clases.Add(b);
        }

        foreach (var c in clases.OrderBy(x => x))
            lines.Add($"class {c}");

        lines.Add("");

        foreach (var r in relaciones.OrderBy(x => x))
        {
            var parts = r.Split(new[] { "-->" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) continue;

            string a = parts[0].Trim();
            string b = parts[1].Trim();

            lines.Add($"{a} --> {b} : usa");
        }

        return string.Join("\n", lines);
    }
}

// =========================
// 7) PLANTUML (SIN filtros)
// =========================
public static class CSPlantUmlService
{
    // 1) PlantUML clásico (PlantText / PlantUML Editor)
    public static string GenerarPlantUml_ClassDiagram(List<string> relaciones)
    {
        var lines = new List<string>();
        lines.Add("@startuml");
        lines.Add("left to right direction");
        lines.Add("");

        var clases = ExtraerClases(relaciones);
        foreach (var c in clases)
            lines.Add($"class {c}");

        lines.Add("");

        foreach (var r in relaciones.OrderBy(x => x))
        {
            if (!TryParseRelacion(r, out var a, out var b)) continue;
            lines.Add($"{a} --> {b} : usa");
        }

        lines.Add("@enduml");
        return string.Join("\n", lines);
    }

    // 2) PlantUML solo relaciones (más liviano)
    public static string GenerarPlantUml_SoloRelaciones(List<string> relaciones)
    {
        var lines = new List<string>();
        lines.Add("@startuml");
        lines.Add("left to right direction");
        lines.Add("");

        foreach (var r in relaciones.OrderBy(x => x))
        {
            if (!TryParseRelacion(r, out var a, out var b)) continue;
            lines.Add($"{a} --> {b} : usa");
        }

        lines.Add("@enduml");
        return string.Join("\n", lines);
    }

    // 3) Server base (igual que lite)
    public static string GenerarPlantUml_ServerBase(List<string> relaciones)
    {
        return GenerarPlantUml_SoloRelaciones(relaciones);
    }

    // -------- helpers --------
    private static bool TryParseRelacion(string r, out string a, out string b)
    {
        a = ""; b = "";
        if (string.IsNullOrWhiteSpace(r)) return false;
        var parts = r.Split(new[] { "-->" }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;
        a = parts[0].Trim();
        b = parts[1].Trim();
        return a.Length > 0 && b.Length > 0;
    }

    private static List<string> ExtraerClases(List<string> relaciones)
    {
        var clases = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in relaciones)
        {
            if (!TryParseRelacion(r, out var a, out var b)) continue;
            clases.Add(a);
            clases.Add(b);
        }
        return clases.OrderBy(x => x).ToList();
    }
}

// =========================
// 8) EXPORT: 4 ARCHIVOS a C:\PlantUML (SIN filtros)
// =========================
public static class CSUmlMultiExportService
{
    private const string ExportFolder = @"C:\PlantUML";

    public static List<string> Guardar4Archivos(List<string> relaciones)
    {
        Directory.CreateDirectory(ExportFolder);

        var outPaths = new List<string>();

        var mermaid = CSMermaidService.GenerarMermaidClassDiagram(relaciones);
        outPaths.Add(WriteFile("01_mermaid_classDiagram", "mmd", mermaid));

        var pumlClass = CSPlantUmlService.GenerarPlantUml_ClassDiagram(relaciones);
        outPaths.Add(WriteFile("02_plantuml_classDiagram", "puml", pumlClass));

        var pumlLite = CSPlantUmlService.GenerarPlantUml_SoloRelaciones(relaciones);
        outPaths.Add(WriteFile("03_plantuml_soloRelaciones", "puml", pumlLite));

        var pumlServer = CSPlantUmlService.GenerarPlantUml_ServerBase(relaciones);
        outPaths.Add(WriteFile("04_plantuml_serverBase", "puml", pumlServer));

        outPaths.Add(WriteFile("05_relaciones_crudas", "txt", string.Join("\n", relaciones)));

        return outPaths;
    }


    private static string WriteFile(string prefix, string ext, string content)
    {
        string fileName = $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
        string fullPath = Path.Combine(ExportFolder, fileName);
        File.WriteAllText(fullPath, content);
        return fullPath.Replace("\\", "/");
    }
}


public static class CSNoiseService
{
    public sealed class NodeNoise
    {
        public string Name;
        public int Out;   // aparece como A
        public int In;    // aparece como B
        public int Total; // In + Out
    }

    // relaciones: ["A --> B", ...]
    public static List<NodeNoise> CalcularRuido(List<string> relaciones)
    {
        var map = new Dictionary<string, NodeNoise>(StringComparer.Ordinal);

        foreach (var r in relaciones)
        {
            if (!TryParse(r, out var a, out var b)) continue;

            var na = GetOrCreate(map, a);
            var nb = GetOrCreate(map, b);

            na.Out++;
            nb.In++;
        }

        foreach (var kv in map)
            kv.Value.Total = kv.Value.In + kv.Value.Out;

        return map.Values
            .OrderByDescending(x => x.Total)
            .ThenByDescending(x => x.Out)
            .ThenBy(x => x.Name)
            .ToList();
    }

    public static string ReportTop(List<string> relaciones, int top = 20)
    {
        var noise = CalcularRuido(relaciones);
        if (top <= 0) top = noise.Count;

        var lines = new List<string>();
        lines.Add("Top\tScript\tTotal\tOut\tIn");

        for (int i = 0; i < Math.Min(top, noise.Count); i++)
        {
            var n = noise[i];
            lines.Add($"{i + 1}\t{n.Name}\t{n.Total}\t{n.Out}\t{n.In}");
        }

        return string.Join("\n", lines);
    }

    public static List<string> KeywordsTop(List<string> relaciones, int top = 20)
    {
        return CalcularRuido(relaciones)
            .Take(Math.Max(0, top))
            .Select(x => x.Name)
            .ToList();
    }

    private static bool TryParse(string r, out string a, out string b)
    {
        a = ""; b = "";
        if (string.IsNullOrWhiteSpace(r)) return false;

        var parts = r.Split(new[] { "-->" }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;

        a = parts[0].Trim();
        b = parts[1].Trim();
        return a.Length > 0 && b.Length > 0;
    }

    private static NodeNoise GetOrCreate(Dictionary<string, NodeNoise> map, string name)
    {
        if (!map.TryGetValue(name, out var n))
        {
            n = new NodeNoise { Name = name };
            map[name] = n;
        }
        return n;
    }
}


public static class CSExcludePathFilterService
{
    // 👇 EDITÁ SOLO ESTA LISTA
    public static List<string> ExcludedNames = new List<string>
    {
        "Pepe1",
        "Jugador",
        "GameManager"
    };

    public static List<string> FiltrarRutas(List<string> rutas)
    {
        if (rutas == null || rutas.Count == 0)
            return new List<string>();

        var excludeSet = new HashSet<string>(
            ExcludedNames.Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.Ordinal
        );

        return rutas
            .Where(path =>
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                return !excludeSet.Contains(fileName);
            })
            .ToList();
    }
}