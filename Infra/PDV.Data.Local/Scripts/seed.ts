import { Database } from "bun:sqlite";

// 1. Configurações Iniciais
const DB_PATH = "C:\\Users\\frank\\AppData\\Local\\PDV\\pdv_local.db"; // <-- Caminho do seu SQLite
const args = Bun.argv;
const SALES_TO_GENERATE = args.length > 2 ? parseInt(args[2]) : 50; // Padrão: 50 vendas

const db = new Database(DB_PATH);

console.log(`\n🚀 Iniciando geração de ${SALES_TO_GENERATE} vendas simuladas...`);

// ==========================================
// HELPERS DE UUID (.NET BLOB)
// ==========================================

// Converte UUID (string) para o formato BLOB (16 bytes) com o Endianness do Guid do C# (.NET)
const uuidToDotNetBlob = (uuid: string): Buffer => {
    const hex = uuid.replace(/-/g, '');
    const bytes = new Uint8Array(16);

    // Bloco 1 (Int32) - Little Endian (Invertido)
    bytes[0] = parseInt(hex.slice(6, 8), 16);
    bytes[1] = parseInt(hex.slice(4, 6), 16);
    bytes[2] = parseInt(hex.slice(2, 4), 16);
    bytes[3] = parseInt(hex.slice(0, 2), 16);

    // Bloco 2 (Int16) - Little Endian (Invertido)
    bytes[4] = parseInt(hex.slice(10, 12), 16);
    bytes[5] = parseInt(hex.slice(8, 10), 16);

    // Bloco 3 (Int16) - Little Endian (Invertido)
    bytes[6] = parseInt(hex.slice(14, 16), 16);
    bytes[7] = parseInt(hex.slice(12, 14), 16);

    // Bloco 4 (Byte[8]) - Big Endian (Sequencial)
    for (let i = 0; i < 8; i++) {
        bytes[8 + i] = parseInt(hex.slice(16 + (i * 2), 18 + (i * 2)), 16);
    }

    return Buffer.from(bytes);
};

// Gera um novo UUID e já retorna como Blob
const newGuidBlob = () => uuidToDotNetBlob(crypto.randomUUID());

// ==========================================
// HELPERS DE DATA E NÚMEROS
// ==========================================

// Formata data JS para o formato .NET/SQLite (YYYY-MM-DD HH:MM:SS.fffffff)
const formatToDotNetDate = (d: Date) => {
    const pad = (n: number, len = 2) => String(n).padStart(len, '0');

    const datePart = `${d.getUTCFullYear()}-${pad(d.getUTCMonth() + 1)}-${pad(d.getUTCDate())}`;
    const timePart = `${pad(d.getUTCHours())}:${pad(d.getUTCMinutes())}:${pad(d.getUTCSeconds())}`;

    // Adicionamos 4 dígitos aleatórios aos milissegundos para simular os "ticks" do .NET
    const ms = pad(d.getUTCMilliseconds(), 3);
    const extraTicks = Math.floor(Math.random() * 10000).toString().padStart(4, '0');

    return `${datePart} ${timePart}.${ms}${extraTicks}`;
};

// Gera uma data aleatória no passado, limitando-se estritamente ao momento atual
const getRandomDateUpToNow = (daysBack = 180) => {
    const now = new Date();
    const past = new Date();
    past.setDate(now.getDate() - daysBack);

    return new Date(past.getTime() + Math.random() * (now.getTime() - past.getTime()));
};

// Evita dízimas periódicas no JS
const round2 = (num: number) => Math.round(num * 100) / 100;

// ==========================================
// 2. BASE DE DADOS FICTÍCIA
// ==========================================
const defaultOperators = [
    { Id: newGuidBlob(), Name: "Administrador", Code: "001", PinHash: "hash123", IsAdmin: 1 },
    { Id: newGuidBlob(), Name: "Caixa Padrão", Code: "002", PinHash: "hash123", IsAdmin: 0 }
];

const defaultProducts = [
    { Id: newGuidBlob(), Barcode: "789100001", Description: "Refrigerante Cola 2L", Price: 8.50, Stock: 150 },
    { Id: newGuidBlob(), Barcode: "789100002", Description: "Cerveja Pilsen Lata 350ml", Price: 4.00, Stock: 300 },
    { Id: newGuidBlob(), Barcode: "789100003", Description: "Pão de Forma Tradicional", Price: 9.90, Stock: 50 },
    { Id: newGuidBlob(), Barcode: "789100004", Description: "Café Torrado 500g", Price: 18.50, Stock: 100 },
    { Id: newGuidBlob(), Barcode: "789100005", Description: "Arroz Branco 5kg", Price: 25.00, Stock: 80 }
];

// ==========================================
// 3. PREPARAÇÃO DAS QUERIES
// ==========================================
const insertOperator = db.prepare(`INSERT OR IGNORE INTO Operators (Id, Name, Code, PinHash, IsActive, IsAdmin, CreatedAt) VALUES ($Id, $Name, $Code, $PinHash, 1, $IsAdmin, $CreatedAt)`);
const insertProduct = db.prepare(`INSERT OR IGNORE INTO Products (Id, Barcode, Description, UnitPrice, UnitOfMeasure, StockQuantity, TaxOrigin, SyncState, IsActive, CreatedAt) VALUES ($Id, $Barcode, $Description, $Price, 'UN', $Stock, 0, 0, 1, $CreatedAt)`);
const insertSession = db.prepare(`INSERT INTO CashSessions (Id, OperatorId, TerminalId, OpenedAt, OpeningBalance, ClosingBalance, CalculatedBalance, Status, SyncState, CreatedAt) VALUES ($Id, $OpId, $Terminal, $OpenedAt, 150.00, 0, 0, 1, 0, $CreatedAt)`);
const insertSale = db.prepare(`INSERT INTO Sales (Id, SaleNumber, SaleDate, Subtotal, Discount, Total, Change, Status, OperatorId, CashSessionId, SyncState, FiscalStatus, CreatedAt) VALUES ($Id, $Number, $Date, $Subtotal, $Discount, $Total, $Change, 2, $OpId, $SessionId, 0, 0, $CreatedAt)`);
const insertSaleItem = db.prepare(`INSERT INTO SaleItems (Id, SaleId, ProductId, Barcode, ProductDescription, Quantity, UnitPrice, Discount, Total, CreatedAt) VALUES ($Id, $SaleId, $ProdId, $Barcode, $Desc, $Qty, $Price, $Discount, $Total, $CreatedAt)`);
const insertPayment = db.prepare(`INSERT INTO Payments (Id, SaleId, Method, Amount, PaymentDate, CreatedAt) VALUES ($Id, $SaleId, $Method, $Amount, $Date, $CreatedAt)`);

// ==========================================
// 4. EXECUÇÃO EM TRANSAÇÃO
// ==========================================
const seedData = db.transaction(() => {
    // Sorteia a data inicial da Sessão (turno de trabalho)
    const sessionDate = getRandomDateUpToNow(180); // Últimos 6 meses
    const sessionTimestampStr = formatToDotNetDate(sessionDate);

    // Populando Entidades Base
    for (const op of defaultOperators) {
        insertOperator.run({ $Id: op.Id, $Name: op.Name, $Code: op.Code, $PinHash: op.PinHash, $IsAdmin: op.IsAdmin, $CreatedAt: sessionTimestampStr });
    }

    for (const prod of defaultProducts) {
        insertProduct.run({ $Id: prod.Id, $Barcode: prod.Barcode, $Description: prod.Description, $Price: prod.Price, $Stock: prod.Stock, $CreatedAt: sessionTimestampStr });
    }

    // Buscando Operadores e Produtos (pegamos os IDs do banco em formato Buffer)
    const operators = db.query("SELECT Id FROM Operators").all() as { Id: Buffer }[];
    const products = db.query("SELECT Id, Barcode, Description, UnitPrice FROM Products").all() as { Id: Buffer, Barcode: string, Description: string, UnitPrice: number }[];

    // Pegando último número de venda
    const lastSale = db.query("SELECT MAX(SaleNumber) as MaxNum FROM Sales").get() as { MaxNum: number | null };
    let currentSaleNumber = (lastSale?.MaxNum || 0) + 1;

    // Criando Sessão de Caixa
    const sessionUuidStr = crypto.randomUUID(); // Guardamos a string apenas para o log
    const sessionId = uuidToDotNetBlob(sessionUuidStr);
    const activeOpId = operators[0]?.Id;
    
    if (!activeOpId) {
        throw new Error("Nenhum operador encontrado no banco. Verifique se os operadores foram inseridos corretamente.");
    }
    
    insertSession.run({ $Id: sessionId, $OpId: activeOpId, $Terminal: "CX-01", $OpenedAt: sessionTimestampStr, $CreatedAt: sessionTimestampStr });

    let sessionTotal = 0;

    // Clonamos a data da sessão para irmos incrementando o horário de cada venda cronologicamente
    let currentSaleDate = new Date(sessionDate.getTime());

    // Gerando Vendas
    for (let i = 0; i < SALES_TO_GENERATE; i++) {
        // Avança o relógio entre 1 e 12 minutos para a próxima venda
        currentSaleDate.setMinutes(currentSaleDate.getMinutes() + Math.floor(Math.random() * 12) + 1);

        // Trava de segurança: impede que a data/hora da venda passe da data/hora atual do sistema
        const nowReal = new Date();
        if (currentSaleDate > nowReal) {
            currentSaleDate = nowReal;
        }

        const saleTimestampStr = formatToDotNetDate(currentSaleDate);
        const saleId = newGuidBlob();
        const itemsCount = Math.floor(Math.random() * 5) + 1; // 1 a 5 itens por venda

        let subtotal = 0;

        // Gerando Itens da Venda
        for (let j = 0; j < itemsCount; j++) {
            const prod = products[Math.floor(Math.random() * products.length)];
            
            if (!prod) continue; // Segurança caso não haja produtos
            
            const qty = Math.floor(Math.random() * 3) + 1;
            const itemTotal = round2(prod.UnitPrice * qty);
            subtotal += itemTotal;

            insertSaleItem.run({
                $Id: newGuidBlob(), $SaleId: saleId, $ProdId: prod.Id,
                $Barcode: prod.Barcode, $Desc: prod.Description, $Qty: qty,
                $Price: prod.UnitPrice, $Discount: 0, $Total: itemTotal, $CreatedAt: saleTimestampStr
            });
        }

        const saleDiscount = Math.random() > 0.8 ? round2(subtotal * 0.05) : 0; // 20% de chance de ter 5% de desconto
        const total = round2(subtotal - saleDiscount);
        sessionTotal += total;

        // Inserindo a Venda
        insertSale.run({
            $Id: saleId, $Number: currentSaleNumber++, $Date: saleTimestampStr,
            $Subtotal: subtotal, $Discount: saleDiscount, $Total: total,
            $Change: 0, $OpId: activeOpId, $SessionId: sessionId, $CreatedAt: saleTimestampStr
        });

        // Inserindo Pagamento (Método Aleatório: 1-Dinheiro, 2-Crédito, 3-Débito, 4-Pix)
        const method = Math.floor(Math.random() * 4) + 1;
        insertPayment.run({
            $Id: newGuidBlob(), $SaleId: saleId, $Method: method,
            $Amount: total, $Date: saleTimestampStr, $CreatedAt: saleTimestampStr
        });
    }

    // Atualizando o saldo calculado da Sessão
    db.run(`UPDATE CashSessions SET CalculatedBalance = OpeningBalance + $Total WHERE Id = $Id`, {
        $Total: sessionTotal,
        $Id: sessionId
    });

    console.log(`✅ Turno simulado criado! Data/Hora Base: ${sessionDate.toLocaleString()}`);
    console.log(`🆔 Id da Sessão (C# Guid): ${sessionUuidStr}`);
    console.log(`💰 Faturamento do turno: R$ ${sessionTotal.toFixed(2)}`);
});

// Executa a transação
try {
    seedData();
    console.log("🎉 Script finalizado.");
} catch (err) {
    console.error("❌ Erro ao popular banco:", err);
} finally {
    db.close();
}