# ApiPdfCsv — Transforme PDFs/OFX em CSV padronizado para importar em software contábil

API em .NET 8 que lê comprovantes da Receita (DARF/DAS) em PDF e extratos bancários em OFX, extrai as informações principais e gera CSV pronto para conciliação contábil. Inclui autenticação, logging, testes automatizados e um fluxo de classificação automática/assistida com aprendizado por usuário e CNPJ.

Cliente Web (Frontend): https://pdftoexcel.netlify.app/


## Sumário
- Para quem é e o que resolve (explicação simples)
- Demonstrações (imagens e vídeos)
- Como usar sem precisar programar (passo a passo)
- Principais funcionalidades
- Entendendo o CSV gerado (explicação simples)
- Perguntas frequentes (FAQ)
- Privacidade e segurança dos dados
- Para pessoas técnicas
  - Arquitetura e módulos
  - Requisitos
  - Configuração
  - Execução (Local e Docker)
  - Endpoints
  - Estrutura do CSV (detalhada)
  - Testes e Estrutura de Pastas
  - Observabilidade
  - Roadmap e Versionamento
- Licença


## Para quem é e o que resolve (explicação simples)
- Público: áreas financeira/contábil e pessoas que precisam padronizar dados de PDFs/OFX em formato CSV.
- Problema: cada banco e documento tem um padrão distinto, dificultando a conciliação.
- Solução: você envia o PDF/OFX, a API lê os dados (data, valor, histórico, etc.), aprende como você classifica e entrega um CSV padronizado, pronto para importar.


## Demonstrações (imagens e vídeos)

Os GIFs abaixo mostram o fluxo completo de uso da aplicação para arquivos OFX, desde o upload até a finalização e reaprendizado automático.

### GIF 1 — Upload, identificação e classificação em lote por termo especial
![Upload e classificação em lote](assets/gif-01-upload-classificacao-lote.gif)  
Demonstra o envio de um arquivo OFX, o preenchimento do Código Banco e do CNPJ, a busca por termos recorrentes nos históricos e a classificação em massa de todas as ocorrências utilizando um único código contábil.

### GIF 2 — Classificação individual e depois classificação global das pendências
![Classificação individual e global](assets/gif-02-classificacao-individual-e-global.gif)  
Mostra como classificar manualmente alguns históricos específicos e, em seguida, aplicar um código único para finalizar automaticamente todas as transações restantes que ainda não possuem classificação.

### GIF 3 — Finalização do processamento e download do CSV gerado
![Finalização e download do CSV](assets/gif-03-finalizacao-e-download-csv.gif)  
Exibe o processo de finalização, o download do arquivo PGTO_Finalizado.csv e a visualização do conteúdo final, já formatado conforme o padrão esperado pela contabilidade.

### GIF 4 — Reprocessamento inteligente usando aprendizado por usuário/CNPJ
![Reprocessamento com aprendizado](assets/gif-04-reprocessamento-inteligente.gif)  
Demonstra que as classificações anteriores foram salvas: ao reenviar o mesmo arquivo OFX com o mesmo Código Banco e CNPJ, todas as transações são automaticamente classificadas com base no histórico aprendido.

### GIF 5 — Consulta e atualização das classificações salvas (sem novo upload)
![Consulta e edição das classificações](assets/gif-05-consulta-e-edicao-de-classificacoes.gif)  
Demonstra que o usuário pode visualizar todas as classificações já registradas para um determinado CNPJ e Código Banco sem precisar reenviar o arquivo OFX. A tela busca automaticamente todos os históricos previamente classificados e permite editar, atualizar ou corrigir códigos contábeis já existentes.

---


## Como usar sem precisar programar (passo a passo)
Opção A — Pelo Cliente Web
1) Acesse: https://pdftoexcel.netlify.app/
2) Crie uma conta ou faça login.
3) Envie um PDF (DARF/DAS) ou OFX.
4) No caso de OFX, revise as pendências sugeridas e confirme a classificação.
5) Baixe o CSV gerado e utilize no seu sistema.

Opção B — Pela Documentação Interativa (Swagger)
1) Suba a API localmente ou via Docker (ver seções abaixo).
2) Acesse: http://localhost:5243/swagger
3) Autentique-se (Auth → login → use o token nos outros endpoints).
4) Use o endpoint de upload para enviar seus arquivos e o de download para baixar o CSV.


## Principais funcionalidades
- Processa PDFs de DARF/DAS e extrai dados estruturados.
- Processa OFX (SGML) e extrai Data, Valor, Descrição, etc.
- Classificação automática com base em "Histórico" por usuário e por CNPJ.
- Classificação assistida quando há descrições não mapeadas.
- Gera CSV padronizado (PGTO.csv e PGTO_Finalizado.csv) com as regras esperadas pela contabilidade.
- Autenticação via JWT e logging estruturado.


## Entendendo o CSV gerado (explicação simples)
- O arquivo final é um .csv com separador ;
- Cada transação vira 2 linhas (um par) para representar débito e crédito de forma clara, quando processa arquivos DAS/DARF e mantem apenas uma linha quando processa arquivos OFX.
- Valores negativos viram positivos no CSV, mas a posição (débito/crédito) muda conforme a regra contábil.
- O sistema usa Código Banco quando disponível para padronizar a classificação.

Se preferir, use o CSV parcial (PGTO.csv) para revisão e, depois de finalizar a classificação assistida, baixe o CSV final (PGTO_Finalizado.csv).


## Perguntas frequentes (FAQ)
- O que é OFX? É um formato de extrato bancário que muitos bancos exportam.
- Preciso saber programar? Não. Você pode usar o cliente web ou o Swagger.
- Posso usar com qualquer banco? Sim, desde que você consiga o OFX. A classificação aprende com seus dados.
- E se o CSV vier vazio? Provavelmente o arquivo enviado não tem transações válidas ou foi lido com encoding incorreto.
- Como melhorar a qualidade das classificações? Revise as pendências e confirme; esse feedback fica salvo para melhorar as próximas classificações por usuário e CNPJ.


## Privacidade e segurança dos dados
- Autenticação JWT. Proteja sua chave (Jwt:Key).
- Validação de tamanho de arquivo e sanitização de nomes ao salvar.
- Não registre dados sensíveis em logs.
- Recomendado usar HTTPS e configurar CORS em produção.


## Para pessoas técnicas

### Arquitetura e módulos
A solução segue organização por módulos e camadas:
- API (Web)
  - Controllers: AuthController, UploadController, DownloadController, ConfiguracaoController
  - Swagger e middleware configurados no Program.cs
- Authentication
  - JWT, envio de e-mails e DTOs de login/registro; ApplicationUser (Identity)
- PdfProcessing
  - Use cases de processamento e entidade ProcessedPdfData
  - IFileService (armazenamento) e PdfProcessorService (parsing com iTextSharp)
- OfxProcessing
  - ProcessOfxUseCase, ProcessedOfxData, FinalizacaoRequest
  - OfxProcessorService para parsing de SGML
- CodeManagement
  - Entidades/serviços para Código Conta, Impostos, Termos Especiais e repositórios
- CrossCutting
  - AppDbContext (EF Core) e configurações de identidade
- Shared
  - ExcelGenerator, resultados padronizados, logging

Banco de dados: migrations em Migrations/ (EF Core). Persistência de usuários, termos especiais, códigos, etc.

### Requisitos
- .NET 8 SDK
- Opcional: Visual Studio 2022 ou VS Code
- Banco de dados: ConnectionStrings:DefaultConnection no appsettings.json

### Configuração (appsettings.json — exemplo mínimo)
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

### Execução
Local
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
3) Aplicar migrações (se usar recursos que dependem de banco)
```bash
dotnet ef database update
```
4) Executar
```bash
dotnet run
```
Swagger: http://localhost:5243/swagger

Docker
```bash
docker build -t apipdfcsv:latest .
docker run --rm -p 5243:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="<sua-connection>" \
  -v $(pwd)/outputs:/app/outputs \
  apipdfcsv:latest
```
Swagger: http://localhost:5243/swagger

### Endpoints (resumo)
Autenticação: Bearer JWT.

1) Upload de Arquivo (PDF/OFX)
- POST /api/Upload/upload
- Headers (OFX): CNPJ: 00000000000000 (somente dígitos)
- Body (multipart/form-data): file=@arquivo.pdf|.ofx
- Respostas
  - PDF → 200 OK: { "type": "pdf", "result": ... }
  - OFX pendente → 200 OK: transações classificadas + pendências com sugestões
  - OFX completo → 200 OK: { "type": "ofx", "status": "completed", "outputPath": "outputs/PGTO.csv" }

Exemplo cURL (OFX)
```bash
curl -X POST "http://localhost:5243/api/Upload/upload" \
  -H "Authorization: Bearer <TOKEN>" \
  -H "CNPJ: 12345678000199" \
  -F "file=@/caminho/arquivo.ofx"
```

2) Finalizar Processamento de OFX
- POST /api/Upload/finalizar-processamento
- Body (application/json): FinalizacaoRequest
- Gera: outputs/PGTO_Finalizado.csv

3) Download de Arquivo
- GET /api/Download?file=PGTO.csv|PGTO_Finalizado.csv

4) Autenticação
- POST /api/Auth/login | /api/Auth/register | /api/Auth/forgot-password | /api/Auth/reset-password

### Estrutura do CSV (detalhada)
- Separador: ;
- Locale numérico: pt-BR, 2 casas decimais
- Cada transação = 2 linhas (par) com valor positivo no CSV
  - Valor positivo
    - L1: Data; (CódigoBanco ou Débito); ""; Total; Descrição; "1"
    - L2: Data; ""; Crédito; Total; Descrição; ""
  - Valor negativo
    - L1: Data; (CódigoBanco ou Crédito); ""; |Total|; Descrição; "1"
    - L2: Data; ""; Débito; |Total|; Descrição; ""
- Quando há CodigoBanco, substitui o campo de débito/crédito da segunda coluna da primeira linha do par
- Saídas: outputs/PGTO.csv (parcial) e outputs/PGTO_Finalizado.csv (final)

### Testes e Estrutura de Pastas
Rodar testes
```bash
cd ApiPdfCsv.Tests
dotnet test
```
Tipos de teste: unit, integração (controllers), funcionais e E2E. Recursos de teste em ApiPdfCsv.Tests/Resources.

Estrutura (resumo)
```
ApiPdfCsv/
├── src/
│   ├── API/
│   │   └── Controllers/{AuthController,UploadController,DownloadController,ConfiguracaoController}.cs
│   ├── Modules/{PdfProcessing,OfxProcessing,CodeManagement}
│   ├── CrossCutting/{Data,Identity}
│   └── Shared/{Logging,Results,Utils}
├── Migrations/
├── outputs/
└── ApiPdfCsv.Tests/
```

### Observabilidade
- Logging estruturado (Serilog)
- Correlação por requisição (ASP.NET Core)
- Recomendado exportar para sinks (Elastic, Seq) em produção

### Roadmap e Versionamento
- Versão: SemVer (MAJOR.MINOR.PATCH)
- Próximos itens
  - Histórico de processamentos e paginação
  - Exportação adicional (XLSX completo)
  - Heurísticas de sugestão de Código Banco por CNPJ aprimoradas

## Minhas responsabilidades neste projeto

- Arquitetura e implementação do backend em .NET 8
- Parsing de PDFs e OFX
- Classificação automática e assistida com aprendizado por usuário
- Sistema de autenticação JWT e logging
- Geração de CSV padronizado para conciliação contábil
- Testes unitários, integração e E2E
- Deploy via Docker e documentação completa

## Skills Demonstradas

- .NET 8, C#
- EF Core, Identity, JWT
- Parsing de PDFs/OFX
- CSV padronizado e regras contábeis
- Logging estruturado (Serilog)
- Testes unitários, integração e E2E
- Docker e configuração local
- Arquitetura modular e escalável

⚡ Projeto completo de backend que processa PDFs/OFX e gera CSVs padronizados, com
classificação automática/assistida, autenticação, logging, testes e fluxo pronto para produção.

## Licença
Licença MIT (ver arquivo LICENSE).

Aviso: o sistema processa documentos oficiais e dados financeiros. Use somente com autorização e responsabilidade.
