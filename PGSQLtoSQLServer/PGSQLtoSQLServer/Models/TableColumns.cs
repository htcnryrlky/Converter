using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PGSQLtoSQLServer.Models
{
    public class TableColumns
    {
        public string Name { get; set; }

        public string DataType { get; set; }

        public string TableName { get; set; }

        public int MaxLength { get; set; }

    }
}
