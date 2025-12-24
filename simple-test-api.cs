using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Simple test endpoint
app.MapGet("/", () => "Umi Health API is running");

// Simple account creation test
app.MapPost("/test/register", (RegisterRequest request) => {
    var response = new {
        success = true,
        message = "Test account creation endpoint is working",
        received = new {
            email = request.Email,
            firstName = request.FirstName,
            lastName = request.LastName,
            phoneNumber = request.PhoneNumber,
            tenantId = request.TenantId,
            branchId = request.BranchId
        }
    };
    
    return Results.Ok(response);
});

app.Run();

public record RegisterRequest(
    string Email,
    string Password,
    string ConfirmPassword,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string TenantId,
    string BranchId
);
