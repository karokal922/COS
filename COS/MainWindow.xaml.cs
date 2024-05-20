﻿using Microsoft.Win32;
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
                QmUnitComboBoxSingleVein.SelectedIndex = 0;
                QoUnitComboBox.SelectedIndex = 0;
                QoUnitComboBoxSingleVein.SelectedIndex = 0;
                QoPrimeUnitComboBox.SelectedIndex = 0;
                QoPrimeUnitComboBoxSingleVein.SelectedIndex = 0;

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
                document.Info.Title = "Shape Calculator Results";

                // Utwórz nową stronę PDF
                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Verdana", 12, XFontStyle.Regular);

                // Załaduj obraz z katalogu Assets
                string imagePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Assets", "logo.jpg");
                XImage xImage = XImage.FromFile(imagePath);

                // Wycentruj i dodaj logo na górze strony
                double logoWidth = 100;
                double logoHeight = 100;
                double logoX = (page.Width - logoWidth) / 2;
                double logoY = 20;
                gfx.DrawImage(xImage, logoX, logoY, logoWidth, logoHeight);

                // Dodaj nagłówek poniżej logo
                gfx.DrawString("Shape Calculator Results", new XFont("Verdana", 20, XFontStyle.Bold),
                    XBrushes.Black, new XRect(0, logoY + logoHeight + 10, page.Width, 40), XStringFormats.TopCenter);

                // Dodaj dane wejściowe
                int yPoint = (int)(logoY + logoHeight + 60);
                gfx.DrawString("Input Data", new XFont("Verdana", 16, XFontStyle.Bold),
                    XBrushes.Black, new XRect(20, yPoint, page.Width, 20), XStringFormats.TopLeft);

                yPoint += 30;
                AddInputDataToPdf(gfx, font, ref yPoint);

                // Dodaj dane wyjściowe
                yPoint += 20;
                gfx.DrawString("Output Data", new XFont("Verdana", 16, XFontStyle.Bold),
                    XBrushes.Black, new XRect(20, yPoint, page.Width, 20), XStringFormats.TopLeft);

                yPoint += 30;
                AddOutputDataToPdf(gfx, font, ref yPoint);

                // Zapisz dokument PDF
                document.Save(filename);
                MessageBox.Show("PDF saved successfully!");
            }
        }

        private void AddInputDataToPdf(XGraphics gfx, XFont font, ref int yPoint)
        {
            XFont boldFont = new XFont("Verdana", 12, XFontStyle.Bold);

            gfx.DrawString("Parameter", boldFont, XBrushes.Black, new XRect(20, yPoint, 200, 20), XStringFormats.TopLeft);
            gfx.DrawString("Value", boldFont, XBrushes.Black, new XRect(220, yPoint, 200, 20), XStringFormats.TopLeft);
            yPoint += 20;

            DrawTableRow(gfx, font, ref yPoint, "X", xTextBox.Text + " " + (xUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
            DrawTableRow(gfx, font, ref yPoint, "Y", yTextBox.Text + " " + (yUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
            DrawTableRow(gfx, font, ref yPoint, "R", rTextBox.Text + " " + (rUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
            DrawTableRow(gfx, font, ref yPoint, "Velocity (V)", vTextBox.Text + " " + (vUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
            DrawTableRow(gfx, font, ref yPoint, "Liquid Density (ρc)", liquidDensityTextBox.Text + " " + (liquidDensityUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
            DrawTableRow(gfx, font, ref yPoint, "Solid Density (ρs)", solidDensityTextBox.Text + " " + (solidDensityUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
            DrawTableRow(gfx, font, ref yPoint, "Veins (A)", veinsTextBox.Text);
            DrawTableRow(gfx, font, ref yPoint, "Scale Left (s1)", scaleTextBox1.Text);
            DrawTableRow(gfx, font, ref yPoint, "Scale Right (s2)", scaleTextBox2.Text);
        }

        private void AddOutputDataToPdf(XGraphics gfx, XFont font, ref int yPoint)
        {
            XFont boldFont = new XFont("Verdana", 12, XFontStyle.Bold);

            gfx.DrawString("Parameter", boldFont, XBrushes.Black, new XRect(20, yPoint, 200, 20), XStringFormats.TopLeft);
            gfx.DrawString("Value", boldFont, XBrushes.Black, new XRect(220, yPoint, 200, 20), XStringFormats.TopLeft);
            yPoint += 20;

            DrawTableRow(gfx, font, ref yPoint, "Qm", QmOutputLabel.Content.ToString());
            DrawTableRow(gfx, font, ref yPoint, "Qo", QoOutputLabel.Content.ToString());
            DrawTableRow(gfx, font, ref yPoint, "Qo'", QoPrimeOutputLabel.Content.ToString());
            //DrawTableRow(gfx, font, ref yPoint, "Sq", SqOutputLabel.Content.ToString());
        }

        private void DrawTableRow(XGraphics gfx, XFont font, ref int yPoint, string parameter, string value)
        {
            gfx.DrawString(parameter, font, XBrushes.Black, new XRect(20, yPoint, 200, 20), XStringFormats.TopLeft);
            gfx.DrawString(value, font, XBrushes.Black, new XRect(220, yPoint, 200, 20), XStringFormats.TopLeft);
            yPoint += 20;
        }

        public void ReadExcelData()
        {
            ChangeUnitsToSI();
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Pliki Excel (*.xlsx)|*.xlsx|Wszystkie pliki (*.*)|*.*";
            openFileDialog.Title = "Wybierz plik Excel";

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
                    if (worksheet.Cell("A3").Value.ToString() == "X")
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
                        QmOutputLabel.Content = worksheet.Cell("B9").GetString();
                        QoOutputLabel.Content = worksheet.Cell("B10").GetString();
                        QoPrimeOutputLabel.Content = worksheet.Cell("B11").GetString();
                        //SqOutputLabel.Content = worksheet.Cell("B12").GetString();
                    }
                    // Jeśli zapisane dane odnoszą się do koła
                    else if (worksheet.Cell("A3").Value.ToString() == "R")
                    {
                        circleRadioButton.IsChecked = true;
                        ShapeRadioButton_Checked(circleRadioButton, null);
                        xTextBox.Text = "";
                        yTextBox.Text = "";
                        rTextBox.Text = worksheet.Cell("B3").GetString();
                        vTextBox.Text = worksheet.Cell("B4").GetString();
                        veinsTextBox.Text = worksheet.Cell("B4").GetString();
                        liquidDensityTextBox.Text = worksheet.Cell("B5").GetString();
                        solidDensityTextBox.Text = worksheet.Cell("B6").GetString();
                        QmOutputLabel.Content = worksheet.Cell("B7").GetString();
                        QoOutputLabel.Content = worksheet.Cell("B8").GetString();
                        QoPrimeOutputLabel.Content = worksheet.Cell("B09").GetString();
                        //SqOutputLabel.Content = worksheet.Cell("B10").GetString();
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

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;



                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Arkusz1");

                    worksheet.Clear();

                    worksheet.Cell("A1").Value = "Nazwa";
                    worksheet.Cell("B1").Value = "Wartość";
                    worksheet.Cell("C1").Value = "Jednostka";

                    worksheet.Cell("A2").Value = "S";
                    worksheet.Cell("B2").Value = "'" + s1.ToString() + ":" + s2.ToString();

                    if (rectangleRadioButton.IsChecked == true)
                    {
                        worksheet.Cell("A3").Value = "X";
                        worksheet.Cell("B3").Value = X;
                        worksheet.Cell("C3").Value = "m";

                        worksheet.Cell("A4").Value = "Y";
                        worksheet.Cell("B4").Value = Y;
                        worksheet.Cell("C4").Value = "m";

                        worksheet.Cell("A5").Value = "V";
                        worksheet.Cell("B5").Value = V;
                        worksheet.Cell("C5").Value = "m/min";

                        worksheet.Cell("A6").Value = "a";
                        worksheet.Cell("B6").Value = A;

                        worksheet.Cell("A7").Value = "ρc";
                        worksheet.Cell("B7").Value = Rc;
                        worksheet.Cell("C7").Value = "kg/m^3";

                        worksheet.Cell("A8").Value = "ρs";
                        worksheet.Cell("B8").Value = Rs;
                        worksheet.Cell("C8").Value = "kg/m^3";

                        worksheet.Cell("A9").Value = "Qm";
                        worksheet.Cell("B9").Value = Qm.ToString("F2");
                        worksheet.Cell("C9").Value = "kg/min";

                        worksheet.Cell("A10").Value = "Qo";
                        worksheet.Cell("B10").Value = Qo.ToString("F2");
                        worksheet.Cell("C10").Value = "l/min";

                        worksheet.Cell("A11").Value = "Qo'";
                        worksheet.Cell("B11").Value = Qom.ToString("F2");
                        worksheet.Cell("C11").Value = "l/min";

                        worksheet.Cell("A12").Value = "Sq";
                        worksheet.Cell("B12").Value = Sq.ToString("F2");

                    }
                    if (circleRadioButton.IsChecked == true)
                    {
                        worksheet.Cell("A3").Value = "R";
                        worksheet.Cell("B3").Value = R;
                        worksheet.Cell("C3").Value = "m";

                        worksheet.Cell("A4").Value = "V";
                        worksheet.Cell("B4").Value = V;
                        worksheet.Cell("C4").Value = "m/min";

                        worksheet.Cell("A4").Value = "a";
                        worksheet.Cell("B4").Value = A;

                        worksheet.Cell("A5").Value = "ρc";
                        worksheet.Cell("B5").Value = Rc;
                        worksheet.Cell("C5").Value = "kg/m^3";

                        worksheet.Cell("A6").Value = "ρs";
                        worksheet.Cell("B6").Value = Rs;
                        worksheet.Cell("C6").Value = "kg/m^3";

                        worksheet.Cell("A7").Value = "Qm";
                        worksheet.Cell("B7").Value = Qm.ToString("F2");
                        worksheet.Cell("C7").Value = "kg/min";

                        worksheet.Cell("A8").Value = "Qo";
                        worksheet.Cell("B8").Value = Qo.ToString("F2");
                        worksheet.Cell("C8").Value = "l/min";

                        worksheet.Cell("A09").Value = "Qo'";
                        worksheet.Cell("B09").Value = Qom.ToString("F2");
                        worksheet.Cell("C09").Value = "l/min";

                        worksheet.Cell("A10").Value = "Sq";
                        worksheet.Cell("B10").Value = Sq.ToString("F2");

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
    }
   
}