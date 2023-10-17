using DTCChecker.Items;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;


namespace DTCChecker
{
    public partial class Form1 : Form
    {
        private static Form1 cd = new Form1();
        private string bluetoothAddress = "aa:bb:cc:11:22:33";
        private static Stream stream;
        private static BluetoothClient bluetoothClient = new BluetoothClient();
        private BluetoothDeviceInfo obdDevice;
        private List<DTCval> dtcvalues = new List<DTCval>();
        private String str = "Data Source=C:\\Users\\ADV ELEC (ASSY ME)\\Downloads\\WindowsFormsApp1\\DTCChecker\\localsqlite.sqlite";
        private static SQLiteConnection connection;



        public Form1()
        {
            InitializeComponent();
        }


        private async void connect()
        {
            connection = new SQLiteConnection(str);
            connection.Open();
            recentdtc();
            BluetoothAddress obd2DeviceAddress = BluetoothAddress.Parse(bluetoothAddress);
            obdDevice = new BluetoothDeviceInfo(obd2DeviceAddress);

            while (true)
            {
                try
                {
                    await bluetoothClient.ConnectAsync(obdDevice.DeviceAddress, BluetoothService.SerialPort);
                    stream = bluetoothClient.GetStream();
                    stream.ReadTimeout = 10000;
                    
                    int buffsize = 2048;
                    send("07" + "\r", buffsize);
                    
                }
                catch(ObjectDisposedException e)
                {
                    Console.WriteLine("aa0");
                    Console.WriteLine(e.StackTrace);
                    continue;
                }
                catch (Exception)
                {
                    Console.WriteLine("No Connection");
                    continue;
                }
                await Task.Delay(1000);


            }

        }
        private async void send(String msg, int recbuffsize)
        {

            listView1.Items.Clear();
            byte[] sendbuffer = Encoding.ASCII.GetBytes(msg);       //Convert String to bytes
            stream.Write(sendbuffer, 0, sendbuffer.Length);      //Sending msg = 07/r to Device
            //await Task.Delay(100);
            bool forcestop = false;
            string data = "";
            int k = 0;
            while (!forcestop)
            {
                try
                {
                    byte[] buffer = new byte[2048];

                    int bytesRead = stream.Read(buffer, 0, buffer.Length); 
                    //Read all data until ">" is received

                    byte[] mesajj = new byte[bytesRead];

                    for (int i = 0; i < bytesRead; i++)
                    {
                        mesajj[i] = buffer[i];
                    }

                    k++;

                    data += Encoding.Default.GetString(mesajj, 0, bytesRead).Replace("\r", " ");
                    Console.WriteLine(data);


                    byte[] temp2 = Encoding.ASCII.GetBytes("0120\r");

                    if (data.EndsWith(">") || data.Length > 128 || k > 10)
                    {

                        k = 0;

                        if (data.Length > 128)
                        {
                            stream.Write(temp2, 0, temp2.Length);
                            int byte2read = stream.Read(buffer,0,buffer.Length);
                            byte[] mes2 = new byte[byte2read];
                            for (int i = 0; i < bytesRead; i++)
                            {
                                mes2[i] = buffer[i];
                            }
                            Console.WriteLine(Encoding.Default.GetString(mes2, 0, byte2read));
                            //If data being sent exceeds 128 /r is sent to Device to get rest of the data
                        }
                        List<string> listofdata = new List<string>();
                        string[] templist = data.Split(' ');
                        for (int i = 0; i < templist.Length; i++)
                        {
                            listofdata.Add(templist[i]);
                        }

                        listconvert(listofdata);

                        data = "";
                    }
                }
                catch (Exception)
                {
                    forcestop = true;

                }
            }
        }

        public async void listconvert(List<string> rmsg)
        {
            //label1.Text = recmsg;
            //string[] recarray = recmsg.Split(' ');
            //Console.WriteLine(recmsg);
            List<string> recmsg = new List<string>();
            for (int i = 0; i < rmsg.Count; i++)
            {
                if (i != rmsg.Count - 1)
                {
                    if (rmsg[i] != "07" && rmsg[i + 1] != "47")
                    {
                        if (rmsg[i].Contains("47") && rmsg[i + 1].Contains("00"))
                        {
                            i++;
                        }
                        else if (rmsg[i].Contains("0:") || rmsg[i].Contains("1:"))
                        {
                            continue;
                        }
                        else
                        {
                            if (!rmsg[i].Contains(" "))
                            {
                                if (rmsg[i + 1].Contains(":"))
                                {
                                    recmsg.Add(rmsg[i]);
                                    i = i + 1;
                                }
                                else
                                {
                                    recmsg.Add(rmsg[i]);
                                }

                            }
                        }
                    }

                }
                else
                {
                    recmsg.Add(rmsg[i]);
                }
            }

            int startfrom = 0;
            List<String> list1 = new List<string>();
            for (int i = 0; i < recmsg.Count; i++)
            {
                if (recmsg[i].Contains("47"))
                {
                    startfrom = i;
                    break;
                }
            }

            for (int i = startfrom + 2; i < recmsg.Count; i++)
            {
                if (recmsg[i].StartsWith("0") || recmsg[i].StartsWith("1") || recmsg[i].StartsWith("2") || recmsg[i].StartsWith("3"))
                {

                    string conc = "P" + recmsg[i] + recmsg[i + 1];
                    i++;
                    list1.Add(conc.Trim());
                }
            }
            



            List<string> list2 = new List<string>();

            for (int p = 0; p < list1.Count; p++)
            {
                string ele = list1[p];
                var dtcval = dtcvalues.Where(h => h.DTCCodes == ele).LastOrDefault();
                if (dtcval != null)
                {
                    list2.Add(dtcval.DTCDetails);
                }
                else if (dtcval == null)
                {
                    list2.Add("NULL");
                }

            }

            string alldtc = "";

            for (int i = 0; i < list1.Count; i++)
            {
                ListViewItem item = new ListViewItem(new string[] { list1[i], list2[i] });
                listView1.Items.Add(item);
                alldtc = alldtc + list1[i] + ",";

            }

            DateTime now = DateTime.Now;
            string dt = now.ToString();

            string[] currdt = dt.Split(' ');
            string currd = now.ToString("yyyy-MM-dd");

            //SQLiteConnection con = new SQLiteConnection(str);
            string sc = "INSERT INTO DTCCodes(Date, Time, DTCCodes) values(@date1,@time1,@pcode)";

            //con.Open();
            string ctq = "CREATE TABLE IF NOT EXISTS DTCCodes(Date varchar(10), Time time(0), DTCCodes varchar(100))";
            using (SQLiteCommand cmd = new SQLiteCommand(ctq, connection))
            {
                cmd.ExecuteNonQuery();

            }
            SQLiteCommand si = new SQLiteCommand(sc, connection);
            si.Parameters.AddWithValue("@date1", currd);
            si.Parameters.AddWithValue("@time1", currdt[1]);
            si.Parameters.AddWithValue("@pcode", alldtc);
            si.ExecuteNonQuery();
            //con.
            //
            //();
            recentdtc();




            
            await Task.Delay(1000);

            //connect();
            concheck();

        }

        void listView1_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dtcvalues = Helpers.Helper.ReadJsonConfiguration(Helpers.Helper.ReadResource("dtclist"));
            connect();
        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }



        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        public void recentdtc()
        {
            listView2.Items.Clear();
            //using (SQLiteConnection connection = new SQLiteConnection(str))
            //{
            //connection.Open();
            string query = "SELECT * FROM DTCCodes ORDER BY Date DESC,Time DESC LIMIT 10";
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            foreach (DataRow row in dataTable.Rows)
            {
                ListViewItem item = new ListViewItem(row["Date"].ToString());
                string a = row["Time"].ToString();
                string[] qw = a.Split(' ');
                item.SubItems.Add(qw[1]);
                item.SubItems.Add(row["DTCCodes"].ToString());

                listView2.Items.Add(item);
            }
            numofdtc();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            listView3.Items.Clear();
            string fromdate = dateTimePicker2.Value.ToString().Split(' ')[0];
            string todate = dateTimePicker1.Value.ToString().Split(' ')[0];
            string query = "SELECT * FROM DTCCodes WHERE Date > @fdt AND Date < @fdtt OR Date = @fdt OR Date = @fdtt";
            SQLiteCommand command = new SQLiteCommand(query, connection);
            command.Parameters.Add(new SQLiteParameter("@fdt", DbType.String));
            command.Parameters["@fdt"].Value = fromdate;
            command.Parameters.Add(new SQLiteParameter("@fdtt", DbType.String));
            command.Parameters["@fdtt"].Value = todate;

            SQLiteDataAdapter adapter = new SQLiteDataAdapter(command); // Pass the command to the adapter
            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            foreach (DataRow row in dataTable.Rows)
            {
                ListViewItem item = new ListViewItem(row["Date"].ToString());
                string a = row["Time"].ToString();
                string[] qw = a.Split(' ');
                item.SubItems.Add(qw[1]);
                item.SubItems.Add(row["DTCCodes"].ToString());

                listView3.Items.Add(item);
            }
            //connection.Close();
            //}


        }

        private void listView2_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }


        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            listView3.Items.Clear();
            string query = "SELECT * FROM DTCCodes WHERE DTCCodes LIKE @txtval";
            string txtval = textBox1.Text;

            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@txtval", "%" + txtval + "%");
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                {
                    DataTable datatable = new DataTable();
                    adapter.Fill(datatable);

                    foreach (DataRow row in datatable.Rows)
                    {
                        ListViewItem item = new ListViewItem(row["Date"].ToString());
                        string a = row["Time"].ToString();
                        string[] qw = a.Split(' ');
                        item.SubItems.Add(qw[1]);
                        item.SubItems.Add(txtval.ToString());

                        listView3.Items.Add(item);
                    }

                    int count = datatable.Rows.Count;
                    label6.Text = count.ToString();
                }


            }


        }
        private async void concheck()
        {
            if (bluetoothClient.Connected)
            {
                concheck();
            }
            else
            {
                connect();
            }
            await Task.Delay(1000);
        }
        private void numofdtc()
        {
            listView4.Items.Clear();
            List<string> availdtc = new List<string>();

            string query = "SELECT DTCCodes from DTCCodes";
            using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
            {
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                {
                    DataTable data = new DataTable();
                    adapter.Fill(data);

                    foreach (DataRow row in data.Rows)
                    {
                        string[] templist = row["DTCCodes"].ToString().Split(',');
                        foreach (string i in templist)
                        {
                            if (i.Contains('P'))
                            {
                                availdtc.Add(i);
                            }
                            else if (i.Contains("C"))
                            {
                                availdtc.Add(i);
                            }
                            else if (i.Contains("B"))
                            {
                                availdtc.Add(i);
                            }
                            else if (i.Contains("U"))
                            {
                                availdtc.Add(i);
                            }
                        }
                    }

                }
            }

            List<string> uniquedtc = new List<string>();

            for (int i = 0; i < availdtc.Count; i++)
            {
                if (!uniquedtc.Contains(availdtc[i]))
                {
                    uniquedtc.Add(availdtc[i]);
                }
            }

            var counts = availdtc.GroupBy(item => item).Select(group => new
            {
                Element = group.Key,
                Count = group.Count()
            }).OrderByDescending(item => item.Count);

            foreach (var ele in counts)
            {
                ListViewItem item = new ListViewItem(new string[] { ele.Element, ele.Count.ToString() });
                listView4.Items.Add(item);
            }
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void HOME_Click(object sender, EventArgs e)
        {

        }

        private void listView4_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {

        }

        private void listView3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            listView5.Items.Clear();
            System.Drawing.Size tileSize = new System.Drawing.Size(listView5.Width, 70);
            listView5.TileSize = tileSize;

            string dtc = textBox2.Text;

            var dtcval = dtcvalues.Where(h => h.DTCCodes == dtc).LastOrDefault();
            if (dtcval != null)
            {
                ListViewItem item = new ListViewItem(new string[] { dtcval.DTCCodes, dtcval.DTCDetails, });
                listView5.Items.Add(item);
            }
            else
            {
                ListViewItem item = new ListViewItem(new string[] { textBox2.Text, "No Such DTC Found" });
                listView5.Items.Add(item);
            }



        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }
    }
}
