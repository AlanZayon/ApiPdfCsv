# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copia os arquivos de projeto e restaura dependências
COPY *.sln .
COPY ApiPdfCsv.csproj .
RUN dotnet restore

# Copia tudo e faz build
COPY . .
RUN dotnet publish -c Release -o /out

# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .

ENTRYPOINT ["dotnet", "ApiPdfCsv.dll"]
