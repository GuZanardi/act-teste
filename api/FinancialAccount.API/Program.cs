using System.Reflection;
using FinancialAccount.API.Common;
using FinancialAccount.API.Data;
using FinancialAccount.API.Middlewares;
using FinancialAccount.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

// 1. Carrega variáveis de ambiente a partir do arquivo .env
EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

// 2. Configuração do CORS para permitir comunicação com o frontend React
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 3. Montagem da Connection String exclusivamente a partir das variáveis de ambiente (.env)
var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3307";
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    if (string.IsNullOrWhiteSpace(dbServer) || string.IsNullOrWhiteSpace(dbName) || string.IsNullOrWhiteSpace(dbUser) || string.IsNullOrWhiteSpace(dbPassword))
    {
        throw new InvalidOperationException("Credenciais do banco de dados não foram configuradas. Defina as variáveis DB_SERVER, DB_PORT, DB_NAME, DB_USER e DB_PASSWORD exclusivamente no arquivo .env.");
    }

    connectionString = $"Server={dbServer};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};";
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
        mySqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
    });
});

// Injeção de dependências dos serviços
builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configuração do Swagger com documentação descritiva
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Financial Account API - Desafio Técnico ACT Digital",
        Version = "v1",
        Description = "API RESTful para controle de movimentações financeiras de conta empresarial (entradas, saídas, consulta de saldo e extrato com consistência de saldo e EF Core Migrations).",
        Contact = new OpenApiContact
        {
            Name = "Desafio act digital",
            Email = "contato@actdigital.com"
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// 4. Execução automática de Migrações (EF Core Migrations) e Seed ao inicializar
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<AppDbContext>();
    
    // Aguarda o container do MySQL estar saudável e aplica migrações
    var maxRetries = 12;
    var retryDelay = TimeSpan.FromSeconds(5);
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await DbInitializer.InitializeAsync(context, logger);
            break;
        }
        catch (Exception ex)
        {
            if (i == maxRetries - 1)
            {
                logger.LogError(ex, "Falha crítica ao conectar no banco de dados e aplicar migrações após várias tentativas.");
                throw;
            }
            logger.LogWarning("Aguardando banco de dados para aplicar migrações... Tentativa {Current}/{Max}", i + 1, maxRetries);
            await Task.Delay(retryDelay);
        }
    }
}

// Middleware de tratamento global de exceções
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Swagger habilitado na raiz da API (ex: http://localhost:5001/)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Financial Account API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
