#!/bin/bash

# Script de build para o BCommerce Backend
# Este script compila toda a solução e verifica se não há erros

set -e  # Sai do script se algum comando falhar

echo "🚀 Iniciando build do BCommerce Backend..."

# Define o diretório base do projeto
PROJECT_ROOT="/Users/diasbruno/Documents/programacao/codigos/dotnet/bcommerce-backend"

# Cores para output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Função para imprimir mensagens coloridas
print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

# Verifica se o dotnet está instalado
if ! command -v dotnet &> /dev/null; then
    print_error "O .NET SDK não está instalado. Por favor, instale o .NET SDK 8.0 ou superior."
    exit 1
fi

# Mostra a versão do dotnet
echo "📦 Versão do .NET SDK:"
dotnet --version
echo ""

# Entra no diretório do projeto
cd "$PROJECT_ROOT" || exit 1

# Limpa a solução antes de buildar
echo "🧹 Limpando a solução..."
dotnet clean
print_success "Solução limpa com sucesso!"
echo ""

# Restaura os pacotes NuGet
echo "📥 Restaurando pacotes NuGet..."
dotnet restore
print_success "Pacotes NuGet restaurados com sucesso!"
echo ""

# Builda a solução completa
echo "🔨 Buildando a solução..."
dotnet build --no-restore --configuration Release
print_success "Solução buildada com sucesso!"
echo ""

# Build específico do CatalogService.Api
echo "🔧 Buildando o CatalogService.Api..."
dotnet build src/Catalog/CatalogService.Api/CatalogService.Api.csproj --no-restore --configuration Release
print_success "CatalogService.Api buildado com sucesso!"
echo ""

# Verifica se os arquivos de comando AddProductToFavorites foram criados
echo "🔍 Verificando implementação do AddProductToFavorites..."
FILES_TO_CHECK=(
    "src/Catalog/CatalogService.Application/Commands/FavoriteProducts/AddProductToFavorites/AddProductToFavoritesCommand.cs"
    "src/Catalog/CatalogService.Application/Commands/FavoriteProducts/AddProductToFavorites/AddProductToFavoritesResponse.cs"
    "src/Catalog/CatalogService.Application/Commands/FavoriteProducts/AddProductToFavorites/AddProductToFavoritesCommandValidator.cs"
    "src/Catalog/CatalogService.Application/Commands/FavoriteProducts/AddProductToFavorites/AddProductToFavoritesCommandHandler.cs"
)

all_files_exist=true
for file in "${FILES_TO_CHECK[@]}"; do
    if [ -f "$file" ]; then
        print_success "✓ Arquivo encontrado: $file"
    else
        print_error "✗ Arquivo não encontrado: $file"
        all_files_exist=false
    fi
done

if [ "$all_files_exist" = true ]; then
    print_success "Todos os arquivos do comando AddProductToFavorites foram encontrados!"
else
    print_warning "Alguns arquivos do comando AddProductToFavorites não foram encontrados"
fi

echo ""
print_success "🎉 Build concluído com sucesso!"
echo ""
echo "📋 Próximos passos:"
echo "1. Execute 'dotnet run --project src/Catalog/CatalogService.Api/CatalogService.Api.csproj' para iniciar a API"
echo "2. Acesse http://localhost:5000/swagger para ver a documentação da API"
echo "3. Teste o endpoint POST /api/products/{productId}/favorites/{userId}"