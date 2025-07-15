# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copia os arquivos de projeto e restaura dependências
COPY *.sln .
COPY ApiPdfCsv.csproj .
RUN dotnet restore

# Copia tudo e faz build
COPY . .
WORKDIR /app/ApiPdfCsv
RUN dotnet publish -c Release -o /out

# Etapa 2: imagem de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

# Expõe a porta padrão do ASP.NET
EXPOSE 80
ENTRYPOINT ["dotnet", "ApiPdfCsv.dll"]