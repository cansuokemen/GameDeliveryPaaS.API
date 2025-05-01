using Microsoft.Extensions.Options;
using MongoDB.Driver;
using GameDeliveryPaaS.API.Settings;
using GameDeliveryPaaS.API.Services;
using MongoDB.Bson.Serialization;
using GameDeliveryPaaS.API.Models;

var builder = WebApplication.CreateBuilder(args);

// 👇 Bson ClassMap: UserGamePlay modelini açıkça MongoDB'ye tanıt
if (!BsonClassMap.IsClassMapRegistered(typeof(UserGamePlay)))
{
    BsonClassMap.RegisterClassMap<UserGamePlay>(cm =>
    {
        cm.AutoMap();
        cm.SetIgnoreExtraElements(true);
    });
}

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
builder.Services.AddSingleton<UserService>();

var app = builder.Build();

// Swagger arayüzü ve JSON
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
