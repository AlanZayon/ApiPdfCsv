# ApiPdfCsv — Backend FISCAL2CSV

API .NET 8 para processamento de PDF/OFX e geração de CSV.

## Requisitos

- .NET 8 SDK
- PostgreSQL

## Setup

```bash
cp .env.example .env
dotnet restore
dotnet run
```

## Variáveis de ambiente

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings__DefaultConnection` | Connection string PostgreSQL |
| `Jwt__Key` | Chave JWT |
| `SmtpSettings__*` | Configuração SMTP para reset de senha |
| `Frontend__BaseUrl` | URL do frontend |
| `Cors__AllowedOrigins__0` | Origens CORS permitidas |

## Endpoints principais

- `POST /api/auth/login` — autenticação (cookie JWT)
- `POST /api/upload/upload` — upload PDF/OFX
- `POST /api/upload/finalizar-processamento` — finalizar classificação OFX
- `GET /api/download/download?file=PGTO.csv|EXTRATO.csv` — download
- `GET /health` — health check

## Testes

```bash
dotnet test ApiPdfCsv.Tests/ApiPdfCsv.Tests.csproj
```

## Docker

```bash
docker build -t apipdfcsv .
docker run -p 5243:8080 -e ConnectionStrings__DefaultConnection="..." apipdfcsv
```
