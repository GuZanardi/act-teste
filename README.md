# 💼 Desafio Técnico act digital - Gestão de Conta Empresarial

Solução desenvolvida para o controle de movimentações financeiras de uma conta empresarial por meio de entradas e saídas de valores, consulta de saldo em tempo real e extrato com histórico detalhado das movimentações, **garantindo a consistência dos registros, sem permitir saldo negativo, com suporte a .env e EF Core Migrations**.

---

## 📌 Sumário

1. [Visão Geral e Arquitetura](#-visão-geral-e-arquitetura)
2. [Tecnologias Utilizadas](#-tecnologias-utilizadas)
3. [Segurança e Variáveis de Ambiente (.env)](#-segurança-e-variáveis-de-ambiente-env)
4. [Banco de Dados e Migrações (EF Core Migrations)](#-banco-de-dados-e-migrações-ef-core-migrations)
5. [Estrutura do Repositório](#-estrutura-do-repositório)
6. [Requisitos de Negócio Implementados](#-requisitos-de-negócio-implementados)
7. [Como Executar a Aplicação](#-como-executar-a-aplicação)
   - [Opção 1: Executando com Docker Compose (Recomendado)](#opção-1-executando-com-docker-compose-recomendado)
   - [Opção 2: Executando Localmente](#opção-2-executando-localmente)
8. [Documentação dos Endpoints (API)](#-documentação-dos-endpoints-api)
9. [Testes Automatizados](#-testes-automatizados)
10. [Decisões Técnicas e Boas Práticas](#-decisões-técnicas-e-boas-práticas)
11. [Possíveis Evoluções Futuras](#-possíveis-evoluções-futuras)

---

## 🏗 Visão Geral e Arquitetura

A solução foi projetada de forma simples, desacoplada e segura, priorizando a manutenibilidade, legibilidade e confiabilidade das regras de negócio sem complexidade desnecessária.

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
│   [EnvLoader] ──► [Controllers] ──► [Services / Business]   │
│   (Lê .env)      (Account, Tx)    (Validação & Atomicidade) │
│                                         │                   │
│   [Global Exception Middleware]         ▼                   │
│   (Respostas padronizadas)        [AppDbContext]            │
│                                   (EF Core Migrations)      │
└──────────────────────────────┬──────────────────────────────┘
                               │ MySQL Connector
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                      Banco de Dados MySQL                   │
│        Tabelas: Accounts, Transactions (Índices & FKs)      │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔒 Segurança e Variáveis de Ambiente (.env)

Todas as credenciais sensíveis do banco de dados e portas foram **removidas dos arquivos `appsettings.json`** e agora são gerenciadas de forma segura através do arquivo `.env`.

### Arquivo `.env` (ou `.env.example` para referência):

```env
# Banco de Dados MySQL
# DB_PORT     = porta interna do container (sempre 3306)
# HOST_DB_PORT = porta exposta no host (3307 evita conflito com MySQL local)
DB_SERVER=mysql
DB_PORT=3306
HOST_DB_PORT=3307
DB_NAME=financial_db
DB_USER=act_user
DB_PASSWORD=act_password
MYSQL_ROOT_PASSWORD=root_password

# API .NET 9
ASPNETCORE_ENVIRONMENT=Production
API_PORT=5001

# Frontend React
REACT_APP_API_URL=http://localhost:5001
FRONTEND_PORT=3000
```

- A API carrega as variáveis dinamicamente tanto via **Docker Compose** quanto nativamente no C# via [`EnvLoader`](file:///Users/gustavozanardi/Documents/Teste%20ACT/api/FinancialAccount.API/Common/EnvLoader.cs).
- O `.env` está incluído no `.gitignore` para prevenir vazamento de credenciais em repositórios públicos.

---

## 🗄 Banco de Dados e Migrações (EF Core Migrations)

O schema do banco de dados é versionado e gerenciado através do **Entity Framework Core Migrations**:

- **Histórico de Migrações:** Localizado em [`api/FinancialAccount.API/Migrations`](file:///Users/gustavozanardi/Documents/Teste%20ACT/api/FinancialAccount.API/Migrations).
  - `20260929000000_InitialCreate.cs`: Cria as tabelas `Accounts` e `Transactions`, chaves primárias, chaves estrangeiras com delete em cascata e índices de performance em `CreatedAt` e `AccountId`.
  - `AppDbContextModelSnapshot.cs`: Snapshot do modelo de dados.
- **Execução Automática na Inicialização:** Ao subir a API (via Docker ou Local), o [`DbInitializer`](file:///Users/gustavozanardi/Documents/Teste%20ACT/api/FinancialAccount.API/Data/DbInitializer.cs) executa automaticamente `context.Database.MigrateAsync()`, aplicando quaisquer migrações pendentes e criando a conta empresarial inicial (seed) caso não exista.

---

## 🚀 Tecnologias Utilizadas

### Backend (API)

- **.NET 9** (C# 13)
- **ASP.NET Core Web API**
- **Entity Framework Core 9 Migrations** + **Pomelo MySQL**
- **Swagger / OpenAPI**
- **xUnit**, **FluentAssertions** e **Moq**

### Frontend (SPA)

- **React 18** com **TypeScript**
- **Tailwind CSS**
- **Lucide React**
- **Axios**

### Infraestrutura

- **Docker & Docker Compose**
- **MySQL 8.0**
- **Nginx Alpine**

---

## 📁 Estrutura do Repositório

```text
├── .env                                 # Variáveis de ambiente (ignorado no git)
├── .env.example                         # Exemplo de configuração de variáveis
├── docker-compose.yml                   # Orquestração do MySQL, API e Frontend
├── README.md                            # Documentação da solução
│
├── api/
│   ├── FinancialAccount.API/
│   │   ├── Common/EnvLoader.cs          # Leitor de arquivo .env para .NET
│   │   ├── Controllers/                 # Endpoints REST (Account, Transactions)
│   │   ├── Data/                        # AppDbContext e DbInitializer (MigrateAsync)
│   │   ├── DTOs/                        # DTOs de Request / Response
│   │   ├── Exceptions/                  # Exceções de Domínio (InsufficientBalanceException)
│   │   ├── Middlewares/                 # Middleware Global de Tratamento de Erros
│   │   ├── Migrations/                  # Arquivos de Migração do EF Core
│   │   ├── Models/                      # Entidades Account, Transaction, TransactionType
│   │   ├── Services/                    # Regras de Negócio e consistência transacional
│   │   ├── Program.cs                   # Configuração e inicialização da API
│   │   ├── Dockerfile                   # Imagem Docker da API
│   │   └── FinancialAccount.API.csproj
│   ├── FinancialAccount.Tests/          # Suíte de Testes Unitários
│   └── FinancialAccount.sln
│
└── app/                                 # Frontend SPA em React + TypeScript + Tailwind
    ├── public/index.html
    ├── src/
    │   ├── components/                  # BalanceSummary, TransactionModal, TransactionTable, etc.
    │   ├── services/api.ts              # Cliente Axios tipado
    │   ├── types/index.ts               # Interfaces TypeScript
    │   ├── App.tsx                      # Componente raiz
    │   ├── index.tsx                    # Ponto de entrada
    │   └── index.css                    # Diretivas Tailwind
    ├── tailwind.config.js
    ├── tsconfig.json
    ├── Dockerfile
    └── package.json
```

---

## 💼 Requisitos de Negócio Implementados

1. **Registrar entradas de valores (Crédito):**
   - Validação de valor positivo (> R$ 0,00).
   - Atualização atômica do saldo da conta e gravação da movimentação com snapshot do saldo resultante.
2. **Registrar saídas de valores (Débito):**
   - Validação de valor positivo (> R$ 0,00).
   - **Garantia de consistência sem saldo negativo:** se a conta não possuir saldo suficiente, a transação é cancelada e rejeitada imediatamente retornando HTTP `422 Unprocessable Entity`.
3. **Consultar o saldo disponível:**
   - Retorna o saldo atualizado, total de créditos, total de débitos e quantidade de transações.
4. **Consultar o histórico de movimentações:**
   - Extrato completo com paginação configurável (`page`, `pageSize`), filtro por período (`startDate`, `endDate`) e filtro por tipo de movimentação (`Credit` ou `Debit`).

---

## 🛠 Como Executar a Aplicação

### Opção 1: Executando com Docker Compose (Recomendado)

```bash
docker compose up --build
```

Os seguintes serviços estarão disponíveis:

- 🌐 **Frontend (React + TS + Tailwind):** [http://localhost:3000](http://localhost:3000)
- 📖 **Swagger da API:** [http://localhost:5001](http://localhost:5001)
- 🗄 **Banco de Dados MySQL:** `localhost:3307` (credenciais lidas do `.env`)

Para encerrar:

```bash
docker compose down
```

---

### Opção 2: Executando Localmente

#### 1. Iniciar o MySQL usando as variáveis do .env

```bash
docker run --name act_mysql --env-file .env -p 3307:3306 -d mysql:8.0
```

> **Nota:** `-p 3307:3306` mapeia a porta **3306 interna** do MySQL container para a porta **3307 do host**, evitando conflito com qualquer MySQL local já instalado.

#### 2. Iniciar a API .NET 9

```bash
cd api/FinancialAccount.API
dotnet run
```

A API aplicará as migrations automaticamente e iniciará em `http://localhost:5001`.

#### 3. Iniciar o Frontend React

```bash
cd ../../app
npm install
npm start
```

O Frontend estará acessível em `http://localhost:3000`.

---

## 🧪 Testes Automatizados

Para executar os testes:

```bash
cd api
dotnet test
```

### Cenários cobertos

#### `AccountServiceTests` – Testes de Unidade (Service + InMemory)

| Teste                                        | Cenário                                                                          |
| -------------------------------------------- | -------------------------------------------------------------------------------- |
| `DepositAsync_WithValidAmount_...`           | Depósito válido aumenta saldo e registra transação no banco                      |
| `DepositAsync_WithZeroOrNegativeAmount_...`  | Valor `<= 0` lança `BusinessException`                                           |
| `DepositAsync_WithoutDescription_...`        | Descrição nula usa texto padrão "Entrada de valores"                             |
| `WithdrawAsync_WithSufficientBalance_...`    | Saque válido reduz saldo e registra transação                                    |
| `WithdrawAsync_WhenAmountExceedsBalance_...` | Saldo insuficiente lança `InsufficientBalanceException` e **não altera o saldo** |
| `WithdrawAsync_WithExactBalance_...`         | Saque do valor exato do saldo → saldo fica R$ 0,00                               |
| `WithdrawAsync_WithZeroOrNegativeAmount_...` | Valor `<= 0` lança `BusinessException`                                           |
| `GetBalanceAsync_...`                        | Retorna saldo, total de créditos, débitos e contagem de transações corretamente  |
| `GetTransactionsAsync_WithFilters_...`       | Paginação retorna itens corretos (`page`, `pageSize`, `totalPages`)              |
| `GetTransactionsAsync_WithTypeFilter_...`    | Filtro por tipo retorna apenas Credit ou Debit                                   |
| `GetTransactionsAsync_WithDateFilter_...`    | Filtro por data retorna apenas transações no período                             |
| `GetOrCreateAccount_WhenNonExistentId_...`   | ID inexistente (não-padrão) lança `NotFoundException`                            |

#### `ControllersTests` – Testes de Unidade (Controllers com Mock)

| Teste                                            | Cenário                                                     |
| ------------------------------------------------ | ----------------------------------------------------------- |
| `AccountController_GetBalance_ReturnsOk`         | `GET /balance` → 200 OK com `BalanceResponseDto`            |
| `AccountController_GetTransactions_ReturnsOk`    | `GET /transactions` → 200 OK com resultado paginado         |
| `TransactionsController_Deposit_ReturnsCreated`  | `POST /deposit` → 201 Created com `TransactionResponseDto`  |
| `TransactionsController_Withdraw_ReturnsCreated` | `POST /withdraw` → 201 Created com `TransactionResponseDto` |

#### `GlobalExceptionMiddlewareTests` – Testes de Unidade (Middleware)

| Teste                                     | Cenário                                             |
| ----------------------------------------- | --------------------------------------------------- |
| `InsufficientBalanceException_Returns422` | Saldo insuficiente → HTTP 422 Unprocessable Entity  |
| `BusinessException_Returns400`            | Violação de regra de negócio → HTTP 400 Bad Request |
| `NotFoundException_Returns404`            | Recurso não encontrado → HTTP 404 Not Found         |
| `UnhandledException_Returns500`           | Erro inesperado → HTTP 500 Internal Server Error    |

---

## 📖 Endpoints da API

- `GET /api/account/balance`: Consulta saldo disponível consolidado.
- `GET /api/account/transactions`: Consulta extrato com filtros (`startDate`, `endDate`, `type`, `page`, `pageSize`).
- `POST /api/transactions/deposit`: Registra entrada de valores.
- `POST /api/transactions/withdraw`: Registra saída de valores (com trava de saldo insuficiente).
