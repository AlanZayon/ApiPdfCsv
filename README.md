# Sistema de Processamento de PDFs da Receita Federal

## 📌 Visão Geral
Este sistema é uma API .NET para processar comprovantes de arrecadação da Receita Federal (DARF e DAS) em formato PDF, extraindo os dados financeiros e gerando relatórios estruturados em formato CSV/Excel.

site para uso:https://pdftoexcel.netlify.app/

## ✨ Funcionalidades Principais
- **Processamento de PDFs:** Extrai dados de comprovantes da Receita Federal  
- **Geração de Relatórios:** Cria arquivos estruturados em CSV/Excel  
- **API REST:** Endpoints para upload e download de arquivos  
- **Logging:** Registro detalhado de todas as operações  

## 🛠️ Tecnologias Utilizadas
- .NET 8
- iTextSharp (para manipulação de PDFs)  
- Serilog (para logging)  
- Swagger (para documentação da API)  
- xUnit (para testes automatizados)  

## 🚀 Como Executar

### Pré-requisitos
- .NET 8 SDK  
- Visual Studio 2022 ou VS Code (opcional)  

### Executando a Aplicação

Clone o repositório:
```bash
git clone https://github.com/AlanZayon/ApiPdfCsv.git
```

Navegue até o diretório do projeto:
```bash
cd ApiPdfCsv
```

Execute a aplicação:
```bash
dotnet run --project ApiPdfCsv.API
```

Acesse a interface Swagger:
```
http://localhost:5243/swagger
```

## 📋 Endpoints da API

### Upload de PDF
```
POST /api/Upload/upload
```
- Recebe um arquivo PDF para processamento  
- Retorna os dados extraídos  

### Download de Resultados
```
GET /api/Download/download
```
- Disponibiliza o arquivo CSV gerado  

### Teste
```
GET /
```
- Endpoint de verificação de saúde da API  

## 🧪 Testes

Para executar os testes automatizados:
```bash
cd ApiPdfCsv.Tests
dotnet test
```
Os testes exigem um PDF válido da Receita Federal com comprovantes de arrecadação (DAS ou DARF) dentro da pasta Resources que precisa ser criada dentro da pasta dos testes

### Tipos de Testes Implementados
- Testes de Unidade: Cobertura das classes de serviço e utilitários  
- Testes de Integração: Verificação dos controllers e fluxos principais  
- Testes Funcionais: Validação do processamento completo  

## 🏗️ Estrutura do Projeto
```
ApiPdfCsv/
├── API/                  # Camada de apresentação (Controllers)
├── Application/          # Casos de uso e regras de aplicação
├── Domain/               # Entidades e interfaces de domínio
├── Infrastructure/       # Implementações concretas (serviços, repositórios)
├── Shared/               # Utilitários e componentes compartilhados
└── Tests/                # Projeto de testes automatizados
```

## ⚙️ Configuração

As configurações podem ser ajustadas no arquivo `appsettings.json`:
```json
{
  "Logging": {
    "MinimumLevel": "Information"
  },
  "FileProcessing": {
    "MaxFileSizeMB": 100,
    "OutputDirectory": "outputs"
  }
}
```

## 📊 Formatos de PDF Suportados

O sistema processa comprovantes da Receita Federal nos formatos:

- DARF (Documento de Arrecadação de Receitas Federais)  
- DAS (Documento de Arrecadação do Simples Nacional)  

### Exemplo de estrutura reconhecida:
```
COMPOSIÇÃO DO DOCUMENTO DE ARRECADAÇÃO
Código   Descrição                          Principal   Multa   Juros   Total
1001     IRPJ - SIMPLES NACIONAL            1000,00     -       -       1000,00
...
TOTAIS                                      XXXXX,XX    X,XX    X,XX    XXXXX,XX
```

## 📄 Licença
Distribuído sob a licença MIT. Veja `LICENSE` para mais informações.


> **Nota:** Este sistema foi desenvolvido para processar documentos oficiais da Receita Federal. Certifique-se de ter autorização para processar os documentos em questão.
