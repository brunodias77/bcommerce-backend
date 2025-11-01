using System.Text.RegularExpressions;
using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;

namespace CatalogService.Domain.Entities;

public class InboxEvent : Entity
{
    
    public string EventType { get; private set; }
    public Guid AggregateId { get; private set; }
    public DateTime ProcessedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private InboxEvent() 
    {
        EventType = string.Empty;
    }

    public static InboxEvent Create(string eventType, Guid aggregateId)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType is required", nameof(eventType));

        if (aggregateId == Guid.Empty)
            throw new ArgumentException("AggregateId cannot be empty", nameof(aggregateId));

        var inboxEvent = new InboxEvent
        {
            EventType = eventType,
            AggregateId = aggregateId,
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var validationHandler = new ValidationHandler();
        inboxEvent.Validate(validationHandler);
        validationHandler.ThrowIfHasErrors();

        return inboxEvent;
    }
    
    public override ValidationHandler Validate(ValidationHandler handler)
    {
        
        // Validar EventType
        if (string.IsNullOrEmpty(EventType))
            handler.Add("Tipo do evento é obrigatório");
        else if (string.IsNullOrWhiteSpace(EventType))
            handler.Add("Tipo do evento não pode conter apenas espaços em branco");
        else if (EventType.Length > 200)
            handler.Add("Tipo do evento deve ter no máximo 200 caracteres");
        else if (!IsValidEventType(EventType))
            handler.Add("Tipo do evento deve conter apenas letras, números, pontos e underscores");
        
        // Validar AggregateId
        if (AggregateId == Guid.Empty)
            handler.Add("ID do agregado é obrigatório");
        
        // Validar ProcessedAt
        if (ProcessedAt == default(DateTime))
            handler.Add("Data de processamento é obrigatória");
        else if (ProcessedAt > DateTime.UtcNow.AddMinutes(1))
            handler.Add("Data de processamento não pode estar no futuro");
        
        // Validar CreatedAt
        if (CreatedAt == default(DateTime))
            handler.Add("Data de criação é obrigatória");
        else if (CreatedAt > DateTime.UtcNow.AddMinutes(1))
            handler.Add("Data de criação não pode estar no futuro");
        
        // Validar relação entre CreatedAt e ProcessedAt
        if (CreatedAt != default(DateTime) && ProcessedAt != default(DateTime) && CreatedAt > ProcessedAt)
            handler.Add("Data de criação não pode ser posterior à data de processamento");
        
        return handler;
    }
    
    private static bool IsValidEventType(string eventType)
    {
        // EventType deve conter apenas letras, números, pontos e underscores
        var eventTypePattern = @"^[a-zA-Z0-9._]+$";
        return Regex.IsMatch(eventType, eventTypePattern);
    }
}