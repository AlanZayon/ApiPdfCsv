# ApiPdfCsv — Processamento Profissional de PDFs e OFX

API .NET 8 para processar comprovantes da Receita Federal (DARF/DAS) em PDF e extratos bancários em OFX, extraindo dados financeiros e gerando arquivos CSV padronizados. Inclui autenticação, logging estruturado, testes automatizados e um fluxo de classificação automática/assistida para OFX com aprendizado contínuo por usuário e CNPJ.

Site do cliente web: https://pdftoexcel.netlify.app/


## Sumário
- Visão Geral
- Arquitetura e Módulos
- Funcionalidades
- Requisitos
- Configuração
- Execução (Local e Docker)
- Endpoints
- CSV Gerado (Estrutura)
- Fluxo de Processamento de OFX
- Testes
- Estrutura de Pastas
- Segurança
- Observabilidade
- Troubleshooting
- Roadmap e Versionamento
- Licença


## Visão Geral
- Objetivo: acelerar a conciliação e padronização de dados financeiros a partir de PDFs oficiais e arquivos OFX.
- Abordagem: extração determinística, classificação por regras e termos, e geração de CSV no padrão exigido por sistemas contábeis.
- Público: times financeiros, contabilidade e automação de rotinas fiscais.


## Arquitetura e Módulos
A solução segue separação por camadas dentro de módulos de domínio.

- API (Web):
  - Controllers: AuthController, UploadController, DownloadController, ConfiguracaoController
  - Middleware e Swagger configurados no Program.cs
- Authentication:
  - Serviços de autenticação (JWT), envio de e-mail, DTOs de login/registro, ApplicationUser
- PdfProcessing:
  - Use cases para processamento de PDFs e geração de estrutura ProcessedPdfData
  - IFileService para armazenar/ler arquivos e PdfProcessorService para parsing (iTextSharp)
- OfxProcessing:
  - Use case ProcessOfxUseCase, entidade ProcessedOfxData e FinalizacaoRequest
  - OfxProcessorService para parsing de OFX (SGML)
- CodeManagement:
  - Entidades e serviços para gerenciar códigos (Código Conta, Impostos, Termos Especiais) e repositórios
- CrossCutting:
  - AppDbContext (EF Core), IdentityConfig e configurações de identidade
- Shared:
  - ExcelGenerator, resultados padronizados, logging

Banco de dados: Migrations presentes em Migrations/ (EF Core). O sistema persiste usuários, termos especiais, códigos, entre outros.


## Funcionalidades
- Processamento de PDFs (DARF/DAS) com extração estruturada
- Processamento de OFX (SGML) com extração de Data, Valor, Descrição
- Classificação automática por termos especiais (por usuário e CNPJ)
- Fluxo de classificação assistida quando houver descrições não mapeadas
- Geração de CSVs padronizados: PGTO.csv (parcial) e PGTO_Finalizado.csv (final)
- Autenticação (Bearer JWT)
- Logging estruturado e detalhado
- Swagger para documentação interativa


## Requisitos
- .NET 8 SDK
- Opcional: Visual Studio 2022 ou VS Code
- Banco de dados: configurar connection string no appsettings.json (EF Core)


## Configuração
Arquivo appsettings.json (exemplo mínimo):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ApiPdfCsv;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "FileProcessing": {
    "MaxFileSizeMB": 100,
    "OutputDirectory": "outputs"
  },
  "Serilog": {
    "MinimumLevel": "Information"
  },
  "Jwt": {
    "Issuer": "your-issuer",
    "Audience": "your-audience",
    "Key": "your-secret-key"
  }
}
```
Variáveis de ambiente podem sobrescrever as configurações acima.


## Execução
### Local
1) Clonar o repositório
```bash
git clone https://github.com/AlanZayon/ApiPdfCsv.git
cd ApiPdfCsv
```
2) Restaurar e construir
```bash
dotnet restore
dotnet build -c Release
```
3) Aplicar migrações (opcional, se usar features que dependem de banco)
```bash
dotnet ef database update
```
4) Executar
```bash
dotnet run
```
Acesse Swagger: http://localhost:5243/swagger

### Docker
Exemplo de build e run:
```bash
docker build -t apipdfcsv:latest .
docker run --rm -p 5243:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="<sua-connection>" \
  -v $(pwd)/outputs:/app/outputs \
  apipdfcsv:latest
```
Swagger: http://localhost:5243/swagger


## Endpoints
Observação: endpoints autenticados via Bearer. O userId (NameIdentifier) personaliza os termos especiais armazenados.

1) Upload de Arquivo (PDF ou OFX)
- POST /api/Upload/upload
- Headers (opcional para OFX):
  - CNPJ: 00000000000000 (somente dígitos)
- Body (multipart/form-data): file=@arquivo.pdf|.ofx
- Respostas
  - PDF → 200 OK: { "type": "pdf", "result": ... }
  - OFX pendente → 200 OK: retorna transações já classificadas e lista de pendências com sugestões
  - OFX completo → 200 OK: { "type": "ofx", "status": "completed", "outputPath": "outputs/PGTO.csv" }
  - Erros comuns: 400 (tipo inválido), 500 (falha de processamento)

Exemplo cURL (OFX):
```bash
curl -X POST "http://localhost:5243/api/Upload/upload" \
  -H "Authorization: Bearer <TOKEN>" \
  -H "CNPJ: 12345678000199" \
  -F "file=@/caminho/arquivo.ofx"
```

2) Finalizar Processamento de OFX
- POST /api/Upload/finalizar-processamento
- Body (application/json): FinalizacaoRequest
- Comportamento: persiste termos especiais enviados e gera outputs/PGTO_Finalizado.csv
- Resposta de sucesso: { status: "completed", outputPath: "outputs/PGTO_Finalizado.csv" }

3) Download de Arquivo
- GET /api/Download?file=PGTO.csv ou PGTO_Finalizado.csv
- Retorna conteúdo do diretório outputs

4) Autenticação
- POST /api/Auth/login
- POST /api/Auth/register
- POST /api/Auth/forgot-password
- POST /api/Auth/reset-password


## CSV Gerado (Estrutura)
- Separador: ;
- Números: pt-BR, 2 casas decimais
- Para cada transação, 2 linhas (débito e crédito):
  - Valor positivo
    - Linha 1: Data; (CódigoBanco ou Débito); ""; Total; Descrição; "1"
    - Linha 2: Data; ""; Crédito; Total; Descrição; ""
  - Valor negativo
    - Linha 1: Data; (CódigoBanco ou Crédito); ""; |Total|; Descrição; "1"
    - Linha 2: Data; ""; Débito; |Total|; Descrição; ""
- Quando há CodigoBanco, substitui o campo de débito/crédito da segunda coluna da primeira linha do par
- Saídas padrão: outputs/PGTO.csv (parcial) e outputs/PGTO_Finalizado.csv (final)


## Fluxo de Processamento de OFX
- Leitura do OFX (SGML) com encoding ISO-8859-1
- Extração por transação (<STMTTRN>): DTPOSTED, TRNAMT, MEMO
- Busca de mapeamentos existentes (Termos Especiais) por usuário e CNPJ contendo: Código Débito, Crédito, Código Banco
- Descrições não mapeadas retornam como pendentes com possíveis códigos de banco sugeridos


## Testes
Executar todos os testes:
```bash
cd ApiPdfCsv.Tests
dotnet test
```
- Tipos de testes: unitários (serviços), integração (controllers), funcionais e E2E
- Recursos de teste: colocar amostras de PDFs/OFX em ApiPdfCsv.Tests/Resources


## Estrutura de Pastas (resumo)
```
ApiPdfCsv/
├── src/
│   ├── API/
│   │   └── Controllers/{AuthController,UploadController,DownloadController,ConfiguracaoController}.cs
│   ├── Modules/
│   │   ├── PdfProcessing/
│   │   ├── OfxProcessing/
│   │   └── CodeManagement/
│   ├── CrossCutting/{Data,Identity}
│   └── Shared/{Logging,Results,Utils}
├── Migrations/
├── outputs/
└── ApiPdfCsv.Tests/
```


## Segurança
- Autenticação JWT; proteja a chave privada (Jwt:Key)
- Validação de tamanho de arquivo (FileProcessing:MaxFileSizeMB)
- Sanitização de nomes e diretórios ao salvar no FileService
- Políticas de CORS e HTTPS recomendadas em produção
- Não registre dados sensíveis no log; use níveis adequados


## Observabilidade
- Logging estruturado (Serilog)
- Correlação por requisição via middleware padrão ASP.NET Core
- Recomenda-se exportar logs para sinks (Elastic, Seq) em produção


## Troubleshooting
- 400 no upload: verifique extensão do arquivo (.pdf ou .ofx) e cabeçalho CNPJ quando aplicável
- 500 no processamento: confira logs em console/files e permissões da pasta outputs
- CSV vazio: confirme se o arquivo de entrada possui transações válidas
- Erro de autenticação: valide emissão e validade do JWT e configuração de Issuer/Audience


## Roadmap e Versionamento
- Versionamento Semântico (SemVer) planejado: MAJOR.MINOR.PATCH
- Próximos itens:
  - Paginaç��o e histórico de processamentos
  - Exportação adicional (XLSX completo)
  - Melhoria em heurísticas de sugestão de Código Banco por CNPJ


## Licença
Distribuído sob a licença MIT. Consulte o arquivo LICENSE.

Aviso: o sistema processa documentos oficiais e dados financeiros. Utilize apenas com autorização e responsabilidade.