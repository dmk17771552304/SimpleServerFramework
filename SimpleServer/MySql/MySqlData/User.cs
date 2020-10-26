using SqlSugar;
using System;

namespace MySql.MySqlData
{
    [SugarTable("user")]
    public class User
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime Logindate { get; set; }
        public string Logintype { get; set; }
        public string Token { get; set; }
    }
}
