using System;
using System.Windows;

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
        }

        private double Z = 2.0, Zp;

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
                else {
                    this.getR();
                    this.Rp = this.R * skala;
                    przekroj = Math.PI * this.R * this.R;
                    przekrojModelu = Math.PI * this.Rp * this.Rp;
                }

                this.Qm = przekroj * this.V * this.Rc;//m^2*m/min*kg/m^3 = kg/min
                QmOutputLabel.Content = this.Qm;

                this.Qo = (this.Qm / this.Rs) * 1000.0;// (kg/min)/(kg/m^3) * 1000 = l/min
                QoOutputLabel.Content = this.Qo;

                this.Qmm = przekrojModelu * this.Vp * this.Rc;//m^2*m/min*kg/m^3 = kg/min
                QmPrimeOutputLabel.Content = this.Qmm;

                this.Qom = (this.Qmm / this.Rs) * 1000.0;// (kg/min)/(kg/m^3) * 1000 = l/min
                QoPrimeOutputLabel.Content = this.Qom;

                this.Sq = Math.Sqrt(skala);
                SqOutputLabel.Content = this.Sq;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

        }

        private void getV() {
            try
            {
                this.V = Convert.ToDouble(vTextBox.Text);
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

        private void getX()
        {
            try
            {
                this.X = Convert.ToDouble(xTextBox.Text);
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
                this.Y = Convert.ToDouble(yTextBox.Text);
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
                this.R = Convert.ToDouble(rTextBox.Text);
                if (this.R <= 0.0)
                {
                    throw new Exception("Promień nie może być mniejszy bądź równy zeru!");
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Zła dana R");
            }
            catch (Exception ex) { 
                MessageBox.Show(ex.Message); 
            }
        }

        private void getRc()
        {
            try
            {
                this.Rc = Convert.ToDouble(liquidDensityTextBox.Text);
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
                this.Rs = Convert.ToDouble(solidDensityTextBox.Text);
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
                if (this.s1 <= 0.0) {
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
    }
}
