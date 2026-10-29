<!--
Sync Impact Report
- Version change: (template, unversioned) -> 1.0.0
- Modified principles: none renamed (initial adoption; all placeholders replaced)
- Added sections: Core Principles I-VIII, Restrições Técnicas, Fluxo de Desenvolvimento e Validação, Governance
- Removed sections: none
- Follow-up TODOs: none
- Notes: documents conventions the code in EncurtaUrl.Api ALREADY follows. Observed code
  facts: .NET 10, Minimal APIs, EF Core SQLite, EnsureCreated in Program.cs,
  AddProblemDetails + UseStatusCodePages (bare NotFound becomes ProblemDetails body),
  only packages Microsoft.AspNetCore.OpenApi and Microsoft.EntityFrameworkCore.Sqlite.
- Remove this comment before committing.
-->
# EncurtaUrl.Api Constitution

## Core Principles

### I. Simplicidade
A solução MUST ser uma única API em .NET 10 com Minimal APIs. MUST NOT existir camadas extras
(Application/Domain/Infrastructure), MediatR ou padrão Repository. Endpoints MUST usar o
`AppDbContext` diretamente. Abstrações novas só são aceitas com justificativa no plano.
Rationale: o projeto é pequeno e didático; indireção adicional custa mais do que entrega.

### II. Organização do Código
- Endpoints MUST ficar em `Endpoints/` como métodos de extensão sobre `IEndpointRouteBuilder`
  (ex.: `MapShortUrlEndpoints`), agrupados com `MapGroup`, com `WithName` e `WithTags`, e
  registrados em `Program.cs`.
- Contratos (records de request e response) MUST ficar em `Contracts/`.
- Entidades MUST ficar em `Models/`; o `AppDbContext` em `Data/`.
- Serviços pequenos MUST ficar em `Services/`, expostos por interface (ex.:
  `IShortCodeGenerator`) e registrados via DI em `Program.cs`.
- Handlers são métodos `private static` nomeados com sufixo `Async` quando assíncronos, e
  recebem `CancellationToken`.

### III. Respostas HTTP
- Handlers MUST usar `TypedResults` e declarar `Results<...>` (ou o tipo único) na assinatura.
- Erros de validação MUST retornar `TypedResults.ValidationProblem` (400).
- Demais erros MUST retornar ProblemDetails com o status correto (ex.: `TypedResults.Problem`
  para 500; 404 via `TypedResults.NotFound()` é aceito porque `AddProblemDetails` e
  `UseStatusCodePages` o convertem em ProblemDetails).
- MUST NOT retornar `IResult` genérico nem objetos anônimos para erros.

### IV. Dados
- Persistência MUST usar EF Core com SQLite.
- O banco MUST ser criado com `EnsureCreated`; migrations MUST NOT ser usadas.
- Qualquer mudança de schema (tabela, coluna, índice, restrição) MUST ser justificada no plano,
  incluindo o impacto em bancos SQLite já criados (`EnsureCreated` não altera tabelas
  existentes).
- Mapeamento de entidades fica em `OnModelCreating` (Fluent API), como já feito.
- Consultas somente leitura SHOULD usar `AsNoTracking`.

### V. Compatibilidade de Contratos
Contratos existentes (rotas, verbos, records de request/response, códigos de status) MUST NOT
quebrar. Campos novos em requests MUST ser opcionais (nullable/valor padrão). Campos novos em
responses MAY ser adicionados ao final. Remoções ou renomeações exigem emenda desta
constituição e justificativa explícita no plano.

### VI. Dependências
Nenhum pacote NuGet novo MUST ser adicionado sem justificativa explícita no plano. Pacotes
atuais: `Microsoft.AspNetCore.OpenApi` e `Microsoft.EntityFrameworkCore.Sqlite`. Preferir
recursos do framework antes de adicionar pacotes.

### VII. Validação Sem Projeto de Testes
Não há projeto de testes. Toda feature MUST adicionar cenários de sucesso e de erro em
`EncurtaUrl.Api.http`, no padrão existente (bloco `###` com título descritivo em português e
resultado esperado). O projeto MUST compilar com `dotnet build` sem warnings novos.
Criar um projeto de testes exige emenda desta constituição.

### VIII. Idioma
Mensagens de erro exibidas ao usuário (títulos de ProblemDetails, mensagens de validação)
MUST ser em português. Código, nomes de tipos/membros e comentários técnicos seguem o padrão
atual (identificadores em inglês; comentários em português quando existentes).

## Restrições Técnicas

- Stack: .NET 10, `Nullable` e `ImplicitUsings` habilitados, SQLite via connection string
  `Default`.
- Namespaces seguem `EncurtaUrl.Api.<Pasta>`.
- OpenAPI exposto apenas em Development.
- Tratamento global: `AddProblemDetails`, `UseExceptionHandler` e `UseStatusCodePages`
  MUST permanecer configurados.

## Fluxo de Desenvolvimento e Validação

- Todo plano MUST incluir um Constitution Check cobrindo os princípios I-VIII, listando
  explicitamente schema, dependências e contratos afetados.
- Antes de concluir uma feature: `dotnet build` sem warnings novos e cenários novos no
  arquivo `.http` (sucesso e erro).
- Violações justificadas MUST ser registradas na seção de complexidade do plano.

## Governance

Esta constituição prevalece sobre outras práticas do projeto. Emendas MUST ser feitas via
`/speckit-constitution`, com atualização do Sync Impact Report e da versão.
Versionamento semântico: MAJOR para remoção/redefinição incompatível de princípios; MINOR
para novo princípio ou expansão material; PATCH para esclarecimentos e ajustes de redação.
Revisões de plano e de implementação MUST verificar conformidade com os princípios.

**Version**: 1.0.0 | **Ratified**: 2026-10-29 | **Last Amended**: 2026-10-29
