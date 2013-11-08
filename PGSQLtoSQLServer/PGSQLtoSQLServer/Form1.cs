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
            SqlConnection cnnSql = new SqlConnection("server=.;Database=" + txtSQLServer.Text + ";trusted_connection=true");
            cnnSql.Open();

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
            List<TableColumns> clmSql = new List<TableColumns>();
            foreach (var t in tables)
            {

                Table tb = new Table(d, t.Name);
                NpgsqlDataAdapter da = new NpgsqlDataAdapter("select * from " + t.Name + "", cnn);

                DataTable dtb = new DataTable();
                da.Fill(dtb);
                dataGridView2.DataSource = dtb;
                TableColumns cl = new TableColumns();

                foreach (var c in columns)
                {

                    if (c.TableName == t.Name)
                    {

                        SqlConnection con = new SqlConnection("Server=.;Database=" + txtSQLServer.Text + ";trusted_connection=true");
                        con.Open();

                        Column clmn = new Column(tb, c.Name);
                        cl.Name = clmn.Name;
                        cl.TableName = tb.ToString();

                        if (c.DataType == "double precision")
                        {
                            clmn.DataType = Microsoft.SqlServer.Management.Smo.DataType.Float;
                            tb.Columns.Add(clmn);
                            cl.DataType = clmn.DataType.ToString();
                        }
                        else if (c.DataType.Contains("character"))
                        {

                            if (c.MaxLength <= 255)
                            {
                                clmn.DataType = Microsoft.SqlServer.Management.Smo.DataType.VarChar(c.MaxLength);
                            }
                            else
                            {
                                clmn.DataType = Microsoft.SqlServer.Management.Smo.DataType.Text;
                            }
                            tb.Columns.Add(clmn);
                            cl.MaxLength = c.MaxLength;
                            cl.DataType = clmn.DataType.ToString();

                        }
                        else if (c.DataType == "integer")
                        {
                            clmn.DataType = Microsoft.SqlServer.Management.Smo.DataType.Int;
                            tb.Columns.Add(clmn);
                            cl.DataType = clmn.DataType.ToString();

                        }

                        else if (c.Name == "geom")
                        {
                            clmn.DataType = Microsoft.SqlServer.Management.Smo.DataType.Geometry;
                            tb.Columns.Add(clmn);
                            cl.DataType = clmn.DataType.ToString();
                        }


                    }
                    clmSql.Add(cl);
                }

                if (!d.Tables.Contains(tb.Name))
                {
                    tb.Create();
                }
                //MessageBox.Show(dtb.TableName);


                for (int i = 0; i < dtb.Rows.Count; i++)
                {
                    for (int j = 0; j < dtb.Columns.Count; j++)
                    {
                        if (tb.Columns[j].Name != "geom")
                        {
                            SqlCommand cmdData = new SqlCommand("insert into " + t.Name + "(" + dtb.Columns[j].ColumnName + ")  values('" + dtb.Rows[i][j] + "')", cnnSql);
                            cmdData.ExecuteNonQuery();
                        }

                    }

                }


                //DataTable dtbl = new DataTable();
                //foreach (var cSql in clmSql)
                //{
                //    foreach (var cPG in columns)
                //    {

                //        NpgsqlDataAdapter da = new NpgsqlDataAdapter("select " + cPG.Name + " from " + cPG.TableName + "", cnn);
                //        da.Fill(dtbl);
                //        dataGridView2.DataSource = dtbl;

                //    }

                //}
                //DataTable dt2 = new DataTable();
                //foreach (var tbl  in tables)
                //{
                //    foreach (var clm in columns)
                //    {
                //        if (tbl.Name==clm.TableName & clm.Name!="geom")
                //        {
                //            NpgsqlDataAdapter dAdapter = new NpgsqlDataAdapter("select "+clm.Name+" from "+clm.TableName+"",cnn);
                //            dAdapter.Fill(dt2);
                //            dataGridView2.DataSource = dt2;
                //        }
                //    }
                //}

            }



        }


    }


}
