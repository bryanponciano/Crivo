# 🛡️ Crivo — Sistema de Gestão B2B e Filtro de Endpoint

Sistema corporativo de controle de produtividade e bloqueio avançado de sites, com interceptação no nível do sistema operacional via Windows Filtering Platform (WFP).

## Arquitetura

| Componente | Tecnologia | Descrição |
|:---|:---|:---|
| **Crive.Shared** | .NET 8 Class Library | DTOs, Enums e Contratos SignalR compartilhados |
| **Crive.Api** | ASP.NET Core 8 | API REST + SignalR Hubs (backend multi-tenant) |
| **Crive.Agent** | .NET 8 Worker Service | Agente Windows com WFP, DNS proxy e bloqueio SNI |
| **crive-dashboard** | React 18 + TypeScript + Vite | Painel web SPA para gestão |

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para PostgreSQL + Redis)

## Setup Rápido

### 1. Subir banco de dados e cache
```bash
cd deploy
docker-compose up -d
```

### 2. Rodar o Backend
```bash
cd src/Crive.Api
dotnet run
```
A API estará em `https://localhost:5001` e `http://localhost:5000`

### 3. Rodar o Frontend
```bash
cd src/crive-dashboard
npm install
npm run dev
```
O dashboard estará em `http://localhost:5173`

### 4. Compilar o Agente (requer Windows)
```bash
cd src/Crive.Agent
dotnet publish -c Release -r win-x64 --self-contained
```

## Credenciais de Desenvolvimento

| Campo | Valor |
|:---|:---|
| Email | admin@exemplo.com |
| Senha | admin123 |
| Tenant Code | exemplo |
| Senha Desinstalação | CriveAdmin2024! |

## Licença

Proprietário — Todos os direitos reservados.
