# Research: Código Customizado

Sem NEEDS CLARIFICATION pendentes. Decisões:

## Detectar violação do índice único
- **Decision**: capturar `DbUpdateException` e checar `InnerException is SqliteException { SqliteErrorCode: 19, SqliteExtendedErrorCode: 2067 }` (SQLITE_CONSTRAINT_UNIQUE).
- **Rationale**: modo confiável sem novo pacote; `Microsoft.Data.Sqlite` já é transitivo. Cobre a corrida entre `AnyAsync` e `SaveChanges` (SC-005).
- **Alternatives**: só `AnyAsync` (corrida vira 500); transação serializável (complexo, sem ganho); casar texto da mensagem (frágil).

## Validação do código
- **Decision**: classe própria com `[GeneratedRegex]` e `HashSet<string>` `OrdinalIgnoreCase`.
- **Rationale**: pedido explícito de não usar pacote; regras triviais.
- **Alternatives**: FluentValidation (pacote); DataAnnotations (não valida em Minimal API sem filtro extra).

## Lista de reservados
- **Decision**: `urls`, `openapi`.
- **Rationale**: primeiros segmentos mapeados: `/urls` e `/openapi/{doc}.json` (Development). Status pages e exception handler não têm rota.
- **Alternatives**: ler `EndpointDataSource` em runtime (complexidade e ordem de inicialização sem necessidade).

## Status do conflito
- **Decision**: 400 via `TypedResults.ValidationProblem` com `{ "code": [msg] }` (clarificação 2026-10-29).
- **Rationale**: constituição III; um único formato de erro por campo (FR-009).
- **Alternatives**: 409 Conflict com ProblemDetails (rejeitado na clarificação).

## Caixa e unicidade
- **Decision**: coluna `Code` com collation `NOCASE` (`UseCollation`); checagens de existência com `EF.Functions.Collate(x.Code, "NOCASE")`. `Balta` = `balta` (clarificação 2026-10-29).
- **Rationale**: FR-005; índice único garante a regra na corrida; `Collate` na consulta cobre bancos antigos criados com BINARY.
- **Redirect/detalhe**: também usam `Collate NOCASE` (`/BALTA-IO` → `balta-io`), para valer em bancos antigos.
- **Alternatives**: coluna `CodeNormalized` em minúsculas (coluna extra, mais código); `ToLower()` na consulta (não protege a corrida, não usa índice); migrations (proibidas pela constituição IV).
