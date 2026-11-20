namespace SistemaGestion.API.Configuration;

/// <summary>
/// Carga el archivo <c>.env</c> de la raíz del repositorio (gitignored,
/// AGENTS.md §46) como variables de entorno del proceso ANTES de construir
/// el host, para que la capa de configuración normal las lea con la
/// prioridad correcta: una variable ya existente en el entorno nunca se
/// pisa. Si no hay archivo, no hace nada (producción usa las variables
/// reales del entorno).
///
/// Los nombres documentados en <c>.env.example</c> (FIREBASE_*,
/// DATABASE_CONNECTION_STRING, ...) se traducen a las claves que lee la
/// aplicación (Firebase__*, ConnectionStrings__*, ...); cualquier otra
/// clave se copia tal cual.
/// </summary>
public static class LocalEnvFileLoader
{
    private static readonly Dictionary<string, string> Traducciones = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DATABASE_CONNECTION_STRING"] = "ConnectionStrings__DefaultConnection",
        ["FIREBASE_PROJECT_ID"] = "Firebase__ProjectId",
        ["FIREBASE_CLIENT_EMAIL"] = "Firebase__ClientEmail",
        ["FIREBASE_PRIVATE_KEY"] = "Firebase__PrivateKey",
        ["FIREBASE_SERVICE_ACCOUNT_JSON"] = "Firebase__ServiceAccountJson",
        ["FIREBASE_STORAGE_BUCKET"] = "Firebase__StorageBucket",
        ["CORS_ALLOWED_ORIGINS"] = "Cors__AllowedOrigins",
    };

    /// <summary>
    /// Busca <c>.env</c> subiendo desde el directorio de trabajo y desde
    /// <c>AppContext.BaseDirectory</c>. Devuelve la ruta cargada, o null.
    /// </summary>
    public static string? Load()
    {
        var ruta = FindEnvFile();
        if (ruta is null)
        {
            return null;
        }

        foreach (var linea in File.ReadLines(ruta))
        {
            var texto = linea.Trim();
            if (texto.Length == 0 || texto.StartsWith('#'))
            {
                continue;
            }

            var separador = texto.IndexOf('=');
            if (separador <= 0)
            {
                continue;
            }

            var clave = texto[..separador].Trim();
            var valor = QuitarComillas(texto[(separador + 1)..].Trim());
            var destino = Traducciones.TryGetValue(clave, out var traducida) ? traducida : clave;

            // Cors:AllowedOrigins es un array: "a.com,b.com" → índice 0, 1...
            if (destino.Equals("Cors__AllowedOrigins", StringComparison.OrdinalIgnoreCase)
                && valor.Contains(','))
            {
                var origenes = valor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                for (var i = 0; i < origenes.Length; i++)
                {
                    SetIfMissing($"{destino}__{i}", origenes[i]);
                }

                continue;
            }

            SetIfMissing(destino, valor);
        }

        return ruta;
    }

    private static void SetIfMissing(string clave, string valor)
    {
        if (Environment.GetEnvironmentVariable(clave) is null)
        {
            Environment.SetEnvironmentVariable(clave, valor, EnvironmentVariableTarget.Process);
        }
    }

    private static string QuitarComillas(string valor)
    {
        if (valor.Length >= 2)
        {
            if ((valor[0] == '"' && valor[^1] == '"') || (valor[0] == '\'' && valor[^1] == '\''))
            {
                return valor[1..^1];
            }
        }

        return valor;
    }

    private static string? FindEnvFile()
    {
        foreach (var inicio in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(inicio);
            for (var niveles = 0; niveles < 8 && dir is not null; niveles++, dir = dir.Parent)
            {
                var ruta = Path.Combine(dir.FullName, ".env");
                if (File.Exists(ruta))
                {
                    return ruta;
                }
            }
        }

        return null;
    }
}
