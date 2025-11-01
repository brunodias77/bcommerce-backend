using System.Text.Json;
using System.Text.RegularExpressions;
using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;

namespace CatalogService.Domain.Entities;

public class ReceivedEvent : Entity
{
    public string EventType { get; private set; }
    public string SourceService { get; private set; }
    public string Payload { get; private set; } // JSON
    public bool Processed { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private ReceivedEvent() 
    {
        EventType = string.Empty;
        SourceService = string.Empty;
        Payload = string.Empty;
    }

    public static ReceivedEvent Create(string eventType, string sourceService, string payload)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType is required", nameof(eventType));

        if (string.IsNullOrWhiteSpace(sourceService))
            throw new ArgumentException("SourceService is required", nameof(sourceService));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is required", nameof(payload));

        var receivedEvent = new ReceivedEvent
        {
            EventType = eventType,
            SourceService = sourceService,
            Payload = payload,
            Processed = false,
            ProcessedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        var validationResult = receivedEvent.Validate();
        if (validationResult.HasErrors)
        {
            throw new ArgumentException($"Dados inválidos: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
        }

        return receivedEvent;
    }
    
    public override ValidationHandler Validate()
    {
        var handler = new ValidationHandler();
        
        // Validar EventType
        if (string.IsNullOrEmpty(EventType))
            handler.Add("Tipo do evento é obrigatório");
        else if (string.IsNullOrWhiteSpace(EventType))
            handler.Add("Tipo do evento não pode conter apenas espaços em branco");
        else if (EventType.Length > 200)
            handler.Add("Tipo do evento deve ter no máximo 200 caracteres");
        else if (!IsValidEventType(EventType))
            handler.Add("Tipo do evento deve conter apenas letras, números, pontos e underscores");
        
        // Validar SourceService
        if (string.IsNullOrEmpty(SourceService))
            handler.Add("Serviço de origem é obrigatório");
        else if (string.IsNullOrWhiteSpace(SourceService))
            handler.Add("Serviço de origem não pode conter apenas espaços em branco");
        else if (SourceService.Length > 100)
            handler.Add("Serviço de origem deve ter no máximo 100 caracteres");
        else if (!IsValidSourceService(SourceService))
            handler.Add("Serviço de origem deve conter apenas letras, números, pontos, hífens e underscores");
        
        // Validar Payload
        if (string.IsNullOrEmpty(Payload))
            handler.Add("Payload é obrigatório");
        else if (Payload.Length > 50000)
            handler.Add("Payload deve ter no máximo 50000 caracteres");
        else if (!IsValidJson(Payload))
            handler.Add("Payload deve ser um JSON válido");
        
        // Validar consistência entre Processed e ProcessedAt
        if (Processed && !ProcessedAt.HasValue)
            handler.Add("Data de processamento é obrigatória quando o evento está marcado como processado");
        
        if (!Processed && ProcessedAt.HasValue)
            handler.Add("Data de processamento deve ser nula quando o evento não está processado");
        
        // Validar ProcessedAt
        if (ProcessedAt.HasValue)
        {
            if (ProcessedAt.Value > DateTime.UtcNow.AddMinutes(1))
                handler.Add("Data de processamento não pode estar no futuro");
            
            if (CreatedAt != default(DateTime) && ProcessedAt.Value < CreatedAt)
                handler.Add("Data de processamento deve ser maior ou igual à data de criação");
        }
        
        // Validar CreatedAt
        if (CreatedAt == default(DateTime))
            handler.Add("Data de criação é obrigatória");
        else if (CreatedAt > DateTime.UtcNow.AddMinutes(1))
            handler.Add("Data de criação não pode estar no futuro");
        
        return handler;
    }
    
    private static bool IsValidEventType(string eventType)
    {
        // EventType deve conter apenas letras, números, pontos e underscores
        var eventTypePattern = @"^[a-zA-Z0-9._]+$";
        return Regex.IsMatch(eventType, eventTypePattern);
    }
    
    private static bool IsValidSourceService(string sourceService)
    {
        // SourceService deve conter apenas letras, números, pontos, hífens e underscores
        var sourceServicePattern = @"^[a-zA-Z0-9._-]+$";
        return Regex.IsMatch(sourceService, sourceServicePattern);
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