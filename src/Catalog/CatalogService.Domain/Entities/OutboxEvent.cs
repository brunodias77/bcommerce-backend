using System.Text.Json;
using System.Text.RegularExpressions;
using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;
using CatalogService.Domain.Enums;

namespace CatalogService.Domain.Entities;

public class OutboxEvent : Entity
{
    public Guid AggregateId { get; private set; }
    public string AggregateType { get; private set; }
    public string EventType { get; private set; }
    public int EventVersion { get; private set; }
    public string Payload { get; private set; } // JSON
    public string Metadata { get; private set; } // JSON
    public OutboxStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    private OutboxEvent() 
    {
        AggregateType = string.Empty;
        EventType = string.Empty;
        Payload = string.Empty;
        Metadata = string.Empty;
    }

    public static OutboxEvent Create(
        Guid aggregateId,
        string aggregateType,
        string eventType,
        int eventVersion,
        string payload,
        string metadata = "{}",
        int maxRetries = 3)
    {
        if (aggregateId == Guid.Empty)
            throw new ArgumentException("AggregateId cannot be empty", nameof(aggregateId));

        if (string.IsNullOrWhiteSpace(aggregateType))
            throw new ArgumentException("AggregateType is required", nameof(aggregateType));

        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType is required", nameof(eventType));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is required", nameof(payload));

        if (eventVersion < 1)
            throw new ArgumentException("EventVersion must be positive", nameof(eventVersion));

        if (maxRetries < 0)
            throw new ArgumentException("MaxRetries cannot be negative", nameof(maxRetries));

        var outboxEvent = new OutboxEvent
        {
            AggregateId = aggregateId,
            AggregateType = aggregateType,
            EventType = eventType,
            EventVersion = eventVersion,
            Payload = payload,
            Metadata = metadata,
            Status = OutboxStatus.Pending,
            RetryCount = 0,
            MaxRetries = maxRetries,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var validationResult = outboxEvent.Validate();
        if (validationResult.HasErrors)
        {
            throw new ArgumentException($"Dados inválidos: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
        }

        return outboxEvent;
    }
    
    public override ValidationHandler Validate()
    {
        var handler = new ValidationHandler();
        
        // Validar AggregateId
        if (AggregateId == Guid.Empty)
            handler.Add("ID do agregado é obrigatório");
        
        // Validar AggregateType
        if (string.IsNullOrEmpty(AggregateType))
            handler.Add("Tipo do agregado é obrigatório");
        else if (string.IsNullOrWhiteSpace(AggregateType))
            handler.Add("Tipo do agregado não pode conter apenas espaços em branco");
        else if (AggregateType.Length > 100)
            handler.Add("Tipo do agregado deve ter no máximo 100 caracteres");
        else if (!IsValidAggregateType(AggregateType))
            handler.Add("Tipo do agregado deve conter apenas letras, números, pontos e underscores");
        
        // Validar EventType
        if (string.IsNullOrEmpty(EventType))
            handler.Add("Tipo do evento é obrigatório");
        else if (string.IsNullOrWhiteSpace(EventType))
            handler.Add("Tipo do evento não pode conter apenas espaços em branco");
        else if (EventType.Length > 200)
            handler.Add("Tipo do evento deve ter no máximo 200 caracteres");
        else if (!IsValidEventType(EventType))
            handler.Add("Tipo do evento deve conter apenas letras, números, pontos e underscores");
        
        // Validar EventVersion
        if (EventVersion <= 0)
            handler.Add("Versão do evento deve ser maior que zero");
        
        // Validar Payload
        if (string.IsNullOrEmpty(Payload))
            handler.Add("Payload é obrigatório");
        else if (Payload.Length > 10000)
            handler.Add("Payload deve ter no máximo 10000 caracteres");
        else if (!IsValidJson(Payload))
            handler.Add("Payload deve ser um JSON válido");
        
        // Validar Metadata
        if (string.IsNullOrEmpty(Metadata))
            handler.Add("Metadata é obrigatório");
        else if (Metadata.Length > 2000)
            handler.Add("Metadata deve ter no máximo 2000 caracteres");
        else if (!IsValidJson(Metadata))
            handler.Add("Metadata deve ser um JSON válido");
        
        // Validar Status (enum sempre será válido, mas verificamos regras de negócio)
        if (!Enum.IsDefined(typeof(OutboxStatus), Status))
            handler.Add("Status deve ser um valor válido");
        
        // Validar RetryCount
        if (RetryCount < 0)
            handler.Add("Contagem de tentativas deve ser maior ou igual a zero");
        else if (RetryCount > MaxRetries)
            handler.Add("Contagem de tentativas não pode ser maior que o máximo de tentativas");
        
        // Validar MaxRetries
        if (MaxRetries < 0)
            handler.Add("Máximo de tentativas deve ser maior ou igual a zero");
        
        // Validar ErrorMessage
        if (!string.IsNullOrEmpty(ErrorMessage) && ErrorMessage.Length > 1000)
            handler.Add("Mensagem de erro deve ter no máximo 1000 caracteres");
        
        if (Status == OutboxStatus.Failed && string.IsNullOrEmpty(ErrorMessage))
            handler.Add("Mensagem de erro é obrigatória quando o status é Failed");
        
        // Validar PublishedAt
        if (Status == OutboxStatus.Published && !PublishedAt.HasValue)
            handler.Add("Data de publicação é obrigatória quando o status é Published");
        
        if (PublishedAt.HasValue)
        {
            if (PublishedAt.Value > DateTime.UtcNow.AddMinutes(1))
                handler.Add("Data de publicação não pode estar no futuro");
            
            if (CreatedAt != default(DateTime) && PublishedAt.Value < CreatedAt)
                handler.Add("Data de publicação deve ser maior ou igual à data de criação");
        }
        
        // Validar CreatedAt
        if (CreatedAt == default(DateTime))
            handler.Add("Data de criação é obrigatória");
        else if (CreatedAt > DateTime.UtcNow.AddMinutes(1))
            handler.Add("Data de criação não pode estar no futuro");
        
        // Validar UpdatedAt
        if (UpdatedAt == default(DateTime))
            handler.Add("Data de atualização é obrigatória");
        else if (UpdatedAt > DateTime.UtcNow.AddMinutes(1))
            handler.Add("Data de atualização não pode estar no futuro");
        
        // Validar relação entre CreatedAt e UpdatedAt
        if (CreatedAt != default(DateTime) && UpdatedAt != default(DateTime) && UpdatedAt < CreatedAt)
            handler.Add("Data de atualização deve ser maior ou igual à data de criação");
        
        return handler;
    }
    
    private static bool IsValidEventType(string eventType)
    {
        // EventType deve conter apenas letras, números, pontos e underscores
        var eventTypePattern = @"^[a-zA-Z0-9._]+$";
        return Regex.IsMatch(eventType, eventTypePattern);
    }
    
    private static bool IsValidAggregateType(string aggregateType)
    {
        // AggregateType deve conter apenas letras, números, pontos e underscores
        var aggregateTypePattern = @"^[a-zA-Z0-9._]+$";
        return Regex.IsMatch(aggregateType, aggregateTypePattern);
    }
    
    private static bool IsValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}