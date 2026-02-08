namespace PDV.Data.Local.Handlers;

using Dapper;
using System.Data;

public class SqliteGuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.Value = value.ToByteArray();
    }

    public override Guid Parse(object value)
    {
        return value switch
        {
            byte[] b => new Guid(b),
            string s => Guid.Parse(s),
            _ => throw new FormatException($"Não foi possível converter {value.GetType()} para Guid.")
        };
    }
}