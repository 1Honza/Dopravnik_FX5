using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using EasyModbus;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;



namespace Dopravnik_FX5
{
    public partial class Dopravnik : Form
    {
        bool isconnect = false;

        ModbusClient modbus = new ModbusClient();
        public Dopravnik()
        {
            InitializeComponent();
        }

        // definovani vlakna
        Thread thread;

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            // vytvoreni pripojeni
            modbus.IPAddress = Convert.ToString(textBoxTCP.Text);
            modbus.Port = Convert.ToInt32(textBoxPort.Text);
            try
            {
                modbus.Connect();
                isconnect = true;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            if (modbus.Connected == true)
            {
                labelStatus.Text = "Connected";
                labelStatus.ForeColor = Color.Green;
            }
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            // odpojeni, vypnuti vlakna, vypnuti vystupu

            if (isconnect == true)
            {
                modbus.Disconnect();
                labelStatus.Text = "Disconnected";
                labelStatus.ForeColor = Color.Red;

                if (modbus.Connected == true)
                {
                    thread.Abort();
                }
                modbus.WriteSingleCoil(2, false); // 0 - 2 - dopravník vpravo
                modbus.WriteSingleCoil(3, false); // 1 - 3 - dopravník vlevo
                modbus.WriteSingleCoil(6, false); // 4 - 6 - kontrolka S_1
                modbus.WriteSingleCoil(7, false); // 5 - 7 - kontrolka S_2
                modbus.WriteSingleCoil(8, false); // 6 - 8 - kontrolka S_3
            
                isconnect = false;
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (isconnect == true)
            {
                // OUTPUT Y2 = 2 - dopravnik vpravo, Y3 = 3 - dopravnik vlevo
                modbus.WriteSingleCoil(3, false);

                modbus.WriteSingleCoil(2, true);

                // Online read Input
                thread = new Thread(new ThreadStart(WorkThreadFunction));
                thread.Start();

                // Read INPUT X4 - S_1, X5 - S_2, X6 - S_3
                // OUT Y6 - 1. kontrolka S_1, Y7 - 2. kontrolka S_2, Y10 - 3. kontrolka S_3
                // definovani vlakna pro cteni aktualniho stavu vstupu

                void WorkThreadFunction()
                {
                    while (isconnect == true)
                    {
                        // read input senzor - zmena barvy 
                        Color color_S_1;
                        Color color_S_2;
                        Color color_S_3;

                        bool[] S_1 = modbus.ReadDiscreteInputs(4, 1); // X4 - S_1 senzor
                        bool[] S_2 = modbus.ReadDiscreteInputs(5, 1); // X5 - S_2 senzor
                        bool[] S_3 = modbus.ReadDiscreteInputs(6, 1); // X6 - S_3 senzor

                        // Senzor S_1
                        if (S_1[0] == true)
                        {
                            color_S_1 = Color.Green;
                            modbus.WriteSingleCoil(3, false); // dopravnik vlevo
                            Thread.Sleep(100);
                            modbus.WriteSingleCoil(2, true);  // dopravnik vpravo
                            modbus.WriteSingleCoil(6, true);  // kontrolka S_1
                        }

                        else
                        {
                            color_S_1 = Color.Red;
                            modbus.WriteSingleCoil(6, false);  // kontrolka S_1
                        }

                        // Senzor S_2
                        if (S_2[0] == true)
                        {
                            color_S_2 = Color.Green;
                            modbus.WriteSingleCoil(7, true);  // kontrolka S_2
                        }

                        else
                        {
                            color_S_2 = Color.Red;
                            modbus.WriteSingleCoil(7, false); // kontrolka S_2
                        }

                        // Senzor S_3
                        if (S_3[0] == true)
                        {
                            color_S_3 = Color.Green;
                            modbus.WriteSingleCoil(2, false); // dopravnik vpravo
                            Thread.Sleep(100);
                            modbus.WriteSingleCoil(3, true);  // dopravnik vlevo
                            modbus.WriteSingleCoil(8, true);  // kontrolka S_3
                        }

                        else
                        {
                            color_S_3 = Color.Red;
                            modbus.WriteSingleCoil(8, false);  // kontrolka S_3
                        }

                        // potlaceni vlakna pro prevod do jineho vlakna
                        {
                            this.Invoke((MethodInvoker)delegate { pictureBox_S_1.BackColor = color_S_1; });
                            this.Invoke((MethodInvoker)delegate { pictureBox_S_2.BackColor = color_S_2; });
                            this.Invoke((MethodInvoker)delegate { pictureBox_S_3.BackColor = color_S_3; });
                        }
                    }
                }
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            // stop tl. shodi vystupy a vlakno

            if (isconnect == true)
            {
                // End online read Input
                thread.Abort();

                modbus.WriteSingleCoil(2, false);  // 2 - dopravník vpravo
                modbus.WriteSingleCoil(3, false);  // 3 - dopravník vlevo
                modbus.WriteSingleCoil(6, false);  // 6 - kontrolka S_1
                modbus.WriteSingleCoil(7, false);  // 7 - kontrolka S_2
                modbus.WriteSingleCoil(8, false);  // 8 - kontrolka S_3
            }
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {    
            // exit shodi vystupy, vlakno, odpoji se a zavre okno 

            if (isconnect == true)
            {
                modbus.WriteSingleCoil(2, false);  // 2 - dopravník vpravo
                modbus.WriteSingleCoil(3, false);  // 3 - dopravník vlevo
                modbus.WriteSingleCoil(6, false);  // 6 - kontrolka S_1
                modbus.WriteSingleCoil(7, false);  // 7 - kontrolka S_2
                modbus.WriteSingleCoil(8, false);  // 8 - kontrolka S_3
                modbus.Disconnect();
            }

            isconnect = false;

            Close();
        }
    }
}
