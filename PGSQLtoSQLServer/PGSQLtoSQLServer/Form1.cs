using Npgsql;
using PGSQLtoSQLServer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.SqlServer.Management;
using Microsoft.SqlServer.Management.Smo;
namespace PGSQLtoSQLServer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        private void btnConvert_Click(object sender, EventArgs e)
        {

            string constr = String.Format("Server={0};Port={1};" + "User Id={2};Password={3};Database={4};", "localhost", "5432", "postgres", "anka", txtPGSQL.Text);

            NpgsqlConnection cnn = new NpgsqlConnection(constr);
            NpgsqlCommand cmd = new NpgsqlCommand();
            cmd.Connection = cnn;
            cmd.CommandText = "SELECT table_name FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND table_schema = 'public' AND table_name != 'spatial_ref_sys'";
            cnn.Open();

            NpgsqlDataReader rdr = cmd.ExecuteReader();

            List<PostgreTables> tables = new List<PostgreTables>();

            while (rdr.Read())
            {
                PostgreTables tbl = new PostgreTables();
                tbl.Name = rdr["table_name"].ToString();
                tables.Add(tbl);

            }
            rdr.Close();
            DataTable dt = new DataTable();
            List<TableColumns> columns = new List<TableColumns>();
            foreach (var t in tables)
            {

                NpgsqlCommand cmdColumns = new NpgsqlCommand();
                cmdColumns.Connection = cnn;
                cmdColumns.CommandText = "SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME =@table_name";
                cmdColumns.Parameters.AddWithValue("@table_name", t.Name);

                NpgsqlDataReader rdr2 = cmdColumns.ExecuteReader();
                while (rdr2.Read())
                {
                    TableColumns clm = new TableColumns();

                    clm.Name = rdr2["COLUMN_NAME"].ToString();
                    clm.DataType = rdr2["DATA_TYPE"].ToString();
                    if (clm.DataType.Contains("character"))
                    {
                        clm.MaxLength = Convert.ToInt32(rdr2["character_maximum_length"]);
                    }

                    clm.TableName = t.Name;
                    columns.Add(clm);
                }
                rdr2.Close();

            }

            dataGridView1.DataSource = columns;


            Server s = new Server(@".");

            Database d = s.Databases[txtSQLServer.Text];

            List<string> columnName = new List<string>();
            foreach (var t in tables)
            {

                Table tb = new Table(d, t.Name);

                foreach (var c in columns)
                {
                    if (c.TableName == t.Name)
                    {
                        if (c.DataType == "double precision")
                        {

                            Column clm = new Column(tb, c.Name, Microsoft.SqlServer.Management.Smo.DataType.Float);
                            tb.Columns.Add(clm);

                        }
                        else if (c.DataType.Contains("character"))
                        {
                            if (c.MaxLength <= 255)
                            {
                                Column clm = new Column(tb, c.Name, Microsoft.SqlServer.Management.Smo.DataType.VarChar(c.MaxLength));
                                tb.Columns.Add(clm);
                            }
                            else
                            {
                                Column clm = new Column(tb, c.Name, Microsoft.SqlServer.Management.Smo.DataType.Text);
                                tb.Columns.Add(clm);
                            }

                        }
                        else if (c.DataType == "integer")
                        {
                            Column clm = new Column(tb, c.Name, Microsoft.SqlServer.Management.Smo.DataType.Int);
                            tb.Columns.Add(clm);
                        }


                        else if (c.Name == "geom")
                        {
                            Column clm = new Column(tb, c.Name, Microsoft.SqlServer.Management.Smo.DataType.Geometry);
                            tb.Columns.Add(clm);
                        }

                        columnName.Add(c.Name);

                    }

                }

                foreach (var table in tables)
                {
                    //NpgsqlDataAdapter da = new NpgsqlDataAdapter("select gid from  " + table.Name + "", cnn);

                    //DataTable dtbl = new DataTable();

                    //da.Fill(dtbl);

                    //dataGridView2.DataSource = dtbl;
                    
                    foreach (var  column in columns)
                    {
                        if (column.TableName==table.Name & column.Name!="geom")
                        {
                            
                            //datalar sqlservera atılacak
                           
                            //NpgsqlDataAdapter da = new NpgsqlDataAdapter("select " + column.Name + " from " + table.Name + "",cnn);
                     
                            //DataTable dtbl=new DataTable();
                            //da.Fill(dtbl);
                            //dataGridView2.DataSource=dtbl;
                            //while (rdr3.Read())
                            //{
                            //    NpgsqlCommand cmdSql = new NpgsqlCommand("insert into "+table.Name+"("+column.Name+")""  values ()" ");
                            //}

                        }
                        else
                        {
                            //burada srid ve tip dönüşümü latin=>utf8 yapılacak
                        }
                    }
                }
                //if (d.Tables.Contains(tb.Name)) //tablo databasede varsa refresh etsin
                //{
                //    tb.Refresh();
                //}
                //else //yoksa yeni eklesin
                //{
                tb.Create();
                cnn.Close();
                

                foreach (var msg in columnName)
                {
                    lstMessage.Items.Add("Created column " + msg.ToString() + " on " + tb.Name);
                }

                //}

            }
        }

    }
}
