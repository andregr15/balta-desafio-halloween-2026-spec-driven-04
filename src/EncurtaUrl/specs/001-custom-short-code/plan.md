# Implementation Plan: Código Customizado para URL Curta

**Branch**: `001-custom-short-code` | **Date**: 2026-10-29 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-custom-short-code/spec.md`

## Summary

Estender `POST /urls` com o campo opcional `code`. Regras do código (formato, tamanho 3–15,
reservados) ficam em um serviço pequeno `ICustomCodeValidator` em `Services/`. Código livre é
usado no lugar do gerado; repetido (ignorando caixa) gera 400 `ValidationProblem` no campo `code`;
corrida entre requisições é resolvida capturando a violação do índice único no `SaveChangesAsync`.
Unicidade sem diferenciar caixa: coluna `Code` passa a usar collation `NOCASE` (mudança de schema
justificada abaixo). Sem pacote novo, sem endpoint novo. Redirecionamento e detalhe passam a buscar ignorando a caixa (`EF.Functions.Collate`); listagem não muda.

## Technical Context

**Language/Version**: C# / .NET 10 (`Nullable` e `ImplicitUsings` habilitados)

**Primary Dependencies**: `Microsoft.AspNetCore.OpenApi`, `Microsoft.EntityFrameworkCore.Sqlite` (nenhum novo)

**Storage**: SQLite via EF Core, `EnsureCreated`; coluna `Code` com `MaxLength(32)`, índice único e collation `NOCASE`

**Testing**: Sem projeto de testes (constituição VII); cenários em `EncurtaUrl.Api.http` + `dotnet build`

**Target Platform**: Servidor ASP.NET Core (Kestrel)

**Project Type**: web-service (API única)

**Performance Goals**: N/A (validação em memória + 1 consulta de existência)

**Constraints**: Contratos existentes intactos; mensagens em português; sem pacote de validação

**Scale/Scope**: 1 endpoint alterado, 1 serviço novo, 1 record estendido

## Constitution Check

| Princípio | Status | Nota |
|---|---|---|
| I. Simplicidade | PASS | Sem camadas/Repository; endpoint usa `AppDbContext` direto. Única abstração nova: `ICustomCodeValidator` (exigida por II para serviços). |
| II. Organização | PASS | Serviço em `Services/` com interface, DI singleton em `Program.cs`; contrato em `Contracts/`; handler `CreateAsync` mantido. |
| III. Respostas HTTP | PASS | `TypedResults.ValidationProblem` (400, inclusive código em uso) e `TypedResults.Problem` (500 na geração); assinatura `Results<...>` mantida. |
| IV. Dados | PASS (com mudança justificada) | **Schema muda:** `Code` ganha `UseCollation("NOCASE")` em `OnModelCreating`, tornando o índice único e as comparações `==` insensíveis à caixa (spec FR-005). `MaxLength(32)` ≥ 15 permanece. **Bancos já criados:** `EnsureCreated` não altera tabelas existentes, então continuam com collation BINARY. Mitigação: as checagens de existência usam `EF.Functions.Collate(x.Code, "NOCASE")`, garantindo a regra em qualquer banco; só a proteção de corrida por variação de caixa depende do índice novo. Para ter o índice NOCASE em banco antigo, apagar o arquivo SQLite (dev) e deixar recriar. Códigos gerados antigos que diferem só pela caixa seguem válidos; se houver dois, a busca ignorando caixa devolve o primeiro encontrado (caso raro, aceito). |
| V. Compatibilidade | PASS | `Code` opcional com default `null`; response inalterado; cliente que envia só `url` segue funcionando. |
| VI. Dependências | PASS | Nenhum pacote NuGet novo. Detecção do erro usa `Microsoft.Data.Sqlite.SqliteException`, já transitivo de `EntityFrameworkCore.Sqlite`. |
| VII. Validação | PASS | Cenários de sucesso e erro novos em `EncurtaUrl.Api.http`; `dotnet build` sem warnings novos. |
| VIII. Idioma | PASS | Mensagens/títulos em português; identificadores em inglês. |

Pós-design: re-avaliado, sem violações. Complexity Tracking vazio.

## Project Structure

### Documentation (this feature)

```text
specs/001-custom-short-code/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── create-short-url.md
└── tasks.md             # /speckit-tasks (não criado aqui)
```

### Source Code

```text
Data/AppDbContext.cs                  # Code: UseCollation("NOCASE")
Contracts/ShortUrlContracts.cs        # CreateShortUrlRequest(string Url, string? Code = null)
Services/CustomCodeValidator.cs       # NOVO: ICustomCodeValidator + implementação
Endpoints/ShortUrlEndpoints.cs        # CreateAsync: ramo custom + captura de unique violation; RedirectAsync/GetByCodeAsync: busca NOCASE
Program.cs                            # AddSingleton<ICustomCodeValidator, CustomCodeValidator>
EncurtaUrl.Api.http                   # cenários novos
```

**Structure Decision**: Projeto único existente (EncurtaUrl.Api); nenhum diretório novo no código.

## Design

### Contrato
`public record CreateShortUrlRequest(string Url, string? Code = null);` Response inalterado.

### `ICustomCodeValidator` (Services/)
- `string[] Validate(string code)` — mensagens em português (vazio = válido).
- Regras (todas avaliadas, mensagens acumuladas):
  - tamanho 3–15 → "O código deve ter entre 3 e 15 caracteres."
  - formato `^[A-Za-z0-9_-]+$` (ASCII, `[GeneratedRegex]`) → "O código deve conter apenas letras, números, hífen e underscore."
  - reservado (HashSet `OrdinalIgnoreCase`: `urls`, `openapi`) → "O código informado é reservado."
- "Em branco = não informado" é decidido no endpoint (`string.IsNullOrWhiteSpace`). Sem `Trim`: `" abc"` falha no formato.

### Fluxo de `CreateAsync`
1. Monta dicionário de erros: `url` (regra atual) e `code` (se informado e não em branco, via validator). Se houver algum → `ValidationProblem` 400 com ambos (FR-010).
2. Com `code`: `AnyAsync(x => EF.Functions.Collate(x.Code, "NOCASE") == code)`; existe → `ValidationProblem` 400 com `{ code: ["O código 'X' já está em uso."] }`. Livre → usa o código (caixa preservada).
   Sem `code`: loop de geração atual, mas a checagem de colisão também ignora a caixa (evita gerar código que violaria o índice NOCASE).
3. `SaveChangesAsync` em try/catch: `DbUpdateException` com `InnerException is SqliteException { SqliteErrorCode: 19, SqliteExtendedErrorCode: 2067 }` **e** código customizado → mesmo 400 do passo 2. Demais casos propagam ao tratamento global.
4. `RedirectAsync` e `GetByCodeAsync` buscam com `EF.Functions.Collate(x.Code, "NOCASE") == code` (FR-004, qualquer banco).
5. `Created` com `Location /urls/{code}` e response atual.

### Reservados
Primeiros segmentos mapeados pela API: `urls` (grupo `/urls`) e `openapi` (`MapOpenApi`, só em Development; reservado sempre por simplicidade).

## Complexity Tracking

Sem violações.
