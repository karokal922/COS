using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ShapeCalculator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (rectangleRadioButton.IsChecked == false && circleRadioButton.IsChecked == false)
            {
                btnCalculate.IsEnabled = false;
            }
            xUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            yUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            rUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            vUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            liquidDensityUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            solidDensityUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            QmUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            QoUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            QmPrimeUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            QoPrimeUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            InitializeConversionFactors();
            InitializeCurrentUnits(); ;
        }
        private Dictionary<string, string> currentUnits;
        private Dictionary<(string, string), double> conversionFactors;

        private double Z, Zp;
        private int A;

        private double V, X, Y, R, Vp, Xp, Yp, Rp; // zmienne z 'p' to prim
        private double Rc;//gestosc ciekla
        private double Rs;//gestosc stala
        private double s1, s2;//skala gorna

        private double Qm, Qo, Sq, Qmm, Qom;

        private void ShapeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == rectangleRadioButton)
            {
                rectangleStackPanel.Visibility = Visibility.Visible;
                circleStackPanel.Visibility = Visibility.Collapsed;
            }
            else if (sender == circleRadioButton)
            {
                rectangleStackPanel.Visibility = Visibility.Collapsed;
                circleStackPanel.Visibility = Visibility.Visible;
            }
            btnCalculate.IsEnabled = true;
        }

        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.getS1();
                this.getS2();
                this.getRc();
                this.getRs();
                this.getV();
                this.getA();

                double przekroj;
                double przekrojModelu;
                double skala = this.s1 / this.s2;
                this.Zp = this.Z * skala;
                this.Vp = Math.Sqrt((this.V * this.V * this.Zp) / this.Z);

                if (rectangleRadioButton.IsChecked == false && circleRadioButton.IsChecked == false)
                {
                    throw new Exception("Należy zaznaczyć kształt odlewki!");
                }
                else if (rectangleRadioButton.IsChecked == true)
                {
                    this.getX();
                    this.getY();
                    this.Xp = this.X * skala;
                    this.Yp = this.X * skala;
                    przekroj = this.X * this.Y;
                    przekrojModelu = this.Xp * this.Yp;
                }
                else
                {
                    this.getR();
                    this.Rp = this.R * skala;
                    przekroj = Math.PI * this.R * this.R;
                    przekrojModelu = Math.PI * this.Rp * this.Rp;
                }

                QmUnitComboBox.SelectedIndex = 0;
                QoUnitComboBox.SelectedIndex = 0;
                QmPrimeUnitComboBox.SelectedIndex = 0;
                QoPrimeUnitComboBox.SelectedIndex = 0;

                this.Qm = this.A * przekroj * this.V * this.Rs;//m^2*m/min*kg/m^3 = kg/min
                QmOutputLabel.Content = this.Qm.ToString("F2");

                this.Qo = (this.Qm / this.Rc) * 1000.0;// (kg/min)/(kg/m^3) * 1000 = l/min
                QoOutputLabel.Content = this.Qo.ToString("F2");

                // QmPrimeLabel.Content = "Qm'(żyły: " + this.A.ToString() + ")";
                this.Qmm = przekrojModelu * this.Vp * this.Rs;//m^2*m/min*kg/m^3 = kg/min    //A żył
                QmPrimeOutputLabel.Content = this.Qmm.ToString("F2");

                //QoPrimeLabel.Content = "Qo'(żyły: " + this.A.ToString() + ")";
                this.Qom = (this.Qmm / this.Rc) * 1000.0;// (kg/min)/(kg/m^3) * 1000 = l/min   //A żył
                QoPrimeOutputLabel.Content = this.Qom.ToString("F2");

                this.Sq = Math.Sqrt(skala);
                SqOutputLabel.Content = this.Sq.ToString("F2");

            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

        }
        private void InitializeConversionFactors()
        {
            conversionFactors = new Dictionary<(string, string), double>
            {
                { ("mm", "cm"), 0.1 },
                { ("mm", "dm"), 0.01 },
                { ("mm", "m"), 0.001 },
                { ("cm", "mm"), 10 },
                { ("cm", "dm"), 0.1 },
                { ("cm", "m"), 0.01 },
                { ("dm", "mm"), 100 },
                { ("dm", "cm"), 10 },
                { ("dm", "m"), 0.1 },
                { ("m", "mm"), 1000 },
                { ("m", "cm"), 100 },
                { ("m", "dm"), 10 },


                { ("kg/m^3", "g/cm^3"), 0.001 },
                { ("g/cm^3", "kg/m^3"), 1000 },

                { ("m/min", "m/s"), 1.0 / 60.0 },
                { ("m/s", "m/min"), 60.0 },

                { ("kg/min", "g/s"), 16.6667 },
                { ("g/s", "kg/min"), 1.0 / 16.6667 },
                { ("kg/min", "kg/s"), 1.0 / 60 },
                { ("kg/s", "kg/min"), 60 },
                { ("kg/s", "g/s"), 1000 },
                { ("g/s", "kg/s"), 1.0 / 1000 },

                { ("l/min", "m^3/min"), 0.001 },
                { ("m^3/min", "l/min"), 1000 },
                { ("l/min", "l/s"), 1.0 / 60 },
                { ("l/s", "l/min"), 60 },
                { ("m^3/min", "l/s"), 1000 / 60.0 },
                { ("l/s", "m^3/min"), 60.0 / 1000 }
            };
        }

        private void InitializeCurrentUnits()
        {

            currentUnits = new Dictionary<string, string>
            {
                { "xUnitComboBox", "m" },
                { "yUnitComboBox", "m" },
                { "rUnitComboBox", "m" },
                { "vUnitComboBox", "m/min" },
                { "liquidDensityUnitComboBox", "kg/m^3" },
                { "solidDensityUnitComboBox", "kg/m^3" },
                { "QmUnitComboBox", "kg/min" },
                { "QoUnitComboBox", "l/min" },
                { "QmPrimeUnitComboBox", "kg/min" },
                { "QoPrimeUnitComboBox", "l/min" }
            };
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double textBoxWidth = (e.NewSize.Width - 50 - 90 - 800); // 50 for Label width, 90 for ComboBox width, 800 for proper scaling
            if (textBoxWidth > 0)
            {
                xTextBox.Width = textBoxWidth;
                yTextBox.Width = textBoxWidth;
                rTextBox.Width = textBoxWidth;
                vTextBox.Width = textBoxWidth;
                liquidDensityTextBox.Width = textBoxWidth;
                solidDensityTextBox.Width = textBoxWidth;
                veinsTextBox.Width = textBoxWidth;
            }
            else
            {
                xTextBox.Width = xTextBox.MinWidth;
                yTextBox.Width = yTextBox.MinWidth;
                rTextBox.Width = rTextBox.MinWidth;
                vTextBox.Width = vTextBox.MinWidth;
                liquidDensityTextBox.Width = liquidDensityTextBox.MinWidth;
                solidDensityTextBox.Width = solidDensityTextBox.MinWidth;
                veinsTextBox.Width = veinsTextBox.MinWidth;
            }
        }

        private void UnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                var newUnit = (comboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (newUnit != null)
                {
                    TextBox relatedTextBox = GetRelatedTextBox(comboBox);
                    Label relatedLabel = GetRelatedLabel(comboBox);
                    string currentUnit = currentUnits[comboBox.Name];
                    double originalValue;

                    if (relatedTextBox != null && double.TryParse(relatedTextBox.Text, out originalValue))
                    {
                        double newValue = ConvertUnit(originalValue, currentUnit, newUnit);
                        relatedTextBox.Text = newValue.ToString("0.###");
                    }
                    else if (relatedLabel != null && double.TryParse(relatedLabel.Content.ToString(), out originalValue))
                    {
                        double newValue = ConvertUnit(originalValue, currentUnit, newUnit);
                        relatedLabel.Content = newValue.ToString("0.###");
                    }

                    currentUnits[comboBox.Name] = newUnit;
                }
            }
        }



        private TextBox GetRelatedTextBox(ComboBox comboBox)
        {
            switch (comboBox.Name)
            {
                case "xUnitComboBox":
                    return xTextBox;
                case "yUnitComboBox":
                    return yTextBox;
                case "rUnitComboBox":
                    return rTextBox;
                case "vUnitComboBox":
                    return vTextBox;
                case "liquidDensityUnitComboBox":
                    return liquidDensityTextBox;
                case "solidDensityUnitComboBox":
                    return solidDensityTextBox;
                default:
                    return null;
            }
        }



        private Label GetRelatedLabel(ComboBox comboBox)
        {
            switch (comboBox.Name)
            {
                case "QmUnitComboBox":
                    return QmOutputLabel;
                case "QoUnitComboBox":
                    return QoOutputLabel;
                case "QmPrimeUnitComboBox":
                    return QmPrimeOutputLabel;
                case "QoPrimeUnitComboBox":
                    return QoPrimeOutputLabel;
                default:
                    return null;
            }
        }


        private double ConvertUnit(double value, string fromUnit, string toUnit)
        {
            if (fromUnit == toUnit) return value;

            if (conversionFactors.TryGetValue((fromUnit, toUnit), out var factor))
            {
                return value * factor;
            }

            throw new ArgumentException($"Nie znaleziono przelicznika z {fromUnit} do {toUnit}");
        }
        private void getV()
        {
            try
            {

                this.V = ConvertUnit(Convert.ToDouble(vTextBox.Text), (vUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(), "m/min");
                this.Z = this.V;
                if (this.V <= 0.0)
                {
                    throw new Exception("Prędkość odlewania nie może być mniejsza bądź równa zeru!");
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Zła dana V");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void getA()
        {
            try
            {
                this.A = Convert.ToInt32(veinsTextBox.Text);
                if (this.A <= 0.0)
                {
                    throw new Exception("Ilość żył nie może być mniejsza bądź równa zeru!");
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Zła dana a");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void getX()
        {
            try
            {
                this.X = ConvertUnit(Convert.ToDouble(xTextBox.Text), (xUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(), "m");
                if (this.X <= 0.0)
                {
                    throw new Exception("Długość boku X nie może być mniejsza bądź równa zeru!");
                }

            }
            catch (FormatException)
            {
                MessageBox.Show("Zła dana X");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void getY()
        {
            try
            {
                this.Y = ConvertUnit(Convert.ToDouble(yTextBox.Text), (yUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(), "m");
                if (this.Y <= 0.0)
                {
                    throw new Exception("Długość boku Y nie może być mniejsza bądź równa zeru!");
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Zła dana Y");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }


        private void getR()
        {
            try
            {
                this.R = ConvertUnit(Convert.ToDouble(rTextBox.Text), (rUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(), "m");
                if (this.R <= 0.0)
                {
                    throw new Exception("Promień nie może być mniejszy bądź równy zeru!");
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Zła dana R");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void getRc()
        {
            try
            {

                this.Rc = ConvertUnit(Convert.ToDouble(liquidDensityTextBox.Text), (liquidDensityUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(), "kg/m^3");
                if (this.Rc <= 0.0)
                {
                    throw new Exception("Gęstość nie może być mniejsza bądź równa zeru!");
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Zła dana ρc");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void getRs()
        {
            try
            {
                this.Rs = ConvertUnit(Convert.ToDouble(solidDensityTextBox.Text), (solidDensityUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(), "kg/m^3");
                if (this.Rs <= 0.0)
                {
                    throw new Exception("Gęstość nie może być mniejsza bądź równa zeru!");
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Zła dana ρs");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void getS1()
        {
            try
            {
                this.s1 = Convert.ToDouble(scaleTextBox1.Text);
                if (this.s1 <= 0.0)
                {
                    throw new Exception("Ta wartość skali nie może być mniejsza bądź równa zeru!");
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Zła dana skali (strona lewa)");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void getS2()
        {
            try
            {
                this.s2 = Convert.ToDouble(scaleTextBox2.Text);
                if (this.s2 <= 0.0)
                {
                    throw new Exception("Ta wartość skali nie może być mniejsza bądź równa zeru!");
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Zła dana skali (strona prawa)");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void btnSavePdf_Click(object sender, RoutedEventArgs e)
        {

            SaveFileDialog saveFileDialog = new SaveFileDialog();


            saveFileDialog.Filter = "Pliki PDF (*.pdf)|*.pdf|Wszystkie pliki (*.*)|*.*";

            saveFileDialog.DefaultExt = "pdf";

            saveFileDialog.FileName = "ShapeCalculatorResults.pdf";


            if (saveFileDialog.ShowDialog() == true)
            {

                string filePath = saveFileDialog.FileName;


                PdfDocument document = new PdfDocument();


                PdfPage page = document.AddPage();


                XGraphics gfx = XGraphics.FromPdfPage(page);


                XFont font = new XFont("Arial", 12, XFontStyle.Regular);


               // XImage image = XImage.FromFile("../Assets/logo.jpg");

              //  gfx.DrawImage(image, 20, 20, 100, 50);

                gfx.DrawString("COS", font, XBrushes.Black,
                    new XRect(0, 80, page.Width, 20),
                    XStringFormats.Center);


                DrawTable(gfx, font, 120, 200, new string[] { "Dane wejściowe", "Wybrane jednostki" },
                    new string[,]
                    {
                { "X", xTextBox.Text + " " + xUnitComboBox.Text },
                { "Y", yTextBox.Text + " " + yUnitComboBox.Text },
                { "R", rTextBox.Text + " " + rUnitComboBox.Text },
                { "V", vTextBox.Text + " " + vUnitComboBox.Text },
                { "", "" },
                { "Qm", QmUnitComboBox.Text },
                { "Qo", QoUnitComboBox.Text },
                { "QmPrime", QmPrimeUnitComboBox.Text },
                { "QoPrime", QoPrimeUnitComboBox.Text }
                    });


                string qmText = $"Qm: {QmOutputLabel.Content}";
                string qoText = $"Qo: {QoOutputLabel.Content}";
                string qmPrimeText = $"QmPrime: {QmPrimeOutputLabel.Content}";
                string qoPrimeText = $"QoPrime: {QoPrimeOutputLabel.Content}";

                gfx.DrawString(qmText, font, XBrushes.Black,
                    new XRect(0, 380, page.Width, 20),
                    XStringFormats.TopLeft);
                gfx.DrawString(qoText, font, XBrushes.Black,
                    new XRect(0, 400, page.Width, 20),
                    XStringFormats.TopLeft);
                gfx.DrawString(qmPrimeText, font, XBrushes.Black,
                    new XRect(0, 420, page.Width, 20),
                    XStringFormats.TopLeft);
                gfx.DrawString(qoPrimeText, font, XBrushes.Black,
                    new XRect(0, 440, page.Width, 20),
                    XStringFormats.TopLeft);


                document.Save(filePath);


                MessageBox.Show($"Plik PDF został zapisany jako: {filePath}");
            }
        }

        private void DrawTable(XGraphics gfx, XFont font, double x, double y, string[] headers, string[,] data)
        {
            const int cellPadding = 5;
            double currentY = y;

            for (int i = 0; i < headers.Length; i++)
            {
                gfx.DrawString(headers[i], font, XBrushes.Black, x, currentY);

                if (i < data.GetLength(0))
                {
                    for (int j = 0; j < 2; j++)
                    {
                        double textWidth = gfx.MeasureString(data[i, j], font).Width;
                        gfx.DrawString(data[i, j], font, XBrushes.Black, x + (j == 0 ? 100 : 250) - textWidth / 2, currentY + cellPadding);
                    }
                }

                currentY += font.Height + cellPadding * 2;
            }
        }
    }
}