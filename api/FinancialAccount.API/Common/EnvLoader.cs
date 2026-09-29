namespace FinancialAccount.API.Common;

public static class EnvLoader
{
    public static void Load(string? path = null)
    {
        var envFilePath = path;
        
        if (string.IsNullOrEmpty(envFilePath))
        {
            // Procura o arquivo .env no diretório atual ou nos diretórios pai (raiz da solução)
            var currentDir = Directory.GetCurrentDirectory();
            var searchDirs = new[]
            {
                currentDir,
                Path.Combine(currentDir, ".."),
                Path.Combine(currentDir, "../.."),
                AppDomain.CurrentDomain.BaseDirectory,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../..")
            };

            foreach (var dir in searchDirs)
            {
                var target = Path.GetFullPath(Path.Combine(dir, ".env"));
                if (File.Exists(target))
                {
                    envFilePath = target;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(envFilePath) || !File.Exists(envFilePath))
        {
            return;
        }

        foreach (var line in File.ReadAllLines(envFilePath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var parts = trimmed.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                // Remove aspas simples ou duplas se presentes
                if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
                {
                    value = value[1..^1];
                }

                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
    }
}
