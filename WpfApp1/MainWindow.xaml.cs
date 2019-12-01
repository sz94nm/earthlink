using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;
using Elasticsearch.Net;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        public static SqlConnection dbConnection()
        {
            string connectionString = Properties.Settings.Default.connection_String;
            SqlConnection dbConnection = new SqlConnection(connectionString);
            if (dbConnection.State != ConnectionState.Open)
            {
                dbConnection.Open();
            }
            return dbConnection;
        }


        private void ProcessDataTable(DataTable dt)
        {
            foreach (DataRow row in dt.Rows)
            {
                if (!HasExactMatch(row))
                {
                    DataTable possibleMatches = CheckPossibleMatch(row);
                    if (possibleMatches.Rows.Count == 0)
                    {
                        InsertRowToDB(row);
                    }
                    else
                    {

                    }

                }




            }

        }


        //check for exact duplicates here------------------------------------------------------
        private bool HasExactMatch(DataRow row)
        {
            SqlConnection con = dbConnection();
            bool hasExactMatch = false;
            string exactMatchQuery = "SELECT * FROM customers_data WHERE " +
                "(name='" + row[0].ToString() + "') and (email='" + row[1].ToString() +
                "')and( phone1='" + row[2].ToString() + "')and( phone2 ='" + row[3].ToString() + "')";
            using (SqlCommand cmd = new SqlCommand(exactMatchQuery, con))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        hasExactMatch = true;
                    }
                }
            }



            con.Close();
            return hasExactMatch;
        }


        //check for possible duplicates here---------------------------------------------------
        private DataTable CheckPossibleMatch(DataRow row)
        {
            DataTable dataTable = new DataTable();
            SqlConnection con = dbConnection();
            //----------------------------------------------------------------------------------------
            string possibleMatchQuery = "SELECT * FROM customers_data WHERE " +
                "(name='" + row[0].ToString() + "' and email='" + row[1].ToString() + "')" +
                "or( phone1='" + row[2].ToString() + "'and email='" + row[1].ToString() + "')" +
                "or( phone2='" + row[3].ToString() + "'and email='" + row[1].ToString() + "')";

            using (SqlCommand cmd = new SqlCommand(possibleMatchQuery, con))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        dataTable.Load(reader);
                    }
                }
            }
            //----------------------------------------------------------------------------------------
            con.Close();
            return dataTable;
        }

        //inserts datarow  to database---------------------------------------------------------
        private void InsertRowToDB(DataRow row)
        {
            SqlConnection con = dbConnection();
            string queryString = "insert into customers_data (name,email,phone1,phone2) " +
                    "values('" + row[0].ToString() + "','" + row[1].ToString() + "','"
                    + row[2].ToString() + "','" + row[3].ToString() + "')";

            using (SqlCommand cmd = new SqlCommand(queryString, con))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            //do something about it maybe?  ... Nah its cool.
                        }
                    }

                }
            }
            con.Close();
        }
        public MainWindow()
        {
            InitializeComponent();
            string sss = "SELECT * FROM customers_data";
            SqlCommand cmd = new SqlCommand(sss, dbConnection());
            SqlDataReader queryCommandReader = cmd.ExecuteReader();
            DataTable dataTable = new DataTable();
            dataTable.Load(queryCommandReader);

            String columns = string.Empty;
            foreach (DataColumn column in dataTable.Columns)
            {
                columns += column.ColumnName + " | ";
            }
            Console.WriteLine(columns);
            
            String rowText = string.Empty;
            foreach (DataColumn column in dataTable.Columns)
            {
                Console.Write(dataTable.Rows[0][column.ColumnName]);
            }
            Console.WriteLine(rowText);
           
            // Close the connection
            dbConnection().Close();
            

        }

        //browse button code-------------------------------------------------------------------

        private void browseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".csv";
            dlg.Filter = "Comma Separated (*.csv)|*.csv";
            dlg.ShowDialog();
            directoryTextBox.Text = dlg.FileName;
        }
        


        //import button code-------------------------------------------------------------------
        private void importButton_Click(object sender, RoutedEventArgs e)
        {
           DataTable importedData = ConvertCSVtoDataTable();
           ProcessDataTable(importedData);
        }
        

       


        
        
        
        //converts csv file to datatable-------------------------------------------------------
        private  DataTable ConvertCSVtoDataTable()
        {
            DataTable dt = new DataTable();
            if (directoryTextBox.Text.Length > 0)
            {


                StreamReader sr = new StreamReader(directoryTextBox.Text);
                string[] headers = sr.ReadLine().Split(',');

                foreach (string header in headers)
                {
                    dt.Columns.Add(header);
                }
                while (!sr.EndOfStream)
                {
                    string[] rows = Regex.Split(sr.ReadLine(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = rows[i];
                    }
                    dt.Rows.Add(dr);
                }
            }
                return dt;
        }

        private void submitButton_Click(object sender, RoutedEventArgs e)
        {
            if(nameTextBox.Text.Length>0&&
                emailTextBox.Text.Length > 0&&
                phone1TextBox.Text.Length > 0&&
                phone2TextBox.Text.Length > 0)
            {
                dbConnection();
                DataTable dt = new DataTable();
                dt.Columns.Add("name");
                dt.Columns.Add("email");
                dt.Columns.Add("phone1");
                dt.Columns.Add("phone2");
                DataRow row = dt.NewRow();
                row[0] = nameTextBox.Text.ToString();
                row[1] = emailTextBox.Text.ToString();
                row[2] = phone1TextBox.Text.ToString();
                row[3] = phone2TextBox.Text.ToString();
                DataTable dt2 = CheckPossibleMatch(row);
                if (HasExactMatch(row))
                {
                    MessageBox.Show("Data already exists", "Alert");
                }
                else if (dt2.Rows.Count > 0)
                {
                   /* String columns = string.Empty;
                    foreach (DataColumn column in dt2.Columns)
                    {
                        columns += column.ColumnName + " | ";
                    }
                    listView1.Items.Add(columns);
                    int i = 0;
                    foreach (Data column in dt2.Columns)
                    {
                        columns=dt2.Rows[i][column.ColumnName].ToString() + " | "+
                            dt2.Rows[1][column.ColumnName].ToString() + " | " +
                            dt2.Rows[2][column.ColumnName].ToString() + " | " +
                            dt2.Rows[3][column.ColumnName].ToString() + " | " ;
                        
                    }*/
                    listView1.ItemsSource = dt2.DefaultView;

                }
                else
                {
                    InsertRowToDB(row);
                }

            }
            else
            {
                MessageBox.Show("Fields must be filled", "Alert");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (listView1.Items.Count > 0 &&
                nameTextBox.Text.Length > 0 &&
                emailTextBox.Text.Length > 0 &&
                phone1TextBox.Text.Length > 0 &&
                phone2TextBox.Text.Length > 0)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("name");
                dt.Columns.Add("email");
                dt.Columns.Add("phone1");
                dt.Columns.Add("phone2");
                DataRow row = dt.NewRow();
                row[0] = nameTextBox.Text.ToString();
                row[1] = emailTextBox.Text.ToString();
                row[2] = phone1TextBox.Text.ToString();
                row[3] = phone2TextBox.Text.ToString();
                InsertRowToDB(row);
                clearStuff();
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {

            clearStuff();
            
        }
        private void clearStuff()
        {
            nameTextBox.Text = "";
            emailTextBox.Text = "";
            phone1TextBox.Text = "";
            phone2TextBox.Text = "";
            listView1.Items.Clear();
        }
            
    }
}
