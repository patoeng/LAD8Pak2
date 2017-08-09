using System;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using LAD08PackagingV1.Properties;
using MetroFramework.Forms;

namespace LAD08PackagingV1
{
    public delegate void WeighingData(double value);
    public delegate void WeighingException(string exception);
    public delegate void WeighingQuantityData(int value,int delta);

    public delegate void DelegateSetLabelText(string value);
    public partial class Weighing : MetroForm
    {
        private readonly Settings _settings;
        private readonly ReferenceModel _referenceModel;

        public event WeighingData WeighingDataUpdated;
        public event WeighingData WeighingDataInRange;
        public event WeighingData WeighingDataOutRange;
        public event WeighingData WeighingBoxRemoved;
        public event WeighingQuantityData WeighingQuantityUpdated;
        public event WeighingQuantityData WeighingBoxQuantityAchieved;
        public event WeighingException WeighingExeption;

        public void SetNeedToRemoveBox()
        {
            NeedToRemove = true;
        }
        public bool NeedToRemove { get; protected set; }
        public double UpperWeightPerBox { get; protected set; }
        public double LowerWeightPerBox { get; protected set; }
        public bool WeightInRange { get; protected set; }
        public double NominalWeightIndividual { get; protected set; }
        public int QuantityByWeight { get; protected set; }
        
        private int _stabilizationCounter;
        private double _value;
        public double Value
        {
            get { return _value; }
            protected set
            {
                if (Math.Abs(_value - value) < 0.01)
                {
                    _stabilizationCounter--;
                    if (_stabilizationCounter <= 0)
                    {
                        _stabilizationCounter = _settings.WeighingStabilizationTime;
                        if (_value > LowerWeightPerBox && _value < UpperWeightPerBox)
                        {
                            if (!WeightInRange)
                            {
                                WeightInRange = true;
                                WeighingDataInRange?.Invoke(Value);
                            }
                        }
                        else
                        {
                            WeightInRange = false;
                            WeighingDataOutRange?.Invoke(_value);
                        }
                        CalculatedWeightQty = CalculateQuantityInBox(_value, _referenceModel.WeighingNominal);
                        if (_value < 26.0)
                        {
                            NeedToRemove = false;
                            WeighingBoxRemoved?.Invoke(_value);
                        }
                        
                    }
                    
                }
                _value = value;
                WeighingDataUpdated?.Invoke(_value);
            }
        }

        private int _calculatedWeightQty;
        public int CalculatedWeightQty
        {
            get {return _calculatedWeightQty;}
            protected set
            {
                if (_calculatedWeightQty == value) return;
                var oldCalculatedWeightQty = _calculatedWeightQty;
                _calculatedWeightQty = value;
                var delta = NeedToRemove? 0 : _calculatedWeightQty - oldCalculatedWeightQty;
                WeighingQuantityUpdated?.Invoke(_calculatedWeightQty,delta);
                if (_calculatedWeightQty == _referenceModel.QuantityGroup)
                {
                    WeighingBoxQuantityAchieved?.Invoke(_calculatedWeightQty,delta);
                }
                if (_calculatedWeightQty > _referenceModel.QuantityGroup)
                {
                    WeighingExeption?.Invoke("Quantity dalam box melebihi quantity grouping.");
                }
            }
        }
        private int CalculateQuantityInBox(double actualWeight, double nominalWeight)
        {
            if (actualWeight <= 0) return 0;
            var temp1 = Math.Round(actualWeight / nominalWeight, MidpointRounding.AwayFromZero);
            var temp2 = Convert.ToInt32(temp1);
            return temp2;
        }

        public Weighing(Settings settings, ReferenceModel reference)
        {
            InitializeComponent();
            _settings = settings;
            _referenceModel = reference;
            LoadAll();

            try
            {
                WeighingDataUpdated -= OnWeighingDataUpdated;
                WeighingDataInRange -= OnWeighingDataInRange;
                WeighingDataOutRange -= OnWeighingDataOutRange;
            }
            catch
            {
                // ignored
            }
            WeighingDataUpdated += OnWeighingDataUpdated;
            WeighingDataInRange += OnWeighingDataInRange;
            WeighingDataOutRange += OnWeighingDataOutRange;
        
        }

        private void OnWeighingDataOutRange(double value)
        {
            lblValue.ForeColor = Color.Red;
        }

        private void OnWeighingDataInRange(double value)
        {
            lblValue.ForeColor = Color.Green;
        }

        private void OnWeighingDataUpdated(double value)
        {
            UpdateLabelWithInvoke(lblValue.Text = value.ToString("F1"));
        }

        private void LoadAll()
        {
            IterateAllComport(cbbComPort, _settings.WeighingComName);

            serialPortWeighing.PortName = _settings.WeighingComName;
            serialPortWeighing.BaudRate = _settings.WeighingComBaudRate;
            serialPortWeighing.StopBits = _settings.WeighingComStopBit;
            serialPortWeighing.Parity = _settings.WeighingComParity;

            _stabilizationCounter = _settings.WeighingStabilizationTime;
            gbLimits.Enabled = false;
            if (_referenceModel != null)
            {
               

                lblReference.Text = _referenceModel.Reference;
                lblReference2.Text = _referenceModel.Reference;

                NominalWeightIndividual = _referenceModel.WeighingNominal;

                textNominal.Text = _referenceModel.WeighingNominal.ToString("F1");
                gbLimits.Enabled = true;
                UpperWeightPerBox = (_referenceModel.WeighingNominal * _referenceModel.QuantityGroup) +
                                    (_referenceModel.WeighingNominal / 2);
                LowerWeightPerBox = (_referenceModel.WeighingNominal * _referenceModel.QuantityGroup) -
                                    (_referenceModel.WeighingNominal / 2);
            }

            textStabilization.Text = _settings.WeighingStabilizationTime.ToString();


            try
            {
                if (!serialPortWeighing.IsOpen)
                    serialPortWeighing.Open();
                btnOpenClose.Text = serialPortWeighing.IsOpen ? "Close" : "Open";
                lbIsOpenClosed.Text = serialPortWeighing.IsOpen ? "Com is Open" : "Com is Closed";
            }
            catch
            {
                // ignored
            }
        }
        private void IterateAllComport(ComboBox cbb, string selected)
        {
            cbb.Items.Clear();
            var ports = SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                cbb.Items.Add(port);
            }
            cbb.Text = selected;
        }
        private void Weighing_Load(object sender, System.EventArgs e)
        {

        }

   

        private string _temporaryContainer;
        private void serialPortWeighing_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_settings.WeighingComSuffix)
            {
                _temporaryContainer += serialPortWeighing.ReadExisting();
                if (_temporaryContainer.Contains("\r") || e.EventType == SerialData.Eof)
                {
                    UpdateValueWithInvoke(_temporaryContainer.Trim('\r','\n'));
                    _temporaryContainer = "";
                }
            }
            else
            {
                _temporaryContainer = serialPortWeighing.ReadExisting();
                UpdateValueWithInvoke(_temporaryContainer.Trim('\r', '\n'));
                _temporaryContainer = "";
            }           
        }

        private void UpdateValueWithInvoke(string text)
        {
            if (lblValue.InvokeRequired)
            {
                DelegateSetLabelText d = UpdateValueWithInvoke;
                Invoke(d, text);
            }
            else
            {
                try
                {
                    var j = text.Split(' ');
                    if (j.Length<3) return;
                    if (j[1] == "-") j[1] = j[1] + j[2];
                    Value = Convert.ToDouble(j[1]);
                }
                catch
                {
                 //  MessageBox.Show(@"Convert Error");
                }
            }

        }
        private void UpdateLabelWithInvoke(string text)
        {
            if (lblValue.InvokeRequired)
            {
                DelegateSetLabelText d = UpdateValueWithInvoke;
                Invoke(d, text);
            }
            else
            {
                try
                {
                    lblValue.Text = text;
                }
                catch
                {
                    //  MessageBox.Show(@"Convert Error");
                }
            }

        }
       

      
        private void btnOpenClose_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnOpenClose.Text == @"Close")
                {
                    if (serialPortWeighing.IsOpen)
                    {
                        serialPortWeighing.Close();
                        btnOpenClose.Text = @"Open";
                        lbIsOpenClosed.Text = @"Com Is Close";
                    }
                }
                else
                {
                    if (!serialPortWeighing.IsOpen)
                    {
                        serialPortWeighing.Open();
                        btnOpenClose.Text = @"Close";
                        lbIsOpenClosed.Text = @"Com Is Open";
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private void btnSavePort_Click(object sender, EventArgs e)
        {
            _settings.WeighingComName = cbbComPort.Text;
            _settings.WeighingStabilizationTime =
                Convert.ToInt32(textStabilization.Text == "" ? "1" : textStabilization.Text);
            _settings.Save();
            _settings.Reload();
        }


        private void btnSaveLimits_Click(object sender, EventArgs e)
        {
            gbLimits.Enabled = false;
            try
            {
               
                _referenceModel.WeighingNominal = Convert.ToDouble(textNominal.Text);
                using (var jj = new ReferenceDataMicrosoftAccess(_settings.DatabaseReference, _settings.ProviderDataBase))
                {
                    jj.UpdateLimits(_referenceModel);

                }
            }
            catch
            {                
                // ignored
            }
            gbLimits.Enabled = true;
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            textNominal.Text = _referenceModel.WeighingNominal.ToString("F1");
        }

        private void Weighing_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            IterateAllComport(cbbComPort, _settings.WeighingComName);
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
