namespace PDV.Data.Local.Handlers;

using Dapper;
using System.Data;

public class SqliteGuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        // Salva como string no banco (mais legível para debug)
        parameter.Value = value.ToString().ToUpper();
    }

    public override Guid Parse(object value)
    {
        // Lida com o valor vindo do banco
        return value switch
        {
            string s => Guid.Parse(s), // Se vier como texto (seu caso)
            byte[] b => new Guid(b),   // Se vier como bytes (BLOB)
            _ => throw new FormatException($"Não foi possível converter {value.GetType()} para Guid.")
        };
    }
}