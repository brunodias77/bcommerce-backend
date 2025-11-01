using System.Text.Json.Serialization;
using BuildingBlocks.Core.Validations;

namespace BuildingBlocks.Core.Responses;

public class ApiResponse<T>
{
    public bool Success { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T Data { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Error>? Errors { get; }

    protected ApiResponse(bool success, T data = default!, string? message = null, List<Error>? errors = null)
    {
        Success = success;
        Data = data;
        Message = message;
        Errors = errors?.Any() == true ? errors : null;
    }

    // Sucesso com dados
    public static ApiResponse<T> Ok(T data, string? message = null)
        => new(true, data, message);

    // Falha com erro único
    public static ApiResponse<T> Fail(string message)
        => new(false, default!, null, [new Error(message)]);

    // Falha com múltiplos erros
    public static ApiResponse<T> Fail(List<Error> errors)
        => new(false, default!, null, errors);

}

// Versão sem generic para respostas sem dados
public class ApiResponse : ApiResponse<object>
{
    private ApiResponse(bool success, string? message = null, List<Error>? errors = null)
        : base(success, null!, message, errors) { }

    public static ApiResponse Ok(string? message = null)
        => new(true, message);

    public static new ApiResponse Fail(string message)
        => new(false, null, [new Error(message)]);

    public static new ApiResponse Fail(List<Error> errors)
        => new(false, null, errors);

}