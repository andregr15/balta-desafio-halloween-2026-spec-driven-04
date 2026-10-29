# Tasks: Código Customizado para URL Curta

**Input**: `specs/001-custom-short-code/` (plan.md, spec.md, research.md, data-model.md, contracts/create-short-url.md, quickstart.md)

**Prerequisites**: plan.md, spec.md

**Tests**: Sem projeto de testes (constituição VII). Validação por cenários em `EncurtaUrl.Api.http` + `dotnet build` sem warnings novos.

**Organization**: Tarefas agrupadas por user story. Todos os caminhos relativos à raiz `src/EncurtaUrl/`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência pendente)
- **[Story]**: US1..US4 (spec.md)

## Path Conventions

Projeto único: `src/EncurtaUrl.Api/` (pastas `Contracts/`, `Data/`, `Endpoints/`, `Services/`, `Models/`).

---

## Phase 1: Setup

- [X] T001 Rodar `dotnet build` em `src/EncurtaUrl.Api/` e anotar o número de warnings atuais (baseline para "sem warnings novos").

---

## Phase 2: Foundational (bloqueia todas as stories)

- [X] T002 [P] Em `src/EncurtaUrl.Api/Data/AppDbContext.cs`, na configuração de `Code` (`OnModelCreating`), adicionar `.UseCollation("NOCASE")` mantendo `IsRequired()`, `HasMaxLength(32)` e o `HasIndex(x => x.Code).IsUnique()` existentes (unicidade ignora caixa, FR-005).
- [X] T003 [P] Em `src/EncurtaUrl.Api/Contracts/ShortUrlContracts.cs`, alterar para `public record CreateShortUrlRequest(string Url, string? Code = null);` (campo opcional, constituição V). `ShortUrlResponse` permanece inalterado.
- [X] T004 [P] Criar `src/EncurtaUrl.Api/Services/CustomCodeValidator.cs` (namespace `EncurtaUrl.Api.Services`) com `ICustomCodeValidator { string[] Validate(string code); }` e a implementação `CustomCodeValidator`. Avalia todas as regras e acumula mensagens (vazio = válido), sem `Trim`:
  - tamanho 3–15 inclusive → "O código deve ter entre 3 e 15 caracteres."
  - formato `^[A-Za-z0-9_-]+$` via `[GeneratedRegex]` (ASCII) → "O código deve conter apenas letras, números, hífen e underscore."
  - reservado: `HashSet<string>` com `StringComparer.OrdinalIgnoreCase` contendo `urls` e `openapi` → "O código informado é reservado."
- [X] T005 Em `src/EncurtaUrl.Api/Program.cs`, registrar `builder.Services.AddSingleton<ICustomCodeValidator, CustomCodeValidator>();` ao lado do `IShortCodeGenerator` (depende de T004).
- [X] T006 Em `CreateAsync` de `src/EncurtaUrl.Api/Endpoints/ShortUrlEndpoints.cs`, injetar `ICustomCodeValidator validator` e substituir a validação isolada de `url` por um dicionário `Dictionary<string, string[]>` de erros: `url` (regra atual, mesma mensagem) e, se `request.Code` não for nulo/em branco (`string.IsNullOrWhiteSpace`), `code` = `validator.Validate(request.Code)` quando houver mensagens. Se o dicionário tiver itens → `TypedResults.ValidationProblem(errors)` com ambos os campos (FR-009/010). Assim nenhum código inválido chega à criação em nenhuma story (depende de T003, T005).

**Checkpoint**: `dotnet build` compila; validação de `url` e `code` ativa; comportamento de criação ainda inalterado.

---

## Phase 3: User Story 1 - Encurtar URL com código customizado (P1) 🎯 MVP

**Goal**: criar URL curta informando `code` válido e livre; sem `code`/em branco gera automaticamente.

**Independent Test**: `POST /urls` com `{ "url": "https://balta.io", "code": "balta-io" }` → 201 com `code` = `balta-io` e `shortUrl` terminando em `/balta-io`; sem `code` → código gerado.

- [X] T007 [US1] Em `CreateAsync` de `src/EncurtaUrl.Api/Endpoints/ShortUrlEndpoints.cs`, se `!string.IsNullOrWhiteSpace(request.Code)`: usar `request.Code` (caixa preservada, sem trim) como `code` e pular o loop de geração; caso contrário manter o loop atual (FR-001/002/003). Tipo de retorno `Results<Created<ShortUrlResponse>, ValidationProblem, ProblemHttpResult>` já cobre os casos.
- [X] T008 [US1] No loop de geração do mesmo arquivo, trocar a checagem de colisão por `db.ShortUrls.AnyAsync(x => EF.Functions.Collate(x.Code, "NOCASE") == candidate, ct)` para o gerador nunca produzir código que viole o índice NOCASE.
- [X] T009 [US1] Em `src/EncurtaUrl.Api/EncurtaUrl.Api.http`, adicionar blocos `###` em português (padrão existente): "Criar URL curta com código customizado (201, code = balta-io)" e "Código em branco gera código automático (201)" com `"code": ""` e `"code": "   "`.

**Checkpoint**: US1 funcional isoladamente (formato/tamanho/reservado já validados pelo T006; unicidade vem na US3).

---

## Phase 4: User Story 2 - Navegar pelo código customizado (P1)

**Goal**: `GET /{code}` redireciona e conta clique para código customizado.

**Independent Test**: criar `balta-io`, `GET /balta-io` e `GET /BALTA-IO` → 302 para a URL original; `GET /urls/balta-io` mostra `clicks` +2.

- [X] T010 [US2] Em `RedirectAsync` e `GetByCodeAsync` de `src/EncurtaUrl.Api/Endpoints/ShortUrlEndpoints.cs`, trocar `x.Code == code` por `EF.Functions.Collate(x.Code, "NOCASE") == code` (busca ignora caixa também em bancos antigos, FR-004). Em `src/EncurtaUrl.Api/EncurtaUrl.Api.http`, adicionar os blocos "Navegar pelo código customizado (302)", "Navegar com caixa diferente `/BALTA-IO` (302)" e "Detalhes do código customizado (clicks +1)".

---

## Phase 5: User Story 3 - Recusar códigos inválidos ou em uso (P2)

**Goal**: recusar com mensagem em português no campo `code` (400 `ValidationProblem`), nada gravado.

**Independent Test**: código repetido (inclusive variação de caixa), com caracteres inválidos, tamanho fora de 3–15 e reservado (`urls`, `URLS`) → todos 400 com `errors.code`; `url` inválida + `code` inválido → 400 com `url` e `code`.

- [X] T011 [US3] No mesmo arquivo, no ramo de código customizado checar `await db.ShortUrls.AnyAsync(x => EF.Functions.Collate(x.Code, "NOCASE") == request.Code, ct)`; se existir → `TypedResults.ValidationProblem` 400 com `["code"] = [$"O código '{request.Code}' já está em uso."]` (FR-005, sem 409).
- [X] T012 [US3] No mesmo arquivo, envolver `db.SaveChangesAsync(ct)` em try/catch de `DbUpdateException` com filtro `when (customCode && ex.InnerException is SqliteException { SqliteErrorCode: 19, SqliteExtendedErrorCode: 2067 })` retornando o mesmo `ValidationProblem` 400 do T011 (corrida, SC-005). Demais exceções propagam ao tratamento global. Adicionar `using Microsoft.Data.Sqlite;` (transitivo, sem pacote novo).
- [X] T013 [US3] Em `src/EncurtaUrl.Api/EncurtaUrl.Api.http`, adicionar blocos de erro em português com resultado esperado: código em uso (400), variação de caixa `BALTA-IO` (400), formato inválido (`balta io`, `a/b`, `olá`), tamanho (2 e 16 caracteres → 400; 3 e 15 → 201), reservado (`urls`, `URLS`), espaço nas pontas `" abc"` (400, sem trim), e `url` inválida + `code: "!"` (400 com os dois campos).

**Checkpoint**: US1–US3 completas.

---

## Phase 6: User Story 4 - Listagem e detalhes inalterados (P3)

**Goal**: listar e detalhar com mesma estrutura para código gerado e customizado; clientes antigos seguem funcionando.

**Independent Test**: criar uma URL gerada e uma customizada; `GET /urls` e `GET /urls/{code}` retornam os mesmos campos; `POST /urls` só com `url` continua 201.

- [X] T014 [US4] Nenhuma mudança de código esperada (`ShortUrlResponse` inalterado). Verificar manualmente e garantir em `src/EncurtaUrl.Api/EncurtaUrl.Api.http` que os blocos existentes de criar sem código, listar e detalhes continuam presentes e funcionando (FR-011/012).

---

## Phase 7: Polish & Cross-Cutting

- [X] T015 Rodar `dotnet build` em `src/EncurtaUrl.Api/` e confirmar zero warnings novos frente ao baseline do T001 (constituição VII).
- [X] T016 Apagar o arquivo SQLite local de desenvolvimento (para `EnsureCreated` recriar a tabela com collation NOCASE), subir a API e executar todos os cenários de `specs/001-custom-short-code/quickstart.md` (#1–#12), incluindo a corrida simultânea (#11) com duas requisições em paralelo.

---

## Dependencies & Execution Order

- Phase 1 → Phase 2 → stories → Polish.
- Foundational: T002, T003, T004 em paralelo (arquivos distintos); T005 depende de T004; T006 depende de T003 e T005.
- US1 (T007–T009) depende de T006. T007 e T008 editam o mesmo método: sequenciais.
- US2 (T010) depende de US1 (precisa criar o código para navegar) e edita `ShortUrlEndpoints.cs`: sequencial.
- US3 (T011–T013) depende de US1 e edita o mesmo método `CreateAsync`: sequencial após T007/T008. T013 após T011–T012.
- US4 (T014) depende de US1; independente de US3.
- Todas as edições em `EncurtaUrl.Api.http` e `ShortUrlEndpoints.cs` são no mesmo arquivo: não paralelizar entre stories.

## Parallel Example

```text
T002 (Data/AppDbContext.cs) | T003 (Contracts/ShortUrlContracts.cs) | T004 (Services/CustomCodeValidator.cs)
```

## Implementation Strategy

- **MVP**: Phases 1–3 (US1) + US2 (T010). A validação de formato, tamanho e reservados já vem na Foundational (T006). A US3 (unicidade com mensagem clara, corrida) vem logo depois.
- Entrega incremental: Foundational → US1 → US2 → US3 → US4 → Polish; validar com o `.http` após cada story.
