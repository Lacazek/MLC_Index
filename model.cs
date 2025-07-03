using System.Windows;
using VMS.TPS.Common.Model.API;
using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;

namespace MLC_Index

{
    internal class model
    {

        private ScriptContext _context;
        private List<Tuple<string, double>> _index;
        private int _numberOfIndice;
        private string _path;

        public string output;

        public model(ScriptContext context)
        {
            _context = context;
            _index = new List<Tuple<string, double>>();
            _numberOfIndice = 5;
            _path = @"B:\RADIOTHERAPIE\Killian\Dosi\Script\MLC_Index";

            CalculateIndice();
            Print();
        }


        internal void Print()
        {
            string filename = "MLC_Index.csv";

            StreamWriter sw = new StreamWriter(Path.Combine(_path, filename));
            sw.WriteLine("nom;prénom;IPP;course;plan;dose totale;nb de fraction;MU;Facteur de modulation;EQF;MI;Overtravel,MCSv");
            sw.WriteLine("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11}",
            _context.Patient.Name, _context.Patient.FirstName, _context.Patient.Id, _context.PlanSetup.Course.Name, _context.PlanSetup.Name, _context.PlanSetup.TotalDose.Dose, _context.PlanSetup.NumberOfFractions, _index[0].Item2, _index[1].Item2, _index[2].Item2, _index[3].Item2, _index[4].Item2, _index[5].Item2);
            sw.Close();

            Resultats resultats = new Resultats(output);
            resultats.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            resultats.ShowDialog();
        }

        internal void CalculateIndice()
        {
            var progressWindow = new ProgressWindow(_numberOfIndice);
            progressWindow.Show();
            int calculationstep = 1;

            progressWindow.UpdateProgress((int)(((double)(calculationstep) / _numberOfIndice + 1) * 100), "Calcul des MU...");
            CalculateMU();

            calculationstep++;
            progressWindow.UpdateProgress((int)(((double)(calculationstep) / _numberOfIndice + 1) * 100), "Calcul du champ carré équivalent...");
            CalculateEquivalentSquareField();

            calculationstep++;
            progressWindow.UpdateProgress((int)(((double)(calculationstep) / _numberOfIndice + 1) * 100), "Calcul de la vitesse du MLC...");
            CalculateMI();

            calculationstep++;
            progressWindow.UpdateProgress((int)(((double)(calculationstep) / _numberOfIndice + 1) * 100), "Calcul de l'Overtravel...");
            CalculateOverTravel();

            calculationstep++;
            progressWindow.UpdateProgress((int)(((double)(calculationstep) / _numberOfIndice + 1) * 100), "Calcul du MCSv");
            CalculateMCSv();

            calculationstep++;
            progressWindow.UpdateProgress((int)(((double)(calculationstep) / _numberOfIndice + 1) * 100), "Calcul des");

            progressWindow.Close();
        }

        #region UM
        internal void CalculateMU()
        {
            double UM = 0.0;
            foreach (var b in _context.PlanSetup.Beams)
            {
                if (!b.IsSetupField)
                    UM = Math.Round(UM + b.Meterset.Value, 2);
            }
            AddIndex = new Tuple<string, double>("UM", UM);
            AddIndex = new Tuple<string, double>("UM/cGy", UM / ((_context.PlanSetup.TotalDose.Dose / (int)_context.PlanSetup.NumberOfFractions) * 100));
            output += $"UM: {_index[0].Item2} UM\n";
            output += $"Facteur de modulation : {Math.Round(_index[1].Item2, 3)} UM / cGy\n";

        }
        #endregion
        #region Equivalent Square Field
        internal void CalculateEquivalentSquareField()
        {
            double EQF = 0.0;
            double leafWidthHal = 0.5;

            foreach (var b in _context.PlanSetup.Beams)
            {
                if (!b.IsSetupField)
                {
                    foreach (var cp in b.ControlPoints)
                    {
                        var leafposition = cp.LeafPositions;
                        for (int bank = 0; bank < 2; bank++)
                        {
                            for (int leaf = 0; leaf < leafposition.GetLength(1); leaf++)
                            {
                                double pos1 = leafposition[0, leaf];
                                double pos2 = leafposition[1, leaf];

                                double leafGap = Math.Abs(pos2 - pos1);
                                EQF += (leafGap * leafWidthHal) / 100.0 / 100.0;

                            }
                        }
                    }
                }
            }

            AddIndex = new Tuple<string, double>("Equivalent Square Field", EQF);
            output += $"Champ carré equivalent : {Math.Round(_index[2].Item2, 3)} cm²\n";

        }
        #endregion
        #region MI
        internal void CalculateMI()
        {
            double MLCSpeed = 0.0;
            double GantrySpeed = 6.000;

            foreach (var b in _context.PlanSetup.Beams)
            {
                if (!b.IsSetupField)
                {
                    for (int i = 1; i < b.ControlPoints.Count; i++)
                    {
                        var cp1 = b.ControlPoints[i - 1];
                        var cp2 = b.ControlPoints[i];

                        float[,] leaf_position1 = cp1.LeafPositions;
                        float[,] leaf_position2 = cp2.LeafPositions;

                        for (int bankindex = 0; bankindex <= leaf_position1.GetUpperBound(0); bankindex++)
                        {
                            for (int leafindex = 1; leafindex <= leaf_position1.GetUpperBound(1); leafindex++)
                            {
                                MLCSpeed = Math.Abs(leaf_position1[1, leafindex] - leaf_position2[0, leafindex]) / ((cp2.MetersetWeight - cp1.MetersetWeight) * b.Meterset.Value) / GantrySpeed; // à modifier par le dose rate par cp
                                                                                                                                                                                                 //MLCSpeed = Math.Abs(leaf_position1[1, leafindex] - leaf_position2[0, leafindex]) / ((cp2.MetersetWeight - cp1.MetersetWeight) * b.Meterset.Value) / b.DoseRate; // à modifier par le dose rate par cp
                            }
                        }
                    }
                }
            }

            //double Z_mlcspeed = (1 / (b.ControlPoints.Count - 1))
            AddIndex = new Tuple<string, double>("MLC speed", Math.Round(MLCSpeed,3));
            output += $"MI : {_index[3].Item2}\n";

        }
        #endregion
        #region OverTravel
        internal void CalculateOverTravel()
        {
            double Overtravel = 0.0;
            double[] MaxX1, MaxX2;

            // a fixer
            const double thresholdA = 100;
            const double thresholdB = 100;

            foreach (var b in _context.PlanSetup.Beams)
            {

                MaxX1 = new double[_context.PlanSetup.Beams.Where(x => x.IsSetupField.Equals(false)).Count()];
                MaxX2 = new double[_context.PlanSetup.Beams.Where(x => x.IsSetupField.Equals(false)).Count()];
            }

            foreach (var b in _context.PlanSetup.Beams)
            {
                foreach (var cp in b.ControlPoints)
                {
                    float[,] leaf_position = cp.LeafPositions;

                    for (int bankindex = 0; bankindex <= leaf_position.GetUpperBound(0); bankindex++)
                    {
                        for (int leafindex = 0; leafindex <= leaf_position.GetUpperBound(1); leafindex++)
                        {

                            // a étudier ici
                            /*if (leafindex[] > thresholdA && bankindex == 0)
                                Overtravelling++; 
                            else if (Math.Abs(pos) > thresholdB && bankindex == 1)
                                Overtravelling++;*/
                        }
                    }
                }
            }
            AddIndex = new Tuple<string, double>("Overtravelling Banc", Overtravel);
            output += $"Overtravel : {Math.Round(_index[4].Item2,3)}\n";
        }
        #endregion
        #region MCSv
        internal void CalculateMCSv()
        {
            double MCSv = 0.0;
            double Leaf_Width = 5; // mm;
            double sumMetersetWeight=0.0;

            foreach (var b in _context.PlanSetup.Beams)
            {
                if (!b.IsSetupField)
                {
                    sumMetersetWeight += b.ControlPoints.Last().MetersetWeight;
                    double previousMetersetWeight = 0.00;

                    foreach (var cp in b.ControlPoints)
                    {
                        var mlcPositions = cp.LeafPositions;

                        double maxposA = Enumerable.Range(0, mlcPositions.GetLength(1)).Max(c => mlcPositions[1, c]);
                        double maxposB = Enumerable.Range(0, mlcPositions.GetLength(1)).Max(c => mlcPositions[0, c]);

                        List<double> Openings = new List<double>();
                        double EffectiveSurface = 0.00; 

                        for (int i = 1; i < mlcPositions.GetLength(1)-1; i++)
                        {
                            EffectiveSurface += (Math.Max(0.0, mlcPositions[1, i-1] - mlcPositions[0, i - 1])/(maxposA-maxposB)) * Leaf_Width;
                            Openings.Add(Math.Max(0.0, mlcPositions[0, i-1] - mlcPositions[0, i])* Math.Max(0.0, mlcPositions[1, i - 1] - mlcPositions[1, i]));
                        }
                        //MessageBox.Show((EffectiveSurface * (cp.MetersetWeight - previousMetersetWeight)).ToString()+ "    " + (cp.MetersetWeight - previousMetersetWeight).ToString() +"    " + ComputeVG(Openings.ToArray()).ToString());
                        MCSv += EffectiveSurface * Math.Abs(cp.MetersetWeight -previousMetersetWeight) * ComputeVG(Openings.ToArray());
                        previousMetersetWeight = cp.MetersetWeight;
                    }
                }
            }
            MCSv /= sumMetersetWeight;

            AddIndex = new Tuple<string, double>("MCSv", MCSv);
            output += $"MCSv : {Math.Round(_index[5].Item2, 3)}\n";
        }

        double ComputeVG(double[] openings)
        {
            double vgSum = 0.0;
            double epsilon = 0.01;
            int count = openings.Length;

            for (int i = 0; i < count - 1; i++)
            {
                double d1 = openings[i];
                double d2 = openings[i + 1];
                double maxD = Math.Max(Math.Max(d1, d2), epsilon);

                double diff = Math.Abs(d1 - d2);
                vgSum += (1.0 - (diff / maxD));
            }
            return vgSum / (count - 1);
        }
        #endregion


        internal Tuple<string, double> AddIndex
        {
            set { _index.Add(value); }
        }
    }
}
