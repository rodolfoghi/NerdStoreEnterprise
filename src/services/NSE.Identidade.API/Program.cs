using NSE.Identidade.API.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddIdentityConfiguration(builder.Configuration)
    .AddApiConfiguration()
    .AddSwaggerConfiguration();

var app = builder.Build();

app.UseSwaggerConfiguration();
app.UseApiConfiguration(builder.Environment);

app.Run();