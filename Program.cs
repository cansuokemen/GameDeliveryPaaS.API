var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // bu �nemli!
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Swagger JSON dosyas�n� �retir
    app.UseSwaggerUI(); // Aray�z� sunar
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

