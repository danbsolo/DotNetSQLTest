using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
//

var app = builder.Build();

//
app.UseAuthentication();
app.UseAuthorization();
//

// For production scenarios, consider keeping Swagger configurations behind the environment check
// if (app.Environment.IsDevelopment())
// {
    app.UseSwagger();
    app.UseSwaggerUI();
// }

//app.UseHttpsRedirection();

string connectionString = app.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING")!;

try
{
    // Table would be created ahead of time in production
    using var conn = new SqlConnection(connectionString);
    conn.Open();

    var command = new SqlCommand(
        "CREATE TABLE Persons (ID int NOT NULL PRIMARY KEY IDENTITY, FirstName varchar(255), LastName varchar(255));",
        conn);
    using SqlDataReader reader = command.ExecuteReader();
}
catch (Exception e)
{
    // Table may already exist
    Console.WriteLine(e.Message);
}

app.MapGet("/Person", () => {
    var rows = new List<string>();

    using var conn = new SqlConnection(connectionString);
    conn.Open();

    var command = new SqlCommand("SELECT * FROM Persons", conn);
    using SqlDataReader reader = command.ExecuteReader();

    if (reader.HasRows)
    {
        while (reader.Read())
        {
            rows.Add($"{reader.GetInt32(0)}, {reader.GetString(1)}, {reader.GetString(2)}");
        }
    }

    return rows;
})
.WithName("GetPersons")
.WithOpenApi();

app.MapPost("/Person", (Person person) => {
    using var conn = new SqlConnection(connectionString);
    conn.Open();

    var command = new SqlCommand(
        "INSERT INTO Persons (firstName, lastName) VALUES (@firstName, @lastName)",
        conn);

    command.Parameters.Clear();
    command.Parameters.AddWithValue("@firstName", person.FirstName);
    command.Parameters.AddWithValue("@lastName", person.LastName);

    using SqlDataReader reader = command.ExecuteReader();
})
.WithName("CreatePerson")
.WithOpenApi();

//
app.MapGet("/userInfo", (HttpContext context) =>
{
    var user = context.User;

    if (!user.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    var response = new
    {
        Name = user.Identity.Name,
        Claims = user.Claims.Select(c => new
        {
            Type = c.Type,
            Value = c.Value
        })
    };

    return Results.Json(response);
});
//.RequireAuthorization();
//
app.MapGet("/debug", (HttpContext ctx) =>
{
    return Results.Json(ctx.User.Claims.Select(c => new { c.Type, c.Value }));
});
//


app.MapGet("/", () => "Hello World! Go to /swagger or /userInfo");

app.Run();

public class Person
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}

//"AZURE_SQL_CONNECTIONSTRING": "Server=tcp:sandbox-db-server2.database.windows.net,1433;Initial Catalog=mySampleDatabase;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";"
//"AZURE_SQL_CONNECTIONSTRING": "Server=tcp:sandbox-db-server2.database.windows.net,1433;Initial Catalog=mySampleDatabase;User ID=testuser;Password=StrongPassword123!;Encrypt=True;TrustServerCertificate=False;Authentication=SqlPassword;"

