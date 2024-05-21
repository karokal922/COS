using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using System.Runtime.CompilerServices;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Drawing;
using System.Collections.Generic;
using System.Windows.Input;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Windows.Media;

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
            zapiszBtn.IsEnabled = false;
            btnSavePdf_Copy.IsEnabled = false;   
            xUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            yUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            rUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            vUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            liquidDensityUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            solidDensityUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            QmUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            QoUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            QoPrimeUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            InitializeConversionFactors();
            InitializeCurrentUnits();
        }
        private Dictionary<string, string> currentUnits;
        private Dictionary<(string, string), double> conversionFactors;

        private double Z, Zp;
        private int A;

        private double V, X, Y, R, Vp, Xp, Yp, Rp; // zmienne z 'p' to prim
        private double Rc;//gestosc ciekla
        private double Rs;//gestosc stala
        private double s1, s2;//skala gorna

        private double Qm, Qo, Sq, Qom;

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
            checkInputs();
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
                QmUnitComboBoxSingleVein.SelectedIndex = 0;
                QoUnitComboBox.SelectedIndex = 0;
                QoUnitComboBoxSingleVein.SelectedIndex = 0;
                QoPrimeUnitComboBox.SelectedIndex = 0;
                QoPrimeUnitComboBoxSingleVein.SelectedIndex = 0;

                VeinsNumberLabel.Content = "Ilość żył: " + this.A.ToString();
                VeinsNumberLabel2.Content = "Ilość żył: " + this.A.ToString();

                this.Qm = this.A * przekroj * this.V * this.Rs;//m^2*m/min*kg/m^3 = kg/min
                QmOutputLabel.Content = this.Qm.ToString("F2");
                QmOutputLabelSingleVein.Content = (this.Qm/this.A).ToString("F2");

                this.Qo = (this.Qm / this.Rc) * 1000.0;// (kg/min)/(kg/m^3) * 1000 = l/min
                QoOutputLabel.Content = this.Qo.ToString("F2");
                QoOutputLabelSingleVein.Content = (this.Qo/this.A).ToString("F2");

                //QoPrimeLabel.Content = "Qo'(żyły: " + this.A.ToString() + ")";
                this.Qom = Qo * Math.Pow(skala, 2.5);// (kg/min)/(kg/m^3) * 1000 = l/min   //A żył
                QoPrimeOutputLabel.Content = this.Qom.ToString("F2");
                QoPrimeOutputLabelSingleVein.Content = (this.Qom/this.A).ToString("F2");

                //this.Sq = Math.Sqrt(skala);
                //SqOutputLabel.Content = this.Sq.ToString("F2");

            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }//SingleVein

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
                { "QoPrimeUnitComboBox", "l/min" }
            };
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double textBoxWidth = (e.NewSize.Width - 50 - 90 - 1200); // 50 for Label width, 90 for ComboBox width, 800 for proper scaling
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
                    else if (relatedLabel != null)
                    {
                        if (relatedLabel.Content != null)
                        {
                            double.TryParse(relatedLabel.Content.ToString(), out originalValue);
                            double newValue = ConvertUnit(originalValue, currentUnit, newUnit);
                            relatedLabel.Content = newValue.ToString("0.###");
                        }
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

        private void zapiszBtn_Click(object sender, RoutedEventArgs e)
        {
            WriteExcelData();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ReadExcelData();
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
                MessageBox.Show("Zła dana - prędkość odlewania (V)");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnCalculate_Click(sender, e);
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
                MessageBox.Show("Zła dana - ilość żył (a)");
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
                    throw new Exception("Długość wlewka X nie może być mniejsza bądź równa zeru!");
                }

            }
            catch (FormatException)
            {
                MessageBox.Show("Zła dana - długość wlewka (X)");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSavePdf_Click(object sender, RoutedEventArgs e)
        {
            SavePdf();
        }

        private void getY()
        {
            try
            {
                this.Y = ConvertUnit(Convert.ToDouble(yTextBox.Text), (yUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(), "m");
                if (this.Y <= 0.0)
                {
                    throw new Exception("Szerokość wlewka (Y) nie może być mniejsza bądź równa zeru!");
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Zła dana - szerokość wlewka (Y)");
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
                MessageBox.Show("Zła dana - promień wlewka (R)");
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
                MessageBox.Show("Zła dana - gęstość w stanie ciekłym (ρc)");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            rTextBox.Text = "";
            xTextBox.Text = "";
            yTextBox.Text = "";
            liquidDensityTextBox.Text = "";
            solidDensityTextBox.Text = "";
            scaleTextBox1.Text = "1";
            scaleTextBox2.Text = "1";
            vTextBox.Text = "";
            veinsTextBox.Text = "";

            VeinsNumberLabel.Content = "Ilość żył: ";
            VeinsNumberLabel2.Content = "Ilość żył: ";
            QoOutputLabel.Content = "";
            QmOutputLabel.Content = "";

            QoOutputLabelSingleVein.Content = "";
            QmOutputLabelSingleVein.Content = "";

            QoPrimeOutputLabel.Content = "";
            QoPrimeOutputLabelSingleVein.Content = "";

        }

        private void vTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            checkInputs();
        }

        private void veinsTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            checkInputs();
        }

        private void liquidDensityTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            checkInputs();
        }

        private void solidDensityTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            checkInputs();
        }

        private void xTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            checkInputs();
        }

        private void yTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            checkInputs();
        }

        private void rTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            checkInputs();
        }

        private void scaleTextBox1_KeyUp(object sender, KeyEventArgs e)
        {
            checkInputs();
        }

        private void scaleTextBox2_KeyUp(object sender, KeyEventArgs e)
        {
            checkInputs();
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
                MessageBox.Show("Zła dana - gęstość w stanie stałym (ρs)");
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
                MessageBox.Show("Zła dana - skali (strona lewa)");
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
                MessageBox.Show("Zła dana - skali (strona prawa)");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void SavePdf()
        {
            // Otwórz okno dialogowe zapisu pliku
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PDF file (*.pdf)|*.pdf";
            saveFileDialog.Title = "Save PDF file";

            if (saveFileDialog.ShowDialog() == true)
            {
                string filename = saveFileDialog.FileName;

                // Utwórz nowy dokument PDF
                PdfDocument document = new PdfDocument();
                document.Info.Title = "COS";

                // Utwórz nową stronę PDF
                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont titleFont = new XFont("Verdana", 24, XFontStyle.Bold);
                XFont sectionFont = new XFont("Verdana", 16, XFontStyle.Bold);
                XFont font = new XFont("Verdana", 12, XFontStyle.Regular);
                XFont boldFont = new XFont("Verdana", 12, XFontStyle.Bold);

                // Ustawienie tła strony
                XSolidBrush backgroundBrush = new XSolidBrush(XColor.FromArgb(255, 245, 245, 245));
                gfx.DrawRectangle(backgroundBrush, 0, 0, page.Width, page.Height);

                // Załaduj obraz z katalogu Assets
                string imagePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Assets", "logo.png");
                XImage xImage = null;
                try
                {
                    xImage = XImage.FromFile(imagePath);
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("Logo image not found. Please ensure the logo image exists in the 'Assets' directory.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double logoWidth = 100;
                double logoHeight = (xImage.PixelHeight / (double)xImage.PixelWidth) * logoWidth;
                double logoX = (page.Width - logoWidth) / 2;
                double logoY = 40;
                gfx.DrawImage(xImage, logoX, logoY, logoWidth, logoHeight); ;

                // Dodaj nagłówek poniżej logo
                gfx.DrawString("Parametry Procesowe urządzenia COS", titleFont,
                    XBrushes.Black, new XRect(0, logoY + logoHeight + 10, page.Width, 40), XStringFormats.TopCenter);

                // Dodaj dane wejściowe
                int yPoint = (int)(logoY + logoHeight + 60);
                gfx.DrawString("Dane Wejściowe", sectionFont,
                    XBrushes.Black, new XRect(20, yPoint, page.Width, 20), XStringFormats.TopLeft);

                yPoint += 30;
                AddInputDataToPdf(gfx, font, boldFont, ref yPoint);

                // Dodaj dane wyjściowe
                yPoint += 40;
                gfx.DrawString("Wyniki", sectionFont,
                    XBrushes.Black, new XRect(20, yPoint, page.Width, 20), XStringFormats.TopLeft);

                yPoint += 30;
                AddOutputDataToPdf(gfx, font, boldFont, ref yPoint);

                try
                {
                    // Zapisz dokument PDF
                    document.Save(filename);
                    MessageBox.Show("Generowanie pliku PDF zakończone sukcesem!");
                }
                catch (IOException ex)
                {
                    MessageBox.Show("Nie udało się zapisać pliku. Zamknij plik PDF, jeśli jest otwarty, i spróbuj ponownie.", "Błąd zapisu ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddInputDataToPdf(XGraphics gfx, XFont font, XFont boldFont, ref int yPoint)
        {
            DrawTableHeader(gfx, boldFont, ref yPoint);

            if (rectangleRadioButton.IsChecked == true)
            {
                DrawTableRow(gfx, font, ref yPoint, "Długość przekroju odlewanego wlewka", xTextBox.Text + " " + (xUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
                DrawTableRow(gfx, font, ref yPoint, "Szerokość przekroju odlewanego wlewka", yTextBox.Text + " " + (yUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
            }
            else if (circleRadioButton.IsChecked == true)
            {
                DrawTableRow(gfx, font, ref yPoint, "Promień przekroju odlewanego wlewka", rTextBox.Text + " " + (rUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
            }

            DrawTableRow(gfx, font, ref yPoint, "Prędkość odlewania", vTextBox.Text + " " + (vUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
            DrawTableRow(gfx, font, ref yPoint, "Gęstość ciekła", liquidDensityTextBox.Text + " " + (liquidDensityUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
            DrawTableRow(gfx, font, ref yPoint, "Gęstość stała", solidDensityTextBox.Text + " " + (solidDensityUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
            DrawTableRow(gfx, font, ref yPoint, "Ilość żył", veinsTextBox.Text);
            DrawTableRow(gfx, font, ref yPoint, "Skala", scaleTextBox1.Text + ":" + scaleTextBox2.Text);
        }

        private void AddOutputDataToPdf(XGraphics gfx, XFont font, XFont boldFont, ref int yPoint)
        {
            DrawTableHeader(gfx, boldFont, ref yPoint);

            DrawTableRow(gfx, font, ref yPoint, "Przepływ masowy", QmOutputLabel.Content.ToString()+ " " + (QmUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
            DrawTableRow(gfx, font, ref yPoint, "Przepływ objętościowy", QoOutputLabel.Content.ToString() + " " + (QoUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
            DrawTableRow(gfx, font, ref yPoint, "Modelowy przepływ objętościowy", QoPrimeOutputLabel.Content.ToString() + " " + (QoPrimeUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
        }

        private void DrawTableHeader(XGraphics gfx, XFont boldFont, ref int yPoint)
        {
            XPen pen = new XPen(XColors.Black, 0.5);

            gfx.DrawLine(pen, 20, yPoint - 5, 580, yPoint - 5);
            gfx.DrawString("Parametr", boldFont, XBrushes.Black, new XRect(20, yPoint, 250, 20), XStringFormats.TopLeft);
            gfx.DrawString("Wartość", boldFont, XBrushes.Black, new XRect(270, yPoint, 250, 20), XStringFormats.TopLeft);
            yPoint += 20;
            gfx.DrawLine(pen, 20, yPoint, 580, yPoint);
        }

        private void DrawTableRow(XGraphics gfx, XFont font, ref int yPoint, string parameter, string value)
        {
            XPen pen = new XPen(XColors.Gray, 0.5);

            gfx.DrawString(parameter, font, XBrushes.Black, new XRect(20, yPoint, 250, 20), XStringFormats.TopLeft);
            gfx.DrawString(value, font, XBrushes.Black, new XRect(270, yPoint, 250, 20), XStringFormats.TopLeft);
            yPoint += 20;
            gfx.DrawLine(pen, 20, yPoint, 580, yPoint);
        }



        public void ReadExcelData()
        {
            ChangeUnitsToSI();
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Pliki Excel (*.xlsx)|*.xlsx|Wszystkie pliki (*.*)|*.*";
            openFileDialog.Title = "Wybierz plik Excel";
            openFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet("Arkusz1");

                    string wartosc = worksheet.Cell("B2").Value.ToString();
                    Console.WriteLine(wartosc);
                    wartosc = wartosc.TrimStart('\'');
                    Console.WriteLine(wartosc);
                    string[] wartoscSplit = wartosc.Split(':');
                    Console.WriteLine(wartoscSplit);
                    scaleTextBox1.Text = wartoscSplit[0];
                    scaleTextBox2.Text = wartoscSplit[1];

                    // Jeśli zapisane dane odnoszą się do prostokąta
                    if (worksheet.Cell("A3").Value.ToString() == "Długość przekroju odlewanego wlewka")
                    {
                        rectangleRadioButton.IsChecked = true;
                        ShapeRadioButton_Checked(rectangleRadioButton, null);
                        rTextBox.Text = "";
                        xTextBox.Text = worksheet.Cell("B3").GetString();
                        yTextBox.Text = worksheet.Cell("B4").GetString();
                        vTextBox.Text = worksheet.Cell("B5").GetString();
                        veinsTextBox.Text = worksheet.Cell("B6").GetString();
                        liquidDensityTextBox.Text = worksheet.Cell("B7").GetString();
                        solidDensityTextBox.Text = worksheet.Cell("B8").GetString();

                        int A = int.Parse(worksheet.Cell("B6").GetString());
                        VeinsNumberLabel.Content = "Ilość żył: " + A.ToString();
                        VeinsNumberLabel2.Content = "Ilość żył: " + A.ToString();
                        if (A == 1)
                        {
                            QmOutputLabel.Content = worksheet.Cell("B10").GetString();
                            QoOutputLabel.Content = worksheet.Cell("B12").GetString();
                            QmOutputLabelSingleVein.Content = worksheet.Cell("B10").GetString();
                            QoOutputLabelSingleVein.Content = worksheet.Cell("B12").GetString();
                            QoPrimeOutputLabel.Content = worksheet.Cell("B14").GetString(); ;
                            QoPrimeOutputLabelSingleVein.Content = worksheet.Cell("B14").GetString();
                        }
                        else if (A > 1)
                        {
                            QmOutputLabel.Content = worksheet.Cell("B11").GetString();
                            QoOutputLabel.Content = worksheet.Cell("B14").GetString();
                            QmOutputLabelSingleVein.Content = worksheet.Cell("B10").GetString();
                            QoOutputLabelSingleVein.Content = worksheet.Cell("B13").GetString();
                            QoPrimeOutputLabel.Content = worksheet.Cell("B17").GetString(); ;
                            QoPrimeOutputLabelSingleVein.Content = worksheet.Cell("B16").GetString();
                        }

                    }
                    // Jeśli zapisane dane odnoszą się do koła
                    else if (worksheet.Cell("A3").Value.ToString() == "Promień odlewanego wlewka")
                    {
                        circleRadioButton.IsChecked = true;
                        ShapeRadioButton_Checked(circleRadioButton, null);
                        xTextBox.Text = "";
                        yTextBox.Text = "";
                        rTextBox.Text = worksheet.Cell("B3").GetString();
                        vTextBox.Text = worksheet.Cell("B4").GetString();
                        veinsTextBox.Text = worksheet.Cell("B5").GetString();
                        liquidDensityTextBox.Text = worksheet.Cell("B6").GetString();
                        solidDensityTextBox.Text = worksheet.Cell("B7").GetString();
                        
                        int A = int.Parse(worksheet.Cell("B5").GetString());
                        VeinsNumberLabel.Content = "Ilość żył: " + A.ToString();
                        VeinsNumberLabel2.Content = "Ilość żył: " + A.ToString();
                        if (A == 1)
                        {
                            QmOutputLabel.Content = worksheet.Cell("B9").GetString();
                            QoOutputLabel.Content = worksheet.Cell("B11").GetString();
                            QmOutputLabelSingleVein.Content = worksheet.Cell("B9").GetString();
                            QoOutputLabelSingleVein.Content = worksheet.Cell("B11").GetString();
                            QoPrimeOutputLabel.Content = worksheet.Cell("B13").GetString(); ;
                            QoPrimeOutputLabelSingleVein.Content = worksheet.Cell("B13").GetString();
                        }
                        else if (A > 1)
                        {
                            QmOutputLabel.Content = worksheet.Cell("B10").GetString();
                            QoOutputLabel.Content = worksheet.Cell("B13").GetString();
                            QmOutputLabelSingleVein.Content = worksheet.Cell("B9").GetString();
                            QoOutputLabelSingleVein.Content = worksheet.Cell("B12").GetString();
                            QoPrimeOutputLabel.Content = worksheet.Cell("B16").GetString(); ;
                            QoPrimeOutputLabelSingleVein.Content = worksheet.Cell("B15").GetString();
                        }
                       

                    }
                }
            }
            else
            {
                Console.WriteLine("Nie wybrano pliku. Operacja przerwana.");
            }
        }


        public void WriteExcelData()
        {
            btnCalculate_Click(this, null);
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Pliki Excel (*.xlsx)|*.xlsx";
            saveFileDialog.Title = "Wybierz lokalizację do zapisu pliku Excel";
            saveFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;



                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Arkusz1");

                    worksheet.Clear();

                    worksheet.Column("A").Width = 50;

                    worksheet.Cell("A1").Value = "Nazwa";
                    worksheet.Cell("B1").Value = "Wartość";
                    worksheet.Cell("C1").Value = "Jednostka";

                    worksheet.Cell("A2").Value = "Skala";
                    worksheet.Cell("B2").Value = "'" + s1.ToString() + ":" + s2.ToString();

                    if (rectangleRadioButton.IsChecked == true)
                    {
                        worksheet.Cell("A3").Value = "Długość przekroju odlewanego wlewka";
                        worksheet.Cell("B3").Value = X;
                        worksheet.Cell("C3").Value = "m";

                        worksheet.Cell("A4").Value = "Szerokość przekroju odlewanego wlewka";
                        worksheet.Cell("B4").Value = Y;
                        worksheet.Cell("C4").Value = "m";

                        worksheet.Cell("A5").Value = "Prędkość odlewania";
                        worksheet.Cell("B5").Value = V;
                        worksheet.Cell("C5").Value = "m/min";

                        worksheet.Cell("A6").Value = "Ilość żył";
                        worksheet.Cell("B6").Value = A;

                        worksheet.Cell("A7").Value = "Gęstość stali w stanie ciekłym";
                        worksheet.Cell("B7").Value = Rc;
                        worksheet.Cell("C7").Value = "kg/m^3";

                        worksheet.Cell("A8").Value = "Gęstość stali w stanie stałym";
                        worksheet.Cell("B8").Value = Rs;
                        worksheet.Cell("C8").Value = "kg/m^3";

                        worksheet.Cell("A9").Value = "Przepływ masowy w urządzeniu przemysłowym:";
                        worksheet.Cell("A10").Value = "Jedna żyła";
                        worksheet.Cell("B10").Value = (Qm/A).ToString("F2");
                        worksheet.Cell("C10").Value = "kg/min";
                        if (A == 1)
                        {
                            worksheet.Cell("A11").Value = "Przepływ objętościowy w urządzeniu przemysłowym:";
                            worksheet.Cell("A12").Value = "Jedna żyła";
                            worksheet.Cell("B12").Value = Qo.ToString("F2");
                            worksheet.Cell("C12").Value = "l/min";

                            worksheet.Cell("A13").Value = "Przepływ objętościowy cieczy modelowej:";
                            worksheet.Cell("A14").Value = "Jedna żyła";
                            worksheet.Cell("B14").Value = Qom.ToString("F2");
                            worksheet.Cell("C14").Value = "l/min";


                        }
                        if(A>1)
                        {
                            worksheet.Cell("A11").Value = "Ilość żył: "+A.ToString();
                            worksheet.Cell("B11").Value = Qm.ToString("F2");
                            worksheet.Cell("C11").Value = "kg/min";

                            worksheet.Cell("A12").Value = "Przepływ objętościowy w urządzeniu przemysłowym:";
                            worksheet.Cell("A13").Value = "Jedna żyła";
                            worksheet.Cell("B13").Value = (Qo/A).ToString("F2");
                            worksheet.Cell("C13").Value = "l/min";

                            worksheet.Cell("A14").Value = "Ilość żył: " + A.ToString();
                            worksheet.Cell("B14").Value = Qo.ToString("F2");
                            worksheet.Cell("C14").Value = "l/min";

                            worksheet.Cell("A15").Value = "Przepływ objętościowy cieczy modelowej:";
                            worksheet.Cell("A16").Value = "Jedna żyła";
                            worksheet.Cell("B16").Value = (Qom/4).ToString("F2");
                            worksheet.Cell("C16").Value = "l/min";

                            worksheet.Cell("A17").Value = "Ilość żył: " + A.ToString();
                            worksheet.Cell("B17").Value = Qom.ToString("F2");
                            worksheet.Cell("C17").Value = "l/min";
                        }

                    }
                    if (circleRadioButton.IsChecked == true)
                    {
                        worksheet.Cell("A3").Value = "Promień odlewanego wlewka";
                        worksheet.Cell("B3").Value = R;
                        worksheet.Cell("C3").Value = "m";

                        worksheet.Cell("A4").Value = "Prędkość odlewania";
                        worksheet.Cell("B4").Value = V;
                        worksheet.Cell("C4").Value = "m/min";

                        worksheet.Cell("A5").Value = "Ilość żył";
                        worksheet.Cell("B5").Value = A;

                        worksheet.Cell("A6").Value = "Gęstość stali w stanie ciekłym";
                        worksheet.Cell("B6").Value = Rc;
                        worksheet.Cell("C6").Value = "kg/m^3";

                        worksheet.Cell("A7").Value = "Gęstość stali w stanie stałym";
                        worksheet.Cell("B7").Value = Rs;
                        worksheet.Cell("C7").Value = "kg/m^3";

                        worksheet.Cell("A8").Value = "Przepływ masowy w urządzeniu przemysłowym:";
                        worksheet.Cell("A9").Value = "Jedna żyła";
                        worksheet.Cell("B9").Value = (Qm / A).ToString("F2");
                        worksheet.Cell("C9").Value = "kg/min";
                        if (A == 1)
                        {
                            worksheet.Cell("A10").Value = "Przepływ objętościowy w urządzeniu przemysłowym:";
                            worksheet.Cell("A11").Value = "Jedna żyła";
                            worksheet.Cell("B11").Value = Qo.ToString("F2");
                            worksheet.Cell("C11").Value = "l/min";

                            worksheet.Cell("A12").Value = "Przepływ objętościowy cieczy modelowej:";
                            worksheet.Cell("A13").Value = "Jedna żyła";
                            worksheet.Cell("B13").Value = Qom.ToString("F2");
                            worksheet.Cell("C13").Value = "l/min";


                        }
                        if (A > 1)
                        {
                            worksheet.Cell("A10").Value = "Ilość żył: " + A.ToString();
                            worksheet.Cell("B10").Value = Qm.ToString("F2");
                            worksheet.Cell("C10").Value = "kg/min";

                            worksheet.Cell("A11").Value = "Przepływ objętościowy w urządzeniu przemysłowym:";
                            worksheet.Cell("A12").Value = "Jedna żyła";
                            worksheet.Cell("B12").Value = (Qo / A).ToString("F2");
                            worksheet.Cell("C12").Value = "l/min";

                            worksheet.Cell("A13").Value = "Ilość żył: " + A.ToString();
                            worksheet.Cell("B13").Value = Qo.ToString("F2");
                            worksheet.Cell("C13").Value = "l/min";

                            worksheet.Cell("A14").Value = "Przepływ objętościowy cieczy modelowej:";
                            worksheet.Cell("A15").Value = "Jedna żyła";
                            worksheet.Cell("B15").Value = (Qom / 4).ToString("F2");
                            worksheet.Cell("C15").Value = "l/min";

                            worksheet.Cell("A16").Value = "Ilość żył: " + A.ToString();
                            worksheet.Cell("B16").Value = Qom.ToString("F2");
                            worksheet.Cell("C16").Value = "l/min";
                        }

                    }

                    workbook.SaveAs(filePath);
                }

                Console.WriteLine($"Dane zostały zapisane do pliku Excel w lokalizacji: {filePath}");
            }
            else
            {
                Console.WriteLine("Nie wybrano lokalizacji. Operacja przerwana.");
            }
        }

        private void ChangeUnitsToSI()
        {
            vUnitComboBox.SelectedIndex = 0;
            xUnitComboBox.SelectedIndex = 0;
            yUnitComboBox.SelectedIndex = 0;
            rUnitComboBox.SelectedIndex = 0;
            liquidDensityUnitComboBox.SelectedIndex = 0;
            solidDensityUnitComboBox.SelectedIndex = 0;
            QmUnitComboBox.SelectedIndex = 0;
            QoUnitComboBox.SelectedIndex = 0;
            QoPrimeUnitComboBox.SelectedIndex = 0;
        }

        private void checkInputs()
        {
            if (rectangleRadioButton.IsChecked == true)
            {
                if (xTextBox.Text.Equals("")||yTextBox.Text.Equals("")||vTextBox.Text.Equals("") ||veinsTextBox.Text.Equals("") ||solidDensityTextBox.Text.Equals("") ||liquidDensityTextBox.Text.Equals("") || scaleTextBox1.Text.Equals("")|| scaleTextBox2.Text.Equals(""))
                {
                    btnCalculate.IsEnabled = false;
                }
                else
                {
                    btnCalculate.IsEnabled = true;
                    zapiszBtn.IsEnabled = true;
                    btnSavePdf_Copy.IsEnabled = true;
                }
            }
            else if (circleRadioButton.IsChecked == true)
            {
                if (rTextBox.Text.Equals("") || vTextBox.Text.Equals("") || veinsTextBox.Text.Equals("") || solidDensityTextBox.Text.Equals("") || liquidDensityTextBox.Text.Equals("") || scaleTextBox1.Text.Equals("") || scaleTextBox2.Text.Equals(""))
                {
                    btnCalculate.IsEnabled = false;
                }
                else
                {
                    btnCalculate.IsEnabled = true;
                    zapiszBtn.IsEnabled = true;
                    btnSavePdf_Copy.IsEnabled = true;
                }
            }
        }
    }
   
}