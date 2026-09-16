using RealEstateCrm.BuildingBlocks.Infrastructure.Observability;

namespace RealEstateCrm.BuildingBlocks.Infrastructure.Tests.Observability;

/// <summary>
/// OPS-005: un escaneo de logs no debe encontrar password, token, documento ni texto de
/// actividad. Cubre inputs hostiles (mayúsculas mezcladas, comillas, JSON embebido, valores
/// vacíos/nulos/larguísimos) además del caso feliz.
/// </summary>
public class SensitiveDataRedactorTests
{
    [Theory]
    [InlineData("password=hunter2", "password=[REDACTED]")]
    [InlineData("PASSWORD=hunter2", "PASSWORD=[REDACTED]")]
    [InlineData("Password: hunter2", "Password: [REDACTED]")]
    [InlineData("\"password\": \"hunter2\"", "\"password\": [REDACTED]")]
    [InlineData("token=eyJhbGciOiJSUzI1NiIsInR5cCI", "token=[REDACTED]")]
    [InlineData("accessToken: abc.def.ghi", "accessToken: [REDACTED]")]
    [InlineData("clientSecret=s3cr3t-value", "clientSecret=[REDACTED]")]
    [InlineData("documento=30111222", "documento=[REDACTED]")]
    [InlineData("DNI: 12345678", "DNI: [REDACTED]")]
    [InlineData("cuit=20304050607", "cuit=[REDACTED]")]
    [InlineData("activityText=\"Llamé al cliente y le confirmé la visita\"", "activityText=[REDACTED]")]
    [InlineData("texto: 'nota interna sensible'", "texto: [REDACTED]")]
    public void Redacts_known_sensitive_keys(string input, string expected)
    {
        Assert.Equal(expected, SensitiveDataRedactor.Redact(input));
    }

    [Fact]
    public void Does_not_redact_correlationId_or_actorId()
    {
        var input = "correlationId=1a2b3c4d actorId=9f8e7d6c message=hello";

        Assert.Equal(input, SensitiveDataRedactor.Redact(input));
    }

    [Fact]
    public void Redacts_multiple_sensitive_fields_in_the_same_message_without_touching_the_rest()
    {
        var input = "user=ana password=hunter2 correlationId=cid-1 token=tok-1 status=ok";

        var redacted = SensitiveDataRedactor.Redact(input);

        Assert.Contains("user=ana", redacted);
        Assert.Contains("correlationId=cid-1", redacted);
        Assert.Contains("status=ok", redacted);
        Assert.DoesNotContain("hunter2", redacted);
        Assert.DoesNotContain("tok-1", redacted);
    }

    [Fact]
    public void Null_input_returns_empty_string()
    {
        Assert.Equal(string.Empty, SensitiveDataRedactor.Redact(null));
    }

    [Fact]
    public void Empty_input_returns_empty_string()
    {
        Assert.Equal(string.Empty, SensitiveDataRedactor.Redact(string.Empty));
    }

    [Fact]
    public void Message_without_sensitive_keys_is_returned_unchanged()
    {
        var input = "PartyRegistered publicado correctamente";

        Assert.Equal(input, SensitiveDataRedactor.Redact(input));
    }

    [Fact]
    public void Very_long_sensitive_value_is_fully_redacted()
    {
        var longSecret = new string('x', 10_000);

        var redacted = SensitiveDataRedactor.Redact($"token={longSecret}");

        Assert.DoesNotContain(longSecret, redacted);
        Assert.Contains("token=[REDACTED]", redacted);
    }

    [Fact]
    public void Redacts_sensitive_value_containing_unicode_and_special_characters()
    {
        var redacted = SensitiveDataRedactor.Redact("password=cóntraseñá!#$%_@ñ");

        Assert.DoesNotContain("cóntraseñá", redacted);
    }

    [Fact]
    public void Json_style_payload_with_multiple_sensitive_fields_is_redacted()
    {
        var json = """{"userId":"u-1","password":"hunter2","token":"abc123","correlationId":"cid-1"}""";

        var redacted = SensitiveDataRedactor.Redact(json);

        Assert.DoesNotContain("hunter2", redacted);
        Assert.DoesNotContain("abc123", redacted);
        Assert.Contains("cid-1", redacted);
        Assert.Contains("u-1", redacted);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("Password")]
    [InlineData("PASSWORD")]
    [InlineData("token")]
    [InlineData("documento")]
    [InlineData("dni")]
    [InlineData("activityText")]
    public void IsSensitiveKey_matches_known_keys_case_insensitively(string key)
    {
        Assert.True(SensitiveDataRedactor.IsSensitiveKey(key));
    }

    [Fact]
    public void IsSensitiveKey_does_not_match_unrelated_keys()
    {
        Assert.False(SensitiveDataRedactor.IsSensitiveKey("displayName"));
        Assert.False(SensitiveDataRedactor.IsSensitiveKey("correlationId"));
    }
}
