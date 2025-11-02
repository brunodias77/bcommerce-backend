# Documento de Requisitos de Produto - DeleteProduct

## 1. Visão Geral do Produto

O **comando DeleteProduct** é uma funcionalidade crítica do Catalog Service que permite a remoção segura de produtos do catálogo de e-commerce. Utiliza soft delete para preservar dados históricos e integridade referencial, garantindo que informações de pedidos anteriores permaneçam consistentes.

- **Problema a resolver**: Necessidade de remover produtos do catálogo sem perder histórico de vendas e referências em pedidos existentes.
- **Usuários**: Administradores e gerentes de produto com permissões adequadas.
- **Valor do produto**: Manutenção da integridade de dados e conformidade com regulamentações de auditoria.

## 2. Funcionalidades Principais

### 2.1 Papéis de Usuário

| Papel | Método de Registro | Permissões Principais |
|-------|-------------------|----------------------|
| Administrador | Convite interno | Pode deletar qualquer produto, visualizar produtos deletados, restaurar produtos |
| Gerente de Produto | Aprovação por administrador | Pode deletar produtos de suas categorias, visualizar histórico |
| Usuário Comum | Registro público | Apenas visualização de produtos ativos (sem acesso a funcionalidades de exclusão) |

### 2.2 Módulo de Funcionalidades

Nossos requisitos para o comando DeleteProduct consistem nas seguintes funcionalidades principais:

1. **Endpoint de Exclusão**: Interface REST para receber solicitações de exclusão de produtos.
2. **Validação de Negócio**: Verificação de regras antes da exclusão (produto existe, não tem pedidos pendentes).
3. **Soft Delete**: Marcação do produto como deletado sem remoção física dos dados.
4. **Auditoria**: Registro de quem, quando e por que o produto foi removido.
5. **Notificação**: Comunicação com outros serviços sobre a exclusão do produto.

### 2.3 Detalhes das Páginas

| Nome da Página | Nome do Módulo | Descrição da Funcionalidade |
|----------------|----------------|----------------------------|
| API Endpoint | Exclusão de Produto | Recebe requisições HTTP DELETE, valida parâmetros, executa comando via mediator, retorna resposta padronizada |
| Validação | Regras de Negócio | Verifica se produto existe, não está já deletado, não possui pedidos pendentes, usuário tem permissão |
| Processamento | Soft Delete | Atualiza DeletedAt, UpdatedAt, Version, mantém dados históricos, preserva integridade referencial |
| Auditoria | Log de Operações | Registra timestamp, usuário, motivo, IP, dados antes/depois da operação |
| Notificação | Eventos de Domínio | Publica evento ProductDeleted para outros serviços, invalida cache, atualiza índices de busca |

## 3. Processo Principal

### 3.1 Fluxo do Administrador

O administrador acessa o sistema de gerenciamento, navega até a lista de produtos, seleciona um produto específico e confirma a exclusão. O sistema valida as permissões, verifica regras de negócio, executa o soft delete e confirma a operação.

### 3.2 Fluxo do Sistema

```mermaid
graph TD
    A[Requisição DELETE /api/products/{id}] --> B[Validar Autenticação]
    B --> C[Validar Autorização]
    C --> D[Validar Parâmetros]
    D --> E[Buscar Produto]
    E --> F{Produto Existe?}
    F -->|Não| G[Retornar 404]
    F -->|Sim| H{Já Deletado?}
    H -->|Sim| I[Retornar 409]
    H -->|Não| J{Tem Pedidos Pendentes?}
    J -->|Sim| K[Retornar 409]
    J -->|Não| L[Executar Soft Delete]
    L --> M[Atualizar Timestamps]
    M --> N[Incrementar Versão]
    N --> O[Salvar no Banco]
    O --> P[Publicar Evento]
    P --> Q[Invalidar Cache]
    Q --> R[Retornar 200 OK]
```

## 4. Design da Interface do Usuário

### 4.1 Estilo de Design

**Não aplicável** - Este é um comando de API REST sem interface gráfica. No entanto, para futuras interfaces administrativas:

- **Cores primárias**: #DC2626 (vermelho para ações destrutivas), #F59E0B (amarelo para avisos)
- **Estilo de botão**: Botões com bordas arredondadas, ícone de lixeira, confirmação modal
- **Fonte**: Inter ou system fonts, tamanhos 14px para texto, 16px para botões
- **Layout**: Cards para produtos, modal de confirmação centralizado
- **Ícones**: Feather Icons ou Heroicons para consistência

### 4.2 Visão Geral do Design da Página

| Nome da Página | Nome do Módulo | Elementos da UI |
|----------------|----------------|-----------------|
| Lista de Produtos | Gerenciamento | Tabela com produtos, botão "Excluir" vermelho, filtros por status (ativo/deletado) |
| Modal de Confirmação | Exclusão Segura | Título "Confirmar Exclusão", texto explicativo, botões "Cancelar" (cinza) e "Excluir" (vermelho) |
| Notificação | Feedback | Toast notification verde para sucesso, vermelho para erro, com ícones apropriados |
| Log de Auditoria | Histórico | Timeline com ações, timestamps, usuários, filtros por data e tipo de operação |

### 4.3 Responsividade

**Desktop-first** com adaptação para tablets e mobile. Interface administrativa otimizada para desktop, com modais responsivos e confirmações touch-friendly em dispositivos móveis.

## 5. Regras de Negócio Detalhadas

### 5.1 Validações Obrigatórias

1. **Autenticação**: Usuário deve estar logado com token JWT válido
2. **Autorização**: Usuário deve ter papel "Admin" ou "Manager"
3. **Produto Existente**: ID deve corresponder a um produto válido no banco
4. **Não Deletado**: Produto não pode ter DeletedAt preenchido
5. **Sem Pedidos Pendentes**: Produto não pode ter pedidos com status "Pending" ou "Processing"
6. **Categoria Ativa**: Se produto tem categoria, ela deve estar ativa (regra opcional)

### 5.2 Comportamentos Especiais

- **Produtos em Promoção**: Avisar se produto está em promoção ativa antes da exclusão
- **Produtos Favoritos**: Remover das listas de favoritos dos usuários
- **Imagens Associadas**: Manter imagens para histórico, mas marcar como "orphaned"
- **Reviews**: Manter reviews para auditoria, mas ocultar da visualização pública
- **Cache**: Invalidar todos os caches relacionados ao produto
- **Busca**: Remover dos índices de busca (Elasticsearch/Solr)

### 5.3 Logs e Auditoria

```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "event": "ProductDeleted",
  "userId": "admin-123",
  "productId": "prod-456",
  "productName": "Smartphone XYZ",
  "reason": "Produto descontinuado",
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "beforeState": {
    "isActive": true,
    "stock": 10,
    "version": 5
  },
  "afterState": {
    "isActive": false,
    "deletedAt": "2024-01-15T10:30:00Z",
    "version": 6
  }
}
```

## 6. Critérios de Aceitação

### 6.1 Funcionalidades Obrigatórias

- ✅ Endpoint DELETE /api/products/{id} funcional
- ✅ Validação de autenticação e autorização
- ✅ Soft delete com preservação de dados
- ✅ Validação de regras de negócio
- ✅ Logs estruturados e auditoria
- ✅ Tratamento de erros padronizado
- ✅ Resposta API consistente
- ✅ Testes unitários e de integração

### 6.2 Critérios de Performance

- ⏱️ Tempo de resposta < 500ms para 95% das requisições
- 📊 Suporte a 100 exclusões simultâneas
- 🔄 Rollback automático em caso de falha
- 📈 Métricas de monitoramento ativas

### 6.3 Critérios de Segurança

- 🔐 Autenticação JWT obrigatória
- 🛡️ Autorização baseada em papéis
- 📝 Log de todas as operações
- 🚫 Rate limiting para prevenir abuso
- 🔍 Validação de entrada rigorosa

## 7. Cenários de Teste

### 7.1 Testes Funcionais

| Cenário | Entrada | Resultado Esperado |
|---------|---------|-------------------|
| Exclusão Válida | ID de produto ativo | 200 OK, produto marcado como deletado |
| Produto Inexistente | ID inválido | 404 Not Found |
| Produto Já Deletado | ID de produto deletado | 409 Conflict |
| Sem Autorização | Token inválido | 401 Unauthorized |
| Sem Permissão | Usuário comum | 403 Forbidden |
| ID Malformado | String inválida | 400 Bad Request |

### 7.2 Testes de Integração

- **Banco de Dados**: Verificar persistência do soft delete
- **Cache**: Confirmar invalidação de cache
- **Eventos**: Validar publicação de eventos de domínio
- **Logs**: Verificar geração de logs de auditoria
- **Performance**: Medir tempo de resposta sob carga

### 7.3 Testes de Regressão

- **Produtos Relacionados**: Verificar se pedidos antigos ainda funcionam
- **Busca**: Confirmar que produto não aparece em buscas
- **Relatórios**: Validar que relatórios históricos permanecem corretos
- **Backup/Restore**: Testar recuperação de dados

## 8. Considerações de Implementação

### 8.1 Fases de Desenvolvimento

**Fase 1 - MVP (2 semanas)**:
- Implementação básica do comando
- Validações essenciais
- Soft delete simples
- Testes unitários

**Fase 2 - Melhorias (1 semana)**:
- Logs de auditoria
- Eventos de domínio
- Invalidação de cache
- Testes de integração

**Fase 3 - Produção (1 semana)**:
- Monitoramento
- Métricas
- Documentação
- Deploy e validação

### 8.2 Dependências Técnicas

- **BuildingBlocks.CQRS**: Para padrão Command/Handler
- **BuildingBlocks.Core**: Para validações e exceções
- **Entity Framework Core**: Para persistência
- **MediatR**: Para mediação de comandos
- **Serilog**: Para logging estruturado
- **JWT Bearer**: Para autenticação

### 8.3 Riscos e Mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|-------|---------------|---------|-----------|
| Perda de dados | Baixa | Alto | Soft delete + backups regulares |
| Performance degradada | Média | Médio | Índices otimizados + cache |
| Falha de autorização | Baixa | Alto | Testes de segurança + code review |
| Inconsistência de dados | Média | Alto | Transações + validações rigorosas |