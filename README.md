# Sistema de Processamento de PDFs e OFX

## 📌 Visão Geral
API .NET para processar comprovantes da Receita Federal (DARF/DAS) em PDF e extratos bancários em OFX, extraindo dados financeiros e gerando relatórios estruturados em CSV. Para OFX, a API implementa um fluxo de classificação automática e assistida, com persistência de termos por usuário e CNPJ para aprendizado contínuo.

Site para uso: https://pdftoexcel.netlify.app/

## ✨ Funcionalidades Principais
- Processamento de PDFs (DARF/DAS): extração de dados dos comprovantes
- Processamento de OFX: leitura direta do arquivo OFX (SGML), extraindo Data, Valor e Descrição (MEMO)
- Classificação Automática de OFX: usa termos cadastrados por usuário/CNPJ (débito, crédito e código do banco)
- Fluxo de Classificação Assistida: quando houver descrições não mapeadas, a API retorna pendências para o cliente finalizar
- Geração de CSV: cria PGTO.csv (parcial) e PGTO_Finalizado.csv (final) com separador “;�� e formatação pt-BR
- API REST com autenticação
- Logging detalhado de operações

## 🛠️ Tecnologias Utilizadas
- .NET 8
- iTextSharp (PDF)
- ClosedXML (planilha/CSV)
- Serilog (logging)
- Swagger (documentação)
- xUnit (testes)

## 🚀 Como Executar

Pré-requisitos
- .NET 8 SDK
- Visual Studio 2022 ou VS Code (opcional)

Clonar o repositório:
```bash
git clone https://github.com/AlanZayon/ApiPdfCsv.git
cd ApiPdfCsv
```

Executar a aplicação (exemplo):
```bash
dotnet run
```

Acessar Swagger:
```
http://localhost:5243/swagger
```

## 📋 Endpoints da API

Observação: Todos os endpoints deste controller exigem autenticação (Bearer). O usuário autenticado é utilizado para personalização de termos (NameIdentifier → userId).

### 1) Upload de Arquivo (PDF ou OFX)
```
POST /api/Upload/upload
Content-Type: multipart/form-data
Headers opcionais para OFX:
  - CNPJ: 00000000000000 (somente dígitos)
```
Campos:
- file: arquivo .pdf ou .ofx

Respostas:
- PDF
  - 200 OK → `{ type: "pdf", result: ... }`
- OFX (classificação pendente)
  - 200 OK →
    ```json
    {
      "type": "ofx",
      "status": "pending_classification",
      "transacoesClassificadas": [
        { "DataDeArrecadacao": "dd/MM/yyyy", "Debito": 0, "Credito": 0, "Total": 0.0, "Descricao": "...", "Divisao": 1, "CodigoBanco": 0 }
      ],
      "pendingTransactions": [
        { "descricao": "...", "data": "dd/MM/yyyy", "valor": 0.0, "codigosBanco": [111, 222] }
      ],
      "filePath": "<caminho temporário interno>"
    }
    ```
- OFX (sem pendências)
  - 200 OK →
    ```json
    {
      "type": "ofx",
      "status": "completed",
      "outputPath": "outputs/PGTO.csv"
    }
    ```
- Erros comuns
  - 400 BadRequest → tipo de arquivo não suportado
  - 500 InternalServerError → falha no processamento

Dica: Para OFX, envie o header `CNPJ` para que a API sugira códigos de banco nas transações pendentes.

Exemplo cURL (OFX):
```bash
curl -X POST "http://localhost:5243/api/Upload/upload" \
  -H "Authorization: Bearer <TOKEN>" \
  -H "CNPJ: 12345678000199" \
  -F "file=@/caminho/arquivo.ofx"
```

### 2) Finalizar Processamento de OFX
```
POST /api/Upload/finalizar-processamento
Content-Type: application/json
```
Body (FinalizacaoRequest):
```json
{
  "transacoesClassificadas": [
    { "DataDeArrecadacao": "01/01/2025", "Debito": 111, "Credito": 222, "Total": 100.00, "Descricao": "SERVICO X", "Divisao": 1, "CodigoBanco": 123 }
  ],
  "classificacoes": [
    { "descricao": "TARIFA BANCARIA", "codigoDebito": 511, "codigoCredito": 312, "codigoBanco": 341 }
  ],
  "transacoesPendentes": [
    { "descricao": "TARIFA BANCARIA", "data": "02/01/2025", "valor": 25.90, "codigosBanco": [341] }
  ],
  "CNPJ": "12345678000199"
}
```
Resposta:
```json
{
  "status": "completed",
  "outputPath": {
    "message": "Processamento finalizado com sucesso",
    "outputPath": "outputs/PGTO_Finalizado.csv"
  }
}
```
Comportamento:
- As classificações enviadas são persistidas como “termos especiais” para o usuário/CNPJ, automatizando próximas execuções.
- Gera `outputs/PGTO_Finalizado.csv` com todas as transações (as já classificadas e as que foram finalizadas agora).

## 🧠 Como funciona o processamento de OFX
- Leitura direta do conteúdo OFX com encoding ISO-8859-1 (modo SGML)
- Campos extraídos por transação (`<STMTTRN>`):
  - `<DTPOSTED>` → Data no formato dd/MM/yyyy
  - `<TRNAMT>` → Valor decimal (InvariantCulture → convertido para pt-BR na saída CSV)
  - `<MEMO>` → Descrição
- O fluxo busca mapeamentos existentes por usuário e CNPJ (Termos Especiais) contendo:
  - Código de Débito, Código de Crédito e Código do Banco
- Para descrições não mapeadas, são retornadas como pendentes com possíveis `codigosBanco` sugeridos para o CNPJ informado.

## 📄 CSV gerado (PGTO.csv e PGTO_Finalizado.csv)
- Separador: `;`
- Ponto flutuante: pt-BR com 2 casas decimais
- Para cada transação, são geradas 2 linhas (lançamento a débito e a crédito):
  - Valores positivos:
    - Linha 1: Data; (CódigoBanco ou Débito); ""; Total; Descrição; "1"
    - Linha 2: Data; ""; Crédito; Total; Descrição; ""
  - Valores negativos:
    - Linha 1: Data; (CódigoBanco ou Crédito); ""; |Total|; Descrição; "1"
    - Linha 2: Data; ""; Débito; |Total|; Descrição; ""
- Quando `CodigoBanco` estiver presente, ele substitui o campo de Débito/Crédito na segunda coluna da primeira linha do par correspondente.
- Arquivos padrão:
  - Execução parcial (sem pendências): `outputs/PGTO.csv`
  - Execução finalizada: `outputs/PGTO_Finalizado.csv`

## 🏗️ Estrutura do Projeto (resumo)
```
ApiPdfCsv/
├── src/
│   ├── API/Controllers/UploadController.cs
│   ├── Modules/
│   │   ├── PdfProcessing/
│   │   ├── OfxProcessing/
│   │   │   ├── Application/UseCases/ProcessOfxUseCase.cs
│   │   │   ├── Domain/Entities/{ProcessedOfxData, FinalizacaoRequest}.cs
│   │   │   ├── Domain/Interfaces/IOfxProcessorService.cs
│   │   │   └── Infrastructure/Services/OfxProcessorService.cs
│   └── Shared/Utils/ExcelGenerator.cs
└── outputs/
```

## ⚙️ Configuração
`appsettings.json` (exemplo):
```json
{
  "Logging": { "MinimumLevel": "Information" },
  "FileProcessing": { "MaxFileSizeMB": 100, "OutputDirectory": "outputs" }
}
```

## 🧪 Testes
Executar testes:
```bash
cd ApiPdfCsv.Tests
dotnet test
```
Os testes exigem um PDF válido da Receita Federal com comprovantes de arrecadação (DAS ou DARF) dentro da pasta Resources (criar dentro do projeto de testes).

## 📊 Formatos Suportados
- PDF: DARF e DAS
- OFX: extratos contendo `<STMTTRN>` com `<DTPOSTED>`, `<TRNAMT>`, `<MEMO>`

## 📄 Licença
Distribuído sob a licença MIT. Veja `LICENSE` para mais informações.

Nota: Este sistema processa documentos oficiais e dados financeiros. Utilize somente com autorização e responsabilidade.