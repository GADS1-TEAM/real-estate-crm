using System.Text.Json;
using RealEstateCrm.Contracts.Errors;
using RealEstateCrm.Contracts.Serialization;

namespace RealEstateCrm.ContractTests.Errors;

public class ProblemDetailsV1ContractTests
{
    private const string FrozenProblemJson = """
        {
          "type": "about:blank",
          "title": "El token no es válido.",
          "status": 401,
          "detail": "El token expiró.",
          "instance": "/api/parties",
          "errorCode": "unauthorized",
          "correlationId": "1a2b3c4d-5e6f-4a1b-9c2d-3e4f5a6b7c22"
        }
        """;

    [Fact]
    public void Deserializes_frozen_json_with_stable_shape()
    {
        var problem = JsonSerializer.Deserialize<ProblemDetailsV1>(FrozenProblemJson, RealEstateCrmJsonDefaults.Options);

        Assert.NotNull(problem);
        Assert.Equal("about:blank", problem.Type);
        Assert.Equal(401, problem.Status);
        Assert.Equal(ErrorCodes.Unauthorized, problem.ErrorCode);
        Assert.Equal(Guid.Parse("1a2b3c4d-5e6f-4a1b-9c2d-3e4f5a6b7c22"), problem.CorrelationId);
    }

    [Theory]
    [InlineData(ErrorCodes.ValidationError)]
    [InlineData(ErrorCodes.Unauthorized)]
    [InlineData(ErrorCodes.Forbidden)]
    [InlineData(ErrorCodes.NotFound)]
    [InlineData(ErrorCodes.Conflict)]
    public void ErrorCodes_round_trip_as_declared_string(string errorCode)
    {
        var problem = new ProblemDetailsV1("about:blank", "t", 400, null, null, errorCode, Guid.NewGuid());

        var json = JsonSerializer.Serialize(problem, RealEstateCrmJsonDefaults.Options);
        var roundTripped = JsonSerializer.Deserialize<ProblemDetailsV1>(json, RealEstateCrmJsonDefaults.Options);

        Assert.Equal(errorCode, roundTripped!.ErrorCode);
    }

    [Fact]
    public void Validation_errors_serialize_as_field_to_messages_map()
    {
        var problem = new ProblemDetailsV1(
            "about:blank", "Validación fallida", 400, null, null, ErrorCodes.ValidationError, Guid.NewGuid(),
            Errors: new Dictionary<string, string[]> { ["email"] = ["Formato inválido"] });

        var json = JsonSerializer.Serialize(problem, RealEstateCrmJsonDefaults.Options);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.TryGetProperty("errors", out var errorsElement));
        Assert.True(errorsElement.TryGetProperty("email", out var emailErrors));
        Assert.Equal("Formato inválido", emailErrors[0].GetString());
    }
}
