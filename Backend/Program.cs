using Backend.DataContext;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();
string? cadenaConexion = configuration.GetConnectionString("mysqlLocal");

//Configuramos la inyeccion de dependencias del DbContext
builder.Services.AddDbContext<SistemaGestionContext>(
    options => options.UseMySql(cadenaConexion,
                               ServerVersion.AutoDetect(cadenaConexion),
                               options => options.EnableRetryOnFailure(
                                   maxRetryCount: 5,
                                   maxRetryDelay: System.TimeSpan.FromSeconds(30),
                                   errorNumbersToAdd: null) //habilitamos reintentos automáticos en
));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer(); //puntos de respuestas que tiene la API
builder.Services.AddSwaggerGen(); //para generar la documentacion de la API

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
