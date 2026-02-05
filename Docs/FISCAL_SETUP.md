# Configuracao do Modulo Fiscal (NFC-e)

Este guia explica como configurar e testar o modulo fiscal do PDV para emissao de NFC-e.

## Pre-requisitos

- PDV Desktop compilado e funcionando
- Banco de dados local inicializado (acontece automaticamente no primeiro uso)

## Passo 1: Acessar a Configuracao Fiscal

1. Inicie o PDV Desktop:
   ```bash
   dotnet run --project Presentation/PDV.Desktop/PDV.Desktop.csproj
   ```

2. Faca login com as credenciais padrao:
   - Usuario: `ADMIN`
   - Senha: `1234`

3. No menu superior, clique em **"Fiscal"**

## Passo 2: Preencher Dados da Empresa

### Dados Obrigatorios

| Campo | Descricao | Exemplo |
|-------|-----------|---------|
| CNPJ | CNPJ da empresa (14 digitos) | `12345678000199` |
| Razao Social | Nome juridico completo | `EMPRESA TESTE LTDA` |
| Nome Fantasia | Nome comercial | `LOJA TESTE` |
| UF | Estado (2 letras) | `SP` |
| Inscricao Estadual | IE da empresa | `123456789012` |

### Endereco

| Campo | Descricao | Exemplo |
|-------|-----------|---------|
| Logradouro | Rua/Avenida | `Rua das Flores` |
| Numero | Numero do estabelecimento | `123` |
| Bairro | Bairro (opcional) | `Centro` |
| CEP | Codigo postal (8 digitos) | `01234567` |
| Codigo IBGE | Codigo do municipio (7 digitos) | `3550308` (Sao Paulo) |

> **Dica**: Consulte o codigo IBGE do seu municipio em: https://www.ibge.gov.br/explica/codigos-dos-municipios.php

### Parametros NFC-e

| Campo | Descricao | Valor Inicial |
|-------|-----------|---------------|
| Serie | Serie da NFC-e | `1` |
| Proximo Numero | Proximo numero (somente leitura) | `1` |
| Ambiente | Homologacao (teste) ou Producao | `Homologacao` |
| CSC ID | Identificador do CSC | Fornecido pela SEFAZ |
| CSC Token | Token de seguranca | Fornecido pela SEFAZ |
| Certificado Digital | Caminho do arquivo .pfx | `C:\certificados\cert.pfx` |
| Senha do Certificado | Senha do certificado A1 | `****` |

## Passo 3: Salvar Configuracao

1. Apos preencher todos os campos obrigatorios, clique em **"Salvar Configuracao"**
2. Verifique a mensagem de status na barra inferior:
   - Verde: "Configuracao salva com sucesso!"
   - Vermelho: Mensagem de erro indicando campo invalido

## Passo 4: Testar Conexao (Opcional)

1. Clique em **"Testar Conexao SEFAZ"**
2. Aguarde o resultado:
   - "Conexao com SEFAZ OK!" - Servico disponivel
   - "SEFAZ indisponivel no momento" - Servico offline (NFC-e sera emitida em contingencia)

> **Nota**: Em ambiente de desenvolvimento, o sistema usa um MockProvider que sempre retorna sucesso.

## Passo 5: Testar Emissao de NFC-e

### Realizar uma Venda

1. Va para **"Checkout (F1)"**
2. Adicione produtos a venda:
   - Digite o codigo de barras e pressione Enter, ou
   - Use a busca por descricao
3. Clique em **"Pagamento"**
4. Selecione a forma de pagamento (Dinheiro, Cartao, PIX)
5. Confirme o pagamento

### Verificar Emissao

Apos a conclusao da venda, observe a barra de status:

- **"NFC-e XXXXXX autorizada"** - Emissao bem-sucedida
- **"NFC-e XXXXXX em contingencia"** - SEFAZ indisponivel, armazenada para transmissao posterior
- **"Erro fiscal: ..."** - Falha na emissao (verifique a configuracao)

## Configuracao para Testes (MockProvider)

O sistema vem configurado com um `MockProvider` que simula a SEFAZ para testes. Este provider:

- Sempre autoriza NFC-e (exceto se configurado para simular falhas)
- Gera chaves de acesso e protocolos ficticios
- Nao requer certificado digital real
- Nao transmite dados para nenhum servidor

### Dados de Teste Sugeridos

```
CNPJ: 12345678000199
Razao Social: EMPRESA TESTE DESENVOLVIMENTO LTDA
Nome Fantasia: LOJA TESTE DEV
UF: SP
Inscricao Estadual: 123456789012
Logradouro: Rua de Teste
Numero: 100
Bairro: Centro
CEP: 01310100
Codigo IBGE: 3550308
Serie: 1
Ambiente: Homologacao
CSC ID: 1
CSC Token: ABCD1234567890
```

## Verificar Transacoes Fiscais

As transacoes fiscais sao armazenadas na tabela `FiscalTransactions` do banco SQLite local.

Localizacao do banco:
```
%LOCALAPPDATA%\PDV\pdv_local.db
```

Voce pode usar ferramentas como DB Browser for SQLite para inspecionar:
- Chave de acesso gerada
- Status da transmissao
- XML de requisicao/resposta
- Protocolo de autorizacao

## Troubleshooting

### "Configuracao fiscal nao encontrada"

A configuracao fiscal nao foi salva. Acesse a tela Fiscal e preencha os dados.

### "CNPJ deve ter 14 digitos"

Remova pontos, barras e tracos do CNPJ. Use apenas numeros.

### "Codigo IBGE deve ter 7 digitos"

Consulte o codigo correto do seu municipio no site do IBGE.

### NFC-e sempre em contingencia

O MockProvider pode estar configurado para simular indisponibilidade. Em producao, verifique:
- Conexao com internet
- Certificado digital valido
- Configuracao correta da URL da SEFAZ

## Proximos Passos (Producao)

Para usar em producao, sera necessario:

1. Substituir o `MockProvider` por um provider real (ex: `SefazProvider`)
2. Obter certificado digital A1 da empresa
3. Solicitar CSC junto a SEFAZ do estado
4. Configurar ambiente como "Producao"
5. Implementar impressao do DANFE NFC-e

## Estrutura dos Arquivos

```
Infra/PDV.Fiscal/
├── Providers/
│   ├── IFiscalProvider.cs    # Interface do provider
│   └── MockProvider.cs       # Provider de testes
├── Services/
│   ├── FiscalManager.cs      # Orquestrador principal
│   ├── XmlBuilderService.cs  # Gera XML da NFC-e
│   └── DanfeService.cs       # Gera DANFE texto
├── Utilities/
│   ├── AccessKeyGenerator.cs # Gera chave de acesso
│   ├── QrCodeGenerator.cs    # Gera URL do QR Code
│   └── DocumentValidator.cs  # Valida CPF/CNPJ
└── Exceptions/
    └── FiscalException.cs    # Excecoes fiscais
```
