using SqlSugar;
using System;
using System.Linq;

namespace MySql
{
    public class MySqlController
    {
#if DEBUG
        private const string _connectingStr = "server=localhost;uid=root;pwd=dmk123456;database=dmk";
#else
        //对应服务器配置
        private const string _connectingStr = "server=localhost;uid=root;pwd=dmk123456;database=dmk";
#endif

        private SqlSugarClient _sqlSugarDB = null;

        public SqlSugarClient SqlSugarClient { get { return _sqlSugarDB; } }

        public void Init()
        {
            _sqlSugarDB = new SqlSugarClient(
                new ConnectionConfig()
                {
                    ConnectionString = _connectingStr,
                    DbType = DbType.MySql,
                    IsAutoCloseConnection = true,
                    InitKeyType = InitKeyType.Attribute
                });

#if DEBUG
            //用来打印Sql方便你调式    
            _sqlSugarDB.Aop.OnLogExecuting = (sql, pars) =>
            {
                Console.WriteLine(sql + "\r\n" +
                _sqlSugarDB.Utilities.SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value)));
                Console.WriteLine();
            };
#endif
        }
    }
}
