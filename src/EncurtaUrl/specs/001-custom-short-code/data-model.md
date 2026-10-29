# Data Model

## ShortUrl

| Campo | Tipo | Regra |
|---|---|---|
| Id | int | PK |
| Code | string (máx. 32) | obrigatório, índice único, collation `NOCASE` (ignora caixa); gerado (7 chars) ou customizado (3–15) |
| OriginalUrl | string (máx. 2048) | obrigatório, http/https absoluta |
| CreatedAt | DateTime | UTC |
| Clicks | int | incrementa no redirecionamento |

**Schema**: única mudança é a collation `NOCASE` em `Code` (15 ≤ 32 mantido). Bancos existentes continuam válidos, porém mantêm collation BINARY até o arquivo ser recriado (`EnsureCreated` não altera tabelas); a checagem de existência por `EF.Functions.Collate` aplica a regra mesmo assim.

## Código Customizado (valor, sem tabela)

- Tamanho 3–15 inclusive.
- Caracteres `A-Z a-z 0-9 - _` (ASCII).
- Fora de `{urls, openapi}` (sem diferenciar caixa).
- Sem trim; só brancos = não informado.
- Imutável após criação.
