using TradeCopilot.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTradeCopilotApi(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("local-client");
app.MapControllers();

app.Run();
