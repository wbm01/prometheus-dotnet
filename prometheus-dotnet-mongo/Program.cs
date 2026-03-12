using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

var mongoConnection = "mongodb://admin:admin123@mongo:27017/?authSource=admin";
var mongoClient = new MongoClient(mongoConnection);
var database = mongoClient.GetDatabase("testdb");
var users = database.GetCollection<User>("users");

builder.Services.AddSingleton(users);

var app = builder.Build();

// Middleware de m�tricas HTTP
app.UseHttpMetrics();

// Endpoint do Prometheus
app.MapMetrics();

// M�tricas customizadas
var usersCreatedCounter = Metrics.CreateCounter(
    "app_users_created_total",
    "Total de usu�rios criados");

var usersListedCounter = Metrics.CreateCounter(
    "app_users_listed_total",
    "Total de consultas de usu�rios");

// Criar usu�rio
app.MapPost("/users", async ([FromBody] User user, IMongoCollection<User> users) =>
{
    await users.InsertOneAsync(user);
    usersCreatedCounter.Inc();

    return Results.Ok(user);
});

// Listar usu�rios
app.MapGet("/users", async (IMongoCollection<User> users) =>
{
    var result = await users.Find(_ => true).ToListAsync();

    usersListedCounter.Inc();

    return Results.Ok(result);
});

app.Run();

public class User
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}