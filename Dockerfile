FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["global.json", "./"]
COPY ["ERP.sln", "./"]
COPY ["src/ERP.Api/ERP.Api.csproj", "src/ERP.Api/"]
COPY ["src/ERP.BuildingBlocks/ERP.BuildingBlocks.csproj", "src/ERP.BuildingBlocks/"]
COPY ["src/ERP.Modules.Catalogo/ERP.Modules.Catalogo.csproj", "src/ERP.Modules.Catalogo/"]
COPY ["src/ERP.Modules.Clientes/ERP.Modules.Clientes.csproj", "src/ERP.Modules.Clientes/"]
COPY ["src/ERP.Modules.Depositos/ERP.Modules.Depositos.csproj", "src/ERP.Modules.Depositos/"]
COPY ["src/ERP.Modules.Empresas/ERP.Modules.Empresas.csproj", "src/ERP.Modules.Empresas/"]
COPY ["src/ERP.Modules.Fornecedores/ERP.Modules.Fornecedores.csproj", "src/ERP.Modules.Fornecedores/"]
COPY ["src/ERP.Modules.Compras/ERP.Modules.Compras.csproj", "src/ERP.Modules.Compras/"]
COPY ["src/ERP.Modules.Estoque/ERP.Modules.Estoque.csproj", "src/ERP.Modules.Estoque/"]
COPY ["src/ERP.Modules.Fiscal/ERP.Modules.Fiscal.csproj", "src/ERP.Modules.Fiscal/"]
COPY ["src/ERP.Modules.Identity/ERP.Modules.Identity.csproj", "src/ERP.Modules.Identity/"]
COPY ["src/ERP.Modules.Integracoes/ERP.Modules.Integracoes.csproj", "src/ERP.Modules.Integracoes/"]
COPY ["src/ERP.Modules.Vendas/ERP.Modules.Vendas.csproj", "src/ERP.Modules.Vendas/"]

RUN dotnet restore "src/ERP.Api/ERP.Api.csproj"

COPY . .
RUN dotnet publish "src/ERP.Api/ERP.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENV Storage__Provider=JsonFile
ENV Storage__FilePath=/data/erp-store.json

COPY --from=build /app/publish .

EXPOSE 10000
ENTRYPOINT ["sh", "-c", "exec dotnet ERP.Api.dll --urls http://0.0.0.0:${PORT:-10000}"]
