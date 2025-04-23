var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // bu önemli!
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Swagger JSON dosyasýný üretir
    app.UseSwaggerUI(); // Arayüzü sunar
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

