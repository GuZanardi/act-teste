# 💼 Desafio Técnico act digital - Gestão de Conta Empresarial

Solução desenvolvida para o controle de movimentações financeiras de uma conta empresarial por meio de entradas e saídas de valores, consulta de saldo em tempo real e extrato com histórico detalhado das movimentações, **garantindo a consistência dos registros e impedindo saldo negativo**.

---

## 📌 Sumário
1. [Visão Geral e Arquitetura](#-visão-geral-e-arquitetura)
2. [Tecnologias Utilizadas](#-tecnologias-utilizadas)
3. [Estrutura do Repositório](#-estrutura-do-repositório)
4. [Requisitos de Negócio Implementados](#-requisitos-de-negócio-implementados)
5. [Como Executar a Aplicação](#-como-executar-a-aplicação)
   - [Opção 1: Executando com Docker Compose (Recomendado)](#opção-1-executando-com-docker-compose-recomendado)
   - [Opção 2: Executando Localmente](#opção-2-executando-localmente)
6. [Documentação dos Endpoints (API)](#-documentação-dos-endpoints-api)
7. [Testes Automatizados](#-testes-automatizados)
8. [Decisões Técnicas e Boas Práticas](#-decisões-técnicas-e-boas-práticas)
9. [Possíveis Evoluções Futuras](#-possíveis-evoluções-futuras)

---

## 🏗 Visão Geral e Arquitetura

A solução foi projetada de forma simples, clara e desacoplada, priorizando a manutenibilidade, legibilidade e confiabilidade das regras de negócio sem adicionar complexidade desnecessária.

```
┌─────────────────────────────────────────────────────────────┐
│             Frontend (React + TypeScript + Tailwind)        │
│          Dashboard, Saldo, Extrato, Nova Transação          │
└──────────────────────────────┬──────────────────────────────┘
                               │ HTTP / JSON
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                      API (.NET 9 / C#)                      │
│                                                             │
│   [Controllers] ──► [Services / Business] ──► [EF Core]     │
│   (Account, Tx)    (Validação & Atomicidade)  (AppDbContext)│
│                                                             │
│   [Global Exception Middleware] (Tratamento de Erros)       │
└──────────────────────────────┬──────────────────────────────┘
                               │ MySQL Connector
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                      Banco de Dados MySQL                   │
│        Tabelas: Accounts, Transactions (Índices & FKs)      │
└──────────────────────────────┘
```

---

## 🚀 Tecnologias Utilizadas

### Backend (API)
- **.NET 9** (C# 13)
- **ASP.NET Core Web API**
- **Entity Framework Core 9** + **Pomelo MySQL**
- **Swagger / OpenAPI** (documentação interativa com UI)
- **xUnit**, **FluentAssertions** e **Moq** (para suíte de testes unitários)

### Frontend (SPA)
- **React 18** com **TypeScript**
- **Tailwind CSS** (estilização moderna e utilitária)
- **Lucide React** (ícones limpos)
- **Axios** (cliente HTTP tipado)

### Infraestrutura
- **Docker & Docker Compose**
- **MySQL 8.0**
- **Nginx Alpine** (servindo o build do frontend)

---

## 📁 Estrutura do Repositório

```text
├── api/
│   ├── FinancialAccount.API/            # Projeto da API Principal
│   │   ├── Controllers/                 # Controllers da API (AccountController, TransactionsController)
│   │   ├── Data/                        # AppDbContext e DbInitializer (Seed e Migração automática)
│   │   ├── DTOs/                        # Request e Response DTOs
│   │   ├── Exceptions/                  # Exceções de Domínio (InsufficientBalanceException, etc.)
│   │   ├── Middlewares/                 # Middleware Global de Tratamento de Exceções
│   │   ├── Models/                      # Entidades de Domínio (Account, Transaction, TransactionType)
│   │   ├── Services/                    # Regras de Negócio e Serviços (IAccountService, AccountService)
│   │   ├── Program.cs                   # Configurações de DI, CORS, EF Core e Swagger
│   │   ├── Dockerfile                   # Build e execução containerizada da API
│   │   └── FinancialAccount.API.csproj
│   ├── FinancialAccount.Tests/          # Projeto de Testes Automatizados
│   │   ├── AccountServiceTests.cs       # Testes unitários das regras de negócio
│   │   ├── ControllersTests.cs          # Testes dos endpoints e controllers
│   │   └── FinancialAccount.Tests.csproj
│   └── FinancialAccount.sln             # Solução do Visual Studio
│
├── app/                                 # Frontend SPA em React + TypeScript + Tailwind
│   ├── public/
│   │   └── index.html                   # HTML base da aplicação
│   ├── src/
│   │   ├── components/                  # Componentes reutilizáveis (BalanceSummary, TransactionTable, etc.)
│   │   ├── services/api.ts              # Cliente Axios tipado
│   │   ├── types/index.ts               # Interfaces e tipos TypeScript
│   │   ├── App.tsx                      # Componente principal
│   │   ├── index.tsx                    # Entrypoint React
│   │   └── index.css                    # Diretivas do Tailwind CSS
│   ├── Dockerfile                       # Build multistage com Nginx
│   ├── tailwind.config.js               # Configuração do Tailwind
│   ├── tsconfig.json                    # Configuração TypeScript
│   └── package.json
│
├── docker-compose.yml                   # Orquestração de todos os serviços (MySQL, API, App)
└── README.md                            # Guia completo da solução
```

---

## 💼 Requisitos de Negócio Implementados

1. **Registrar entradas de valores (Crédito):**
   - Validação de valor positivo (> R$ 0,00).
   - Atualização atômica do saldo da conta e gravação da movimentação com snapshot do saldo resultante.
2. **Registrar saídas de valores (Débito):**
   - Validação de valor positivo (> R$ 0,00).
   - **Garantia de consistência sem saldo negativo:** caso a conta não possua saldo suficiente para a retirada, a transação é cancelada e rejeitada imediatamente retornando HTTP `422 Unprocessable Entity` com mensagem clara para o usuário.
3. **Consultar o saldo disponível:**
   - Retorna o saldo atualizado, total consolidado de entradas, total consolidado de saídas e total de operações realizadas.
4. **Consultar o histórico de movimentações:**
   - Extrato completo com paginação configurável (`page`, `pageSize`), filtro por período (`startDate`, `endDate`) e filtro por tipo de movimentação (`Credit` ou `Debit`).

---

## 🛠 Como Executar a Aplicação

### Opção 1: Executando com Docker Compose (Recomendado)

Com o Docker e Docker Compose instalados, execute na raiz do projeto:

```bash
docker compose up --build
```

Os seguintes serviços estarão disponíveis:
- 🌐 **Frontend (React + TS + Tailwind):** [http://localhost:3000](http://localhost:3000)
- 📖 **Swagger da API:** [http://localhost:5000](http://localhost:5000)
- 🗄 **Banco de Dados MySQL:** `localhost:3306` (Database: `financial_db`, User: `act_user`, Pass: `act_password`)

Para encerrar os containers:
```bash
docker compose down
```

---

### Opção 2: Executando Localmente

#### 1. Iniciar o Banco de Dados MySQL
Inicie uma instância do MySQL com as credenciais padrão:
```bash
docker run --name act_mysql -e MYSQL_DATABASE=financial_db -e MYSQL_USER=act_user -e MYSQL_PASSWORD=act_password -e MYSQL_ROOT_PASSWORD=root_password -p 3306:3306 -d mysql:8.0
```

#### 2. Iniciar a API .NET 9
```bash
cd api/FinancialAccount.API
dotnet run
```
A API iniciará em `http://localhost:5000`.

#### 3. Iniciar o Frontend React (TypeScript + Tailwind)
```bash
cd ../../app
npm install
npm start
```
O Frontend estará acessível em `http://localhost:3000`.

---

## 📖 Documentação dos Endpoints (API)

A documentação interativa Swagger está disponível na raiz da API (`http://localhost:5000`).

### 1. Consultar Saldo
- **GET** `/api/account/balance`
- **Response 200 OK:**
```json
{
  "accountId": "11111111-1111-1111-1111-111111111111",
  "accountName": "Conta Empresarial Principal",
  "balance": 1250.00,
  "totalCredits": 1500.00,
  "totalDebits": 250.00,
  "totalTransactions": 3,
  "lastUpdated": "2026-09-28T22:00:00Z"
}
```

### 2. Registrar Entrada (Crédito)
- **POST** `/api/transactions/deposit`
- **Request Body:**
```json
{
  "amount": 500.00,
  "description": "Recebimento de Cliente - Nota 1042"
}
```
- **Response 201 Created:**
```json
{
  "id": "e4f8c12a-4a2e-4b6a-93e1-325db172f8a1",
  "accountId": "11111111-1111-1111-1111-111111111111",
  "type": "Entrada",
  "typeValue": 1,
  "amount": 500.00,
  "description": "Recebimento de Cliente - Nota 1042",
  "balanceAfter": 1750.00,
  "createdAt": "2026-09-28T22:05:00Z"
}
```

### 3. Registrar Saída (Débito)
- **POST** `/api/transactions/withdraw`
- **Request Body:**
```json
{
  "amount": 200.00,
  "description": "Pagamento Fornecedor"
}
```
- **Response 201 Created:** (se houver saldo suficiente)
```json
{
  "id": "a1b2c3d4-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
  "accountId": "11111111-1111-1111-1111-111111111111",
  "type": "Saída",
  "typeValue": 2,
  "amount": 200.00,
  "description": "Pagamento Fornecedor",
  "balanceAfter": 1550.00,
  "createdAt": "2026-09-28T22:10:00Z"
}
```
- **Response 422 Unprocessable Entity:** (se o saldo for insuficiente)
```json
{
  "message": "Saldo insuficiente para realizar a transação de R$ 5.000,00. Saldo atual disponível: R$ 1.550,00.",
  "details": null,
  "timestamp": "2026-09-28T22:10:00Z"
}
```

### 4. Consultar Histórico de Movimentações (Extrato)
- **GET** `/api/account/transactions?page=1&pageSize=10&type=Credit&startDate=2026-01-01`
- **Response 200 OK:**
```json
{
  "items": [
    {
      "id": "e4f8c12a-4a2e-4b6a-93e1-325db172f8a1",
      "accountId": "11111111-1111-1111-1111-111111111111",
      "type": "Entrada",
      "typeValue": 1,
      "amount": 500.00,
      "description": "Recebimento de Cliente - Nota 1042",
      "balanceAfter": 1750.00,
      "createdAt": "2026-09-28T22:05:00Z"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalItems": 1,
  "totalPages": 1
}
```

---

## 🧪 Testes Automatizados

A suíte de testes cobre regras de negócio críticas e os fluxos dos controllers:
- Validação de saldo suficiente vs. saldo insuficiente (não permitindo saldo negativo);
- Validação de valores zerados ou negativos;
- Atualização atômica do saldo da conta;
- Cálculo de totais consolidados de entradas e saídas;
- Paginação e filtros de histórico;
- Respostas HTTP dos controllers.

Para executar os testes:
```bash
cd api
dotnet test
```

---

## 💡 Decisões Técnicas e Boas Práticas

1. **Camada de Serviço e Separação de Responsabilidades:**
   - Controllers finos e responsáveis apenas por validação HTTP e roteamento.
   - Toda lógica de negócio, integridade e regras financeiras concentradas no `AccountService`.
2. **Consistência de Dados e Concorrência:**
   - Uso de transações com estratégia de execução resiliente (`CreateExecutionStrategy`) para garantir isolamento e atomicidade durante as atualizações de saldo e criação de extrato.
3. **Tratamento Global de Erros:**
   - `GlobalExceptionHandlerMiddleware` captura exceções de negócio (`InsufficientBalanceException`, `BusinessException`, `NotFoundException`) e padroniza a resposta JSON com status HTTP semanticos (`422`, `400`, `404`, `500`).
4. **Resiliência e Auto-Provisionamento do Banco:**
   - Na inicialização, a API aguarda ativamente a prontidão do container MySQL e cria o schema e conta padrão automaticamente via `DbInitializer`.
5. **Experiência do Usuário (Frontend):**
   - Interface limpa com React, TypeScript e Tailwind CSS, feedback em tempo real para operações bem-sucedidas ou bloqueadas (saldo insuficiente), formatação monetária em Real (R$) e filtros de extrato intuitivos.

---

## 🔮 Possíveis Evoluções Futuras

- **Autenticação & Autorização:** Implementação de JWT / Identity com suporte a múltiplos usuários e perfis de acesso.
- **Multi-tenant / Múltiplas Contas:** Suporte para gerenciamento de múltiplas contas bancárias por empresa.
- **Event Sourcing / Ledger Financeiro Imutável:** Utilização de arquitetura orientada a eventos para rastreabilidade estrita de cada centavo movimentado.
- **Exportação de Extrato:** Geração de extrato em formatos PDF, CSV e OFX para integração contábil.
- **Mensageria Assíncrona:** Publicação de eventos em RabbitMQ ou Apache Kafka para notificações de transações (e-mail, push, webhooks).
