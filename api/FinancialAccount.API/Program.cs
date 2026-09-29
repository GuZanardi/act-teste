using System.Reflection;
using FinancialAccount.API.Data;
using FinancialAccount.API.Middlewares;
using FinancialAccount.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuração do CORS para permitir comunicação com o frontend React
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configuração do Banco de Dados MySQL com EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
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
        Description = "API RESTful para controle de movimentações financeiras de conta empresarial (entradas, saídas, consulta de saldo e extrato com consistência de saldo).",
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

// Execução automática de migrações / criação do schema e seed ao inicializar
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<AppDbContext>();
    
    // Tenta conectar ao banco aguardando o container MySQL estar pronto
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
                logger.LogError(ex, "Falha crítica ao conectar no banco de dados após várias tentativas.");
                throw;
            }
            logger.LogWarning("Aguardando banco de dados ficar pronto... Tentativa {Current}/{Max}", i + 1, maxRetries);
            await Task.Delay(retryDelay);
        }
    }
}

// Middleware de tratamento global de exceções
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Swagger habilitado tanto em dev quanto em prod para facilidade de avaliação
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Financial Account API v1");
    c.RoutePrefix = string.Empty; // Define o Swagger na raiz da API (ex: http://localhost:5000/)
});

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
