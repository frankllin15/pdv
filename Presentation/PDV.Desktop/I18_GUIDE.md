
A Abordagem "Clássica" (Recomendada para PDV)
Tecnologia: Arquivos .resx + MarkupExtension.

Esta é a melhor opção porque oferece Type Safety. Se você deletar uma chave de string, o código não compila. Em um PDV com centenas de mensagens de erro e labels, isso evita que o cliente veja "lbl_error_01" na tela.

O Fluxo de Arquitetura
Resources: Você cria Resources.resx (padrão/inglês) e Resources.pt-BR.resx.

Gerenciador: Um Singleton que implementa INotifyPropertyChanged para avisar a UI quando a cultura muda.

XAML: Você usa uma extensão customizada, ex: Text="{Localize WelcomeMessage}".

Para um sistema robusto como um PDV, que tende a crescer e ter muitas telas (Venda, Estoque, Fechamento, Configuração, Sincronização), o padrão mais recomendado e amplamente aceito na indústria é o **Upper Snake Case Hierárquico**.

O formato geral é:
`CONTEXTO_TIPO_DESCRICAO`

### Por que esse padrão?

1. **Agrupamento Visual:** No editor de recursos do Visual Studio (ou Rider), as chaves ficam ordenadas alfabeticamente. Com esse padrão, todas as mensagens de "Login" ficam juntas, todas as mensagens de "Erro" ficam juntas, etc.
2. **Auto-complete (IntelliSense):** Quando você digita `Resouces.Login_`, o IDE já filtra apenas o que é relevante para aquela tela.
3. **Contexto:** Evita ambiguidades. A palavra "Cesto" pode ser `Trash` (Lixeira) ou `Basket` (Cesta de compras). Ter `Icon_Trash` e `Icon_Basket` resolve isso.

---

### A Estrutura Recomendada

#### 1. Contexto (Onde?)

Define a área funcional ou módulo da aplicação.

* `Global` ou `Common`: Para itens reutilizáveis em todo o app (Botões Salvar, Cancelar, Sim, Não).
* `Login`: Tela de acesso.
* `PDV`: Tela de venda/caixa.
* `Sync`: Mensagens de sincronização.
* `Estoque`: Gestão de produtos.

#### 2. Tipo (O quê?)

Define o tipo de controle de UI ou a natureza da string. Isso ajuda a saber se você está traduzindo um botão curto ou uma frase longa.

* `Btn`: Botão (Button)
* `Lbl`: Rótulo (Label/TextBlock)
* `Col`: Coluna de Grid (DataGrid Column Header)
* `Msg`: Mensagem (MessageBox, Toast, Log)
* `Menu`: Item de Menu
* `Tit`: Título de Janela ou Seção (Title)
* `Tip`: Tooltip (Dica de interface)

#### 3. Descrição (Qual?)

Uma descrição curta e objetiva da ação ou conteúdo.

---

### Exemplos Práticos para seu PDV

Aqui está como ficaria seu arquivo `.resx`:

#### Elementos Globais (Reutilizáveis)

| Chave (Key) | Valor (Value) |
| --- | --- |
| `Global_Btn_Save` | Salvar |
| `Global_Btn_Cancel` | Cancelar |
| `Global_Btn_Back` | Voltar |
| `Global_Lbl_Loading` | Carregando... |
| `Global_Msg_ConfirmDelete` | Tem certeza que deseja excluir este item? |

#### Tela de Login

| Chave (Key) | Valor (Value) |
| --- | --- |
| `Login_Tit_Window` | Acesso ao Sistema |
| `Login_Lbl_User` | Usuário |
| `Login_Lbl_Password` | Senha |
| `Login_Msg_InvalidCredentials` | Usuário ou senha incorretos. |
| `Login_Btn_RecoverPass` | Esqueci minha senha |

#### Tela de Vendas (PDV)

| Chave (Key) | Valor (Value) |
| --- | --- |
| `PDV_Col_Product` | Produto |
| `PDV_Col_Qty` | Qtd. |
| `PDV_Col_Price` | Preço Unit. |
| `PDV_Btn_Finalize` | Finalizar Venda (F5) |
| `PDV_Lbl_Subtotal` | Subtotal |
| `PDV_Msg_StockWarning` | Estoque baixo para o produto: {0} |

#### Contexto de Sincronização (Seu caso específico)

| Chave (Key) | Valor (Value) |
| --- | --- |
| `Sync_Lbl_Status_Offline` | Operando Offline |
| `Sync_Lbl_Status_Online` | Conectado à Nuvem |
| `Sync_Msg_UploadPending` | Existem {0} vendas pendentes de envio. |
| `Sync_Btn_ForceSync` | Sincronizar Agora |

---

### Dicas de Ouro para Avalonia e .NET

1. **Evite Duplicidade de Significado, não de Palavras:**
* *Ruim:* `Btn_Close` ("Fechar" janela) e `Btn_Close_Cash` ("Fechar" caixa).
* *Bom:* Use `Global_Btn_Close` para janelas e `PDV_Btn_CloseRegister` para o caixa. Mesmo que ambos apareçam como "Fechar" na tela hoje, amanhã você pode querer mudar o do caixa para "Encerrar Expediente".


2. **Formatação de String (Interpolation):**
   Muitas vezes no PDV você precisa injetar dados.
* Chave: `PDV_Msg_ChangeDue`
* Valor: `Troco: {0:C}` (O `:C` formata como moeda automaticamente dependendo da cultura).
* Uso no C#: `string.Format(Resources.PDV_Msg_ChangeDue, valorTroco);`


3. **Use Comentários no RESX:**
   O editor de RESX tem uma coluna "Comment". Use-a!
* Chave: `PDV_Btn_Hold`
* Valor: `Aguardar`
* Comentário: `Botão para colocar a venda em espera/suspenso.` (Isso ajuda muito se você contratar um tradutor ou outro dev).



Esse padrão manterá seu projeto organizado mesmo quando você tiver 2.000 strings cadastradas.