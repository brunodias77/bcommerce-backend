using BuildingBlocks.Core.Validations;

namespace BuildingBlocks.CQRS.Validations;

/// <summary>
/// Interface para validadores de requests
/// Permite validação automática através do ValidationBehavior
/// </summary>
/// <typeparam name="TRequest">Tipo do request a ser validado</typeparam>
public interface IValidator<in TRequest>
{
    /// <summary>
    /// Valida o request e retorna um ValidationHandler com os erros encontrados
    /// </summary>
    /// <param name="request">Request a ser validado</param>
    /// <returns>ValidationHandler com os erros de validação</returns>
    ValidationHandler Validate(TRequest request);
}