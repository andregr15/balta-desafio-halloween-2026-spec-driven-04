# Feature Specification: Código Customizado para URL Curta

**Feature Branch**: `001-custom-short-code`

**Created**: 2026-10-29

**Status**: Draft

**Input**: User description: "Permitir que o usuário informe um código customizado ao encurtar uma URL, para depois navegar por ele (ex.: /balta-io em vez de /aZ3k9Qx). Extensão do fluxo atual de criação de URL curta, não um sistema novo."

## Clarifications

### Session 2026-10-29

- Q: Qual o tamanho mínimo e máximo aceito para um código customizado? → A: 3 a 15 caracteres (revisado de 3 a 8 a pedido do usuário)
- Q: Qual status HTTP a API deve devolver quando o código customizado já está em uso? → A: 400 ValidationProblem, erro no campo do código
- Q: Códigos que diferem só pela caixa (ex.: `Balta` e `balta`) são o mesmo código ou distintos? → A: Mesmo código (unicidade ignora caixa)
- Q: Navegar por `/BALTA-IO` quando o código é `balta-io` deve redirecionar? → A: Sim, o redirecionamento (e o detalhe) ignora a caixa

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Encurtar URL com código customizado (Priority: P1)

Ao encurtar uma URL, o usuário pode informar opcionalmente um código de sua escolha (ex.: `balta-io`). Se o código for válido e estiver livre, a URL curta passa a usar esse código no lugar de um código gerado automaticamente.

**Why this priority**: É o valor central da feature: links curtos legíveis e memorizáveis.

**Independent Test**: Criar uma URL curta informando o código `balta-io` e verificar que a resposta traz esse código e o link curto `/balta-io`.

**Acceptance Scenarios**:

1. **Given** o código `balta-io` não está em uso, **When** o usuário cria uma URL curta informando a URL original e o código `balta-io`, **Then** a criação é aceita e o código da URL curta é exatamente `balta-io`.
2. **Given** o usuário não informa código customizado, **When** cria uma URL curta, **Then** um código é gerado automaticamente, exatamente como hoje.
3. **Given** o usuário informa o código em branco ou vazio, **When** cria uma URL curta, **Then** o sistema o trata como não informado e gera o código automaticamente.

---

### User Story 2 - Navegar pelo código customizado (Priority: P1)

Qualquer pessoa que acesse `/{codigo}` de uma URL com código customizado é redirecionada para a URL original, e o acesso é contado como clique.

**Why this priority**: Sem o redirecionamento o código customizado não tem utilidade.

**Independent Test**: Criar uma URL com código `balta-io`, acessar `/balta-io` e verificar o redirecionamento e o incremento do contador de cliques.

**Acceptance Scenarios**:

1. **Given** existe uma URL curta com código customizado `balta-io`, **When** alguém acessa `/balta-io`, **Then** é redirecionado para a URL original.
2. **Given** a mesma URL curta, **When** o acesso ocorre, **Then** o contador de cliques aumenta em 1, como em URLs com código gerado.
3. **Given** a mesma URL curta, **When** alguém acessa `/BALTA-IO` (caixa diferente), **Then** é redirecionado para a URL original e o clique é contado.

---

### User Story 3 - Recusar códigos inválidos ou em uso (Priority: P2)

O sistema recusa a criação quando o código customizado já existe, tem formato inválido ou é reservado, informando o motivo em português.

**Why this priority**: Protege a unicidade dos links e a integridade das rotas, mas depende de a criação customizada existir.

**Independent Test**: Tentar criar URLs com código repetido, com caracteres inválidos, curto/longo demais e reservado, e verificar que cada tentativa é recusada com mensagem clara e nada é gravado.

**Acceptance Scenarios**:

1. **Given** o código `balta-io` já pertence a outra URL curta, **When** o usuário tenta criar outra URL com `balta-io`, **Then** a criação é recusada com mensagem informando que o código está em uso, e a URL existente permanece inalterada.
2. **Given** um código com caracteres fora de letras, números, hífen e underscore (ex.: `balta io`, `a/b`, `olá`), **When** o usuário tenta criar, **Then** a criação é recusada com mensagem sobre o formato permitido.
3. **Given** um código menor que o mínimo ou maior que o máximo permitido, **When** o usuário tenta criar, **Then** a criação é recusada com mensagem informando os limites de tamanho.
4. **Given** um código reservado por conflitar com rotas da própria API (ex.: `urls`), **When** o usuário tenta criar, **Then** a criação é recusada com mensagem informando que o código é reservado.
5. **Given** a URL original é inválida e o código customizado também, **When** o usuário tenta criar, **Then** a recusa lista os problemas de cada campo.

---

### User Story 4 - Listagem e detalhes inalterados (Priority: P3)

A listagem e o detalhe de URLs curtas exibem URLs com código gerado e customizado da mesma forma, sem distinção de formato ou campos novos obrigatórios.

**Why this priority**: Garante compatibilidade; nenhum consumidor existente deve quebrar.

**Independent Test**: Criar uma URL com código gerado e outra com customizado; listar e consultar o detalhe de ambas e verificar a mesma estrutura de resposta.

**Acceptance Scenarios**:

1. **Given** existem URLs com código gerado e customizado, **When** o usuário lista as URLs, **Then** ambas aparecem com os mesmos campos e formato.
2. **Given** uma URL com código customizado, **When** o usuário consulta seus detalhes pelo código, **Then** recebe os dados completos, incluindo cliques.

---

### Edge Cases

- Código customizado que difere de um existente apenas pela caixa (ex.: `Balta` vs `balta`): recusado como em uso, pois a unicidade ignora a caixa.
- Código com exatamente o tamanho mínimo ou máximo: aceito.
- Código com espaços nas pontas: considerado inválido (não é aparado silenciosamente), salvo se em branco por completo.
- Duas requisições simultâneas com o mesmo código customizado: apenas uma é aceita; a outra recebe a recusa de código em uso, sem erro genérico.
- Código gerado automaticamente coincidindo com um customizado existente: o gerador continua evitando colisões, como hoje.
- Código customizado idêntico a um código gerado de outra URL: recusado como em uso.
- Código reservado digitado com variação de caixa (ex.: `URLS`): recusado como reservado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir informar, opcionalmente, um código customizado ao criar uma URL curta.
- **FR-002**: Quando o código customizado não for informado (ausente, nulo ou em branco), o sistema MUST gerar o código automaticamente, com comportamento idêntico ao atual.
- **FR-003**: Quando o código customizado for válido e livre, o sistema MUST usá-lo como código da URL curta, e o link curto devolvido MUST refletir esse código.
- **FR-004**: O sistema MUST redirecionar `/{codigo}` de uma URL com código customizado para a URL original e MUST contar o acesso como clique, como já ocorre com códigos gerados. A busca do código MUST ignorar maiúsculas e minúsculas (ex.: `/BALTA-IO` redireciona para a URL de `balta-io`).
- **FR-005**: O sistema MUST garantir unicidade do código sem diferenciar maiúsculas de minúsculas: se já existir (gerado ou customizado, em qualquer variação de caixa), a criação MUST ser recusada com mensagem em português informando que o código está em uso, sem alterar a URL existente. A recusa MUST ser um ValidationProblem (HTTP 400) com o erro no campo do código, igual aos demais erros de validação (sem 409).
- **FR-006**: O sistema MUST aceitar apenas letras (A–Z, a–z), números, hífen e underscore em códigos customizados.
- **FR-007**: O sistema MUST exigir que o código customizado tenha entre 3 e 15 caracteres, inclusive.
- **FR-008**: O sistema MUST recusar códigos reservados que conflitam com rotas da própria API (no mínimo `urls`, e os demais caminhos publicados pela API), sem diferenciar maiúsculas de minúsculas.
- **FR-009**: Toda recusa por código inválido, reservado ou em uso MUST informar o motivo de forma clara, em português, associada ao campo do código.
- **FR-010**: A validação da URL original MUST continuar como hoje; erros em ambos os campos MUST ser reportados juntos.
- **FR-011**: A listagem e o detalhe de URLs curtas MUST funcionar de forma idêntica para URLs com código gerado ou customizado, sem alterar campos existentes.
- **FR-012**: Clientes existentes que enviam apenas a URL MUST continuar funcionando sem qualquer mudança.

### Key Entities

- **URL Curta**: Associação entre uma URL original e um código único, com data de criação e contagem de cliques. O código pode ter origem automática ou customizada; após criado, não muda.
- **Código Customizado**: Texto escolhido pelo usuário, de 3 a 15 caracteres com letras, números, hífen e underscore, único entre todos os códigos e fora da lista de códigos reservados.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Usuário cria uma URL curta com código personalizado em uma única solicitação, sem etapas adicionais.
- **SC-002**: 100% das URLs criadas com código customizado válido redirecionam corretamente para a URL original e registram o clique.
- **SC-003**: 100% das tentativas com código repetido, inválido ou reservado são recusadas com mensagem que identifica o motivo, e nenhuma URL é criada ou alterada nesses casos.
- **SC-004**: 100% dos fluxos existentes (criar sem código, listar, consultar detalhes, navegar) mantêm o mesmo resultado de antes da feature.
- **SC-005**: Em tentativas simultâneas com o mesmo código, no máximo uma é aceita.

## Assumptions

- Unicidade ignora a caixa (confirmado na clarificação), tanto para códigos customizados quanto para checagem contra códigos gerados; a lista de códigos reservados também ignora a caixa. O código é armazenado e devolvido com a caixa informada. O redirecionamento e a consulta de detalhe também ignoram a caixa (`/BALTA-IO` leva a `balta-io`). Como URLs antigas podem ter códigos gerados que diferem só pela caixa, o plano deve tratar o impacto no schema/índice (constituição, princípio IV) e em bancos já criados.
- Limites de tamanho confirmados na clarificação: mínimo 3 e máximo 15 caracteres.
- Código em branco é tratado como não informado.
- Lista de códigos reservados inclui os caminhos de rotas existentes na API (como `urls`) e os de documentação exposta em desenvolvimento; a lista exata é definida no planejamento.
- Não há autenticação: qualquer pessoa pode reivindicar qualquer código livre.
- Fora do escopo: editar ou remover o código depois de criado, autenticação, expiração de links.
- Bancos já existentes continuam válidos; URLs antigas mantêm seus códigos gerados.
