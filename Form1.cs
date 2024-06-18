using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace ProyectoFinal4
{
    public partial class Form1 : Form
    {
        private string connectionString = "Server=DESKTOP-UBTP0PR;Database=Tenis;Integrated Security=True;";
        private SqlConnection connection;
        public Form1()
        {
            InitializeComponent();
            DisplayData();
        }
        private void OpenConnection()
        {
            if (connection == null)
                connection = new SqlConnection(connectionString);

            if (connection.State == ConnectionState.Closed)
                connection.Open();
        }

        private void CloseConnection()
        {
            if (connection != null && connection.State == ConnectionState.Open)
                connection.Close();
        }

        private DataTable LoadData()
        {
            OpenConnection();
            string query = "SELECT Id, Modelo, Precio, FechaCreacion, Cantidad, Descripcion FROM InventarioTenis";
            SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);
            CloseConnection();
            return dataTable;
        }

        private void DisplayData()
        {
            dataGridView.DataSource = LoadData();
        }

        private void AddData(string modelo, decimal precio, DateTime fechaCreacion, int cantidad, string descripcion)
        {
            OpenConnection();
            string query = "INSERT INTO InventarioTenis (Modelo, Precio, FechaCreacion, Cantidad, Descripcion) " +
                           "VALUES (@Modelo, @Precio, @FechaCreacion, @Cantidad, @Descripcion)";
            SqlCommand cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Modelo", modelo);
            cmd.Parameters.AddWithValue("@Precio", precio);
            cmd.Parameters.AddWithValue("@FechaCreacion", fechaCreacion);
            cmd.Parameters.AddWithValue("@Cantidad", cantidad);
            cmd.Parameters.AddWithValue("@Descripcion", descripcion);
            cmd.ExecuteNonQuery();
            CloseConnection();
            DisplayData();
        }

        private void UpdateData(int id, string modelo, decimal precio, DateTime fechaCreacion, int cantidad, string descripcion)
        {
            OpenConnection();
            string query = "UPDATE InventarioTenis SET Modelo = @Modelo, Precio = @Precio, FechaCreacion = @FechaCreacion, " +
                           "Cantidad = @Cantidad, Descripcion = @Descripcion WHERE Id = @Id";
            SqlCommand cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Modelo", modelo);
            cmd.Parameters.AddWithValue("@Precio", precio);
            cmd.Parameters.AddWithValue("@FechaCreacion", fechaCreacion);
            cmd.Parameters.AddWithValue("@Cantidad", cantidad);
            cmd.Parameters.AddWithValue("@Descripcion", descripcion);
            cmd.ExecuteNonQuery();
            CloseConnection();
            DisplayData();
        }

        private void DeleteData(int id)
        {
            OpenConnection();
            string query = "DELETE FROM InventarioTenis WHERE Id = @Id";
            SqlCommand cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
            CloseConnection();
            DisplayData();
        }

        private void ImportFromCSV(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines.Skip(1))
            {
                var values = line.Split(',');

                if (values.Length < 5)
                {
                    MessageBox.Show("Formato de línea incorrecto: " + line);
                    continue;
                }

                string modelo = values[0];

                if (!decimal.TryParse(values[1], out decimal precio))
                {
                    MessageBox.Show($"Error al convertir '{values[1]}' a decimal para Precio.");
                    continue;
                }

                // Intenta analizar la fecha utilizando DateTime.TryParse o DateTime.TryParseExact
                if (!DateTime.TryParse(values[2], out DateTime fechaCreacion))
                {
                    // Puedes probar con diferentes formatos de fecha si es necesario
                    string[] formats = { "MM/dd/yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "MM-dd-yyyy" };
                    if (!DateTime.TryParseExact(values[2], formats, null, System.Globalization.DateTimeStyles.None, out fechaCreacion))
                    {
                        MessageBox.Show($"Error al convertir '{values[2]}' a DateTime para FechaCreacion.");
                        continue;
                    }
                }

                if (!int.TryParse(values[3], out int cantidad))
                {
                    MessageBox.Show($"Error al convertir '{values[3]}' a int para Cantidad.");
                    continue;
                }

                string descripcion = values[4];

                AddData(modelo, precio, fechaCreacion, cantidad, descripcion);
            }
        }



        private void ImportFromJSON(string filePath)
        {
            var jsonData = File.ReadAllText(filePath);
            var dataList = JsonConvert.DeserializeObject<YourDataModel[]>(jsonData);
            foreach (var data in dataList)
            {
                AddData(data.Modelo, data.Precio, data.FechaCreacion, data.Cantidad, data.Descripcion);
            }
        }

        private void ImportFromXML(string filePath)
        {
            var xmlData = XDocument.Load(filePath);
            var dataList = xmlData.Descendants("YourDataModel").Select(x => new YourDataModel
            {
                Modelo = x.Element("Modelo").Value,
                Precio = Convert.ToDecimal(x.Element("Precio").Value),
                FechaCreacion = Convert.ToDateTime(x.Element("FechaCreacion").Value),
                Cantidad = Convert.ToInt32(x.Element("Cantidad").Value),
                Descripcion = x.Element("Descripcion").Value
            }).ToList();

            foreach (var data in dataList)
            {
                AddData(data.Modelo, data.Precio, data.FechaCreacion, data.Cantidad, data.Descripcion);
            }
        }

        private void ExportToCSV(string filePath)
        {
            var dataTable = LoadData();
            var lines = new string[dataTable.Rows.Count + 1];
            var columnNames = string.Join(",", dataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName));
            lines[0] = columnNames;
            var index = 1;
            foreach (DataRow row in dataTable.Rows)
            {
                var values = string.Join(",", row.ItemArray.Select(field => field.ToString()));
                lines[index] = values;
                index++;
            }
            File.WriteAllLines(filePath, lines);
        }

        private void ExportToJSON(string filePath)
        {
            var dataTable = LoadData();
            var jsonData = JsonConvert.SerializeObject(dataTable, Formatting.Indented);
            File.WriteAllText(filePath, jsonData);
        }

        private void ExportToXML(string filePath)
        {
            var dataTable = LoadData();
            var xmlData = new XDocument(
                new XElement("InventarioTenis",
                    from row in dataTable.AsEnumerable()
                    select new XElement("YourDataModel",
                        new XElement("Modelo", row.Field<string>("Modelo")),
                        new XElement("Precio", row.Field<decimal>("Precio")),
                        new XElement("FechaCreacion", row.Field<DateTime>("FechaCreacion")),
                        new XElement("Cantidad", row.Field<int>("Cantidad")),
                        new XElement("Descripcion", row.Field<string>("Descripcion"))
                    )
                )
            );
            xmlData.Save(filePath);
        }

        private void btnAdd_Click_1(object sender, EventArgs e)
        {
            string modelo = txtModelo.Text;
            decimal precio = Convert.ToDecimal(txtPrecio.Text);
            DateTime fechaCreacion = dtFechaCreacion.Value;
            int cantidad = Convert.ToInt32(txtCantidad.Text);
            string descripcion = txtDescripcion.Text;
            AddData(modelo, precio, fechaCreacion, cantidad, descripcion);
        }

        private void btnUpdate_Click_1(object sender, EventArgs e)
        {
            int id = Convert.ToInt32(txtId.Text);
            string modelo = txtModelo.Text;
            decimal precio = Convert.ToDecimal(txtPrecio.Text);
            DateTime fechaCreacion = dtFechaCreacion.Value;
            int cantidad = Convert.ToInt32(txtCantidad.Text);
            string descripcion = txtDescripcion.Text;
            UpdateData(id, modelo, precio, fechaCreacion, cantidad, descripcion);
        }

        private void btnDelete_Click_1(object sender, EventArgs e)
        {
            int id = Convert.ToInt32(txtId.Text);
            DeleteData(id);
        }

        private void btnImportCSV_Click_1(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ImportFromCSV(openFileDialog.FileName);
            }
        }

        private void btnExportCSV_Click_1(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                ExportToCSV(saveFileDialog.FileName);
            }
        }

        private void btnImportJSON_Click_1(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ImportFromJSON(openFileDialog.FileName);
            }
        }

        private void btnExportJSON_Click_1(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                ExportToJSON(saveFileDialog.FileName);
            }
        }

        private void btnImportXML_Click_1(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ImportFromXML(openFileDialog.FileName);
            }
        }

        private void btnExportXML_Click_1(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                ExportToXML(saveFileDialog.FileName);
            }
        }
    }
}
