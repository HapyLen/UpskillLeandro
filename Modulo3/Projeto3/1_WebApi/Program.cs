using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebAPI.Model;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{//
	options.AddPolicy("AllowAll",
		policy => policy.AllowAnyOrigin()
						.AllowAnyMethod()
						.AllowAnyHeader());
});

builder.Services.AddAuthorization();

// Read settings from appsettings.json
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection");
var jwtSettings = configuration.GetSection("JwtSettings");
Model model = new Model(connectionString);
methodsMapping map_method = new methodsMapping();

var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]); // Replace with your secret key
builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = jwtSettings["Issuer"],
		ValidAudience = jwtSettings["Audience"],
		IssuerSigningKey = new SymmetricSecurityKey(key)
	};
});

var app = builder.Build();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", (JsonElement jsonRes) =>
{
	try
	{
		var isAuthenticated = model.Login(jsonRes);
		if (isAuthenticated)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin") }),
				Expires = DateTime.UtcNow.AddHours(1),
				Issuer = jwtSettings["Issuer"],
				Audience = jwtSettings["Audience"],
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};
			var token = tokenHandler.CreateToken(tokenDescriptor);
			var tokenString = tokenHandler.WriteToken(token);

			return Results.Ok(new { Token = tokenString });
		}
		else
			return Results.Ok("");
			// return Results.Unauthorized();
	}
	catch (Exception ex)
	{
        return Results.BadRequest(ex.Message);
	}
})
.WithName("Login")
.WithOpenApi();


app.MapGet("/items/{tab}/{search}/{page}", (string tab, string? search = null, string? page = null) =>
{
    try
	{
		return Results.Ok(model.GetData(tab, search, page));
	}
	catch (Exception ex)
	{
        return Results.BadRequest(ex.Message);
	}
})
.WithName("Items")
.WithOpenApi()
.RequireAuthorization();

app.MapGet("/methods/{tab}", (string tab) =>
{
    try
    {
        return Results.Ok(map_method.GetMethods(tab, "all"));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
})
.WithName("Methods")
.WithOpenApi()
.RequireAuthorization();

app.MapPost("/ResolveMethod/{method}", (string method, JsonElement param) =>
{
	try
	{
		model.MethodCaller(method, param);
		return Results.Ok("Sucesso!");
	}
	catch (Exception ex)
	{
		return Results.BadRequest(ex.Message);
	}
})
.WithName("ResolveMethod")
.WithOpenApi()
.RequireAuthorization();

app.UseCors("AllowAll");
app.Run();