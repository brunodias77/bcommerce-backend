namespace CatalogService.Domain.Enums;

/// <summary>
/// Operações disponíveis para atualização de estoque
/// </summary>
public enum StockOperation
{
    /// <summary>
    /// Adicionar quantidade ao estoque atual
    /// </summary>
    ADD = 1,
    
    /// <summary>
    /// Subtrair quantidade do estoque atual
    /// </summary>
    SUBTRACT = 2,
    
    /// <summary>
    /// Definir quantidade absoluta do estoque
    /// </summary>
    SET = 3
}