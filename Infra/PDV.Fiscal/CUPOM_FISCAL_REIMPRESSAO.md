

### 1. Disponibilidade Offline (Local-First)

Como o seu sistema prioriza o funcionamento offline, todos os dados necessários para reconstruir o layout da nota (itens, valores, impostos, chave de acesso e QR Code da NFC-e) devem estar no seu banco **SQLite local**.

* **Atenção:** Não dependa de uma chamada de API ao servidor para recuperar o XML no momento da reimpressão. Se o cliente pedir a via e você estiver sem internet, o sistema precisa resolver localmente.

### 2. Identificação de Reimpressão

Para evitar fraudes ou confusão contábil, é uma boa prática (e por vezes exigência legal) incluir um marcador visual no topo do documento.

* **Ação:** Insira uma tag clara como `** REIMPRESSÃO - 2ª VIA **` no cabeçalho.
* **Log:** Grave no banco local quem solicitou a reimpressão e o horário, para fins de auditoria.

### 3. Integridade do Layout

O DANFE (Documento Auxiliar da Nota Fiscal Eletrônica) reimpresso deve ser identico ao original.

* **Dica:** Utilize um motor de relatórios ou um template de impressão que receba o objeto da venda e gere o ESC/POS (linguagem das térmicas) de forma padronizada, garantindo que o QR Code seja legível e aponte para a URL correta da SEFAZ.

### 4. Gestão de Contingência

Lembre-se que notas emitidas em regime de **contingência** (quando não há internet no momento da venda) podem ter sua chave de acesso gerada, mas ainda não constarem como "Autorizadas" na SEFAZ.

* Ao reimprimir uma nota emitida em contingência, o documento deve obrigatoriamente exibir a mensagem de que aquela nota foi emitida offline.

---

Você quer que eu ajude a estruturar a tabela de **Histórico de Vendas** no SQLite para garantir que nenhum dado essencial da impressão seja perdido durante a sincronização?