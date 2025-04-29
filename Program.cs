using Microsoft.Extensions.Options;
using MongoDB.Driver;
using GameDeliveryPaaS.API.Settings;
using GameDeliveryPaaS.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // bu önemli!
builder.Services.AddSwaggerGen();

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings")
);

builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;

    var mongoSettings = MongoClientSettings.FromUrl(new MongoUrl(settings.ConnectionString));
    mongoSettings.SslSettings = new SslSettings { EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 };

    return new MongoClient(mongoSettings);
});

builder.Services.AddSingleton(serviceProvider =>
{
    var mongoClient = serviceProvider.GetRequiredService<IMongoClient>();
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return mongoClient.GetDatabase(settings.DatabaseName);
});
builder.Services.AddSingleton<GameService>();


var app = builder.Build();

// Configure the HTTP request pipeline.


app.UseSwagger(); // Swagger JSON dosyasını üretir
app.UseSwaggerUI(); // Arayüzü sunar


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

