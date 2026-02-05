# ESPECIFICAÇÃO TÉCNICA - FASE 3: MÓDULO FISCAL (NFC-e)

**Projeto:** PDV Offline-First
**Versão do Documento:** 1.0
**Escopo:** Implementação da emissão de Nota Fiscal de Consumidor Eletrônica (Modelo 65), gestão de contingência offline e impressão térmica (DANFE).

---

## 1. Visão Arquitetural e Padrões

O módulo fiscal deve ser implementado como um serviço de infraestrutura isolado, garantindo que as regras de negócio do PDV (Core) não sejam acopladas a bibliotecas específicas de terceiros.

### 1.1. Padrão de Projeto: Strategy

Deve-se utilizar o padrão **Strategy** para abstrair a implementação da comunicação fiscal. O sistema deve ser capaz de alternar entre diferentes provedores sem alterar o fluxo de venda.

* **Interface Abstrata:** O Domínio deve interagir apenas com uma interface genérica (ex: `IFiscalManager`).
* **Estratégias Concretas:**
* `NfceProvider`: Comunicação padrão via Webservice SEFAZ.
* `SatProvider`: Comunicação com hardware SAT (para expansão futura SP).
* `MockProvider`: Simulação para ambientes de desenvolvimento e testes automatizados.



### 1.2. Isolamento de Camadas (Anti-Corruption Layer)

* As entidades de venda do Domínio não devem conter anotações ou dependências diretas da biblioteca fiscal.
* Deve existir um **Mapper/Builder** responsável por converter a Entidade `Venda` para o Objeto de Transferência (DTO/XML) exigido pela biblioteca fiscal.

---

## 2. Requisitos Funcionais (FR)

### FR-01: Configuração Fiscal

O sistema deve permitir a configuração persistente dos dados do emitente por loja/terminal:

* Carga de Certificado Digital A1 (Arquivo `.pfx` e Senha).
* Configuração de Ambiente (Produção vs. Homologação).
* Token CSC (Código de Segurança do Contribuinte) e ID do Token.
* Dados tributários padrão (Regime Tributário, CRT).
* Série da Nota Fiscal e Controle de Numeração Sequencial.

### FR-02: Emissão Síncrona (Fluxo Online)

No momento da finalização da venda, se houver conexão com a internet:

1. Validar pré-requisitos cadastrais (NCM, CFOP, Impostos).
2. Gerar o XML assinado digitalmente.
3. Transmitir para a SEFAZ.
4. Processar o retorno:
* **Autorizado:** Persistir protocolo, status e imprimir DANFE.
* **Rejeitado:** Exibir mensagem de erro amigável ao operador e permitir correção ou cancelamento.



### FR-03: Contingência Offline (Crítico)

O sistema deve detectar automaticamente falhas de comunicação ou timeouts com a SEFAZ.

1. **Chaveamento:** Alterar automaticamente o tipo de emissão para "Contingência Offline".
2. **Processamento Local:** Gerar o XML, assinar digitalmente e gerar o QR Code localmente.
3. **Impressão:** Imprimir o DANFE em duas vias com a mensagem obrigatória "EMITIDA EM CONTINGÊNCIA".
4. **Fila de Espera:** Salvar o XML assinado em uma fila de persistência local para transmissão posterior.

### FR-04: Transmissão de Contingência (Recovery Worker)

Um serviço em segundo plano deve monitorar a fila de notas em contingência:

* Verificar periodicamente a conectividade com a SEFAZ.
* Transmitir os XMLs pendentes respeitando a ordem sequencial.
* Atualizar o status da venda de "Contingência Pendente" para "Autorizada".
* Em caso de rejeição tardia (após o cliente sair), marcar a venda para auditoria gerencial (o PDV não deve travar).

### FR-05: Cancelamento de Nota

O sistema deve permitir o cancelamento de uma nota fiscal, respeitando as regras estritas da SEFAZ:

* Validar se a nota foi autorizada.
* Validar o prazo regulamentar (geralmente 30 minutos após autorização).
* Exigir justificativa textual (mínimo de 15 caracteres).
* Transmitir o evento de cancelamento para a SEFAZ e aguardar homologação.
* Só estornar financeiro e estoque após a confirmação do cancelamento fiscal (Protocolo 135).

### FR-06: Impressão (DANFE NFC-e)

Integração com impressoras térmicas via protocolo ESC/POS. O layout deve conter:

* Cabeçalho com dados do emitente.
* Detalhe dos itens.
* Totais e formas de pagamento.
* **QR Code** (legível e dimensionado corretamente).
* Chave de acesso numérica formatada.
* Mensagem de "Consumidor não Identificado" ou dados do CPF/CNPJ quando informado.

---

## 3. Requisitos Não-Funcionais (NFR)

### NFR-01: Performance e UX

* O processo de assinatura e transmissão não deve congelar a interface do usuário (uso obrigatório de chamadas assíncronas).
* O tempo limite (timeout) para tentativa de conexão online deve ser curto (ex: máx 5 segundos) antes de entrar em contingência, para não travar a fila do caixa.

### NFR-02: Segurança

* A senha do certificado digital deve ser armazenada de forma criptografada.
* O certificado digital não deve ser exportável pelo operador do caixa.
* Logs de erro fiscal devem ser detalhados, mas não devem expor dados sensíveis do cliente desnecessariamente.

### NFR-03: Compliance e Auditoria

* **Armazenamento Legal:** O sistema deve garantir a persistência do XML de Distribuição (o XML final com o protocolo de autorização anexado) por, no mínimo, 5 anos (via backup na nuvem na Fase 2).
* **Atomicidade:** A numeração da nota fiscal não pode ter "pulos" injustificados. Se uma numeração for consumida e rejeitada, deve-se avaliar a inutilização da faixa (regra específica de UF).

### NFR-04: Robustez de Dados

* Os dados fiscais (XML, Protocolo, Chave) devem ser armazenados em tabelas separadas das tabelas de Venda operacional para evitar *bloat* (inchaço) nas consultas de relatório gerencial.

---

## 4. Modelo de Dados (Abstrato)

O esquema de banco de dados deve contemplar as seguintes estruturas de informação:

**4.1. Extensão de Produto (Tributário)**

* NCM (Classificação Fiscal).
* CEST (Substituição Tributária).
* CFOP (Código Fiscal de Operações).
* Origem da Mercadoria.
* Alíquotas e Regras de ICMS/PIS/COFINS (CST/CSOSN).

**4.2. Transação Fiscal**
Deve haver um relacionamento 1:N entre Venda e Transações Fiscais, contendo:

* Chave de Acesso (44 dígitos).
* Número e Série da Nota.
* Ambiente de Emissão.
* Status do Retorno (cStat).
* XML de Envio (Request).
* XML de Retorno/Distribuição (Response).
* Indicador de Contingência.

---

## 5. Boas Práticas de Implementação

1. **Validação Prévia (Fail Fast):** Antes de tentar montar o XML, valide se todos os produtos da venda possuem NCM e tributação configurados. Impeça o envio se houver dados cadastrais incompletos.
2. **Mensagens Amigáveis:** Nunca exiba o erro cru da SEFAZ (ex: "Rejeição 704") diretamente ao operador. Traduza para linguagem humana (ex: "Erro: O produto X está com NCM inválido. Corrija o cadastro.").
3. **Gerenciamento de Estado:** A venda deve possuir um status fiscal explícito (`Pendente`, `Autorizado`, `Contingência`, `Cancelado`) independente do status financeiro.
4. **Log de Auditoria:** Grave logs textuais de todas as trocas de mensagens (XMLs) com a SEFAZ em arquivos locais rotacionados por data, para fins de debug e auditoria contábil.
5. **Sanitização de Texto:** Remova acentos e caracteres especiais dos nomes de produtos e clientes antes de gerar o XML, pois a SEFAZ frequentemente rejeita caracteres fora do padrão ASCII/UTF-8 simples.