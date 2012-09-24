using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows;
using WPFLocalizeExtension.Extensions;
using System.Collections.ObjectModel;
using Parser;
using MatrixLibrary;
using VianaNET.Data.Linefit;

using System.Collections;

//  Autor: H. Niemeyer
//  letzte �nderung: 24.9.2012

namespace VianaNET
{
    
    public class LineFitClass
    {
        ///////////////////////////////////////////////////////////////////////////////
        // Defining Constants                                                        //
        ///////////////////////////////////////////////////////////////////////////////
        #region CONSTANTS
          static int maxIteration = 50;          // Begrenzungswerte f�r die internen Berechnungen
          static double startAbw = 1E150;
          static double genauigkeit = 1E-10;
          static double minStep = 1E-6;
        #endregion //CONSTANTS

        ///////////////////////////////////////////////////////////////////////////////
        // Defining Variables, Enumerations, Events                                  //
        ///////////////////////////////////////////////////////////////////////////////
        #region FIELDS
          private int regTyp,                          // Kennzahl f�r den Typ der Ausgleichsfunktion
                      anzahl;                          // Anzahl der Wertepaare, die f�r die Berechnungen der Ausgleichsfunktion benutzt werden
          private long startTime;                                       // erster Zeitwert der Daten (in ms)
          private double startX, endX, startPixelX, endPixelX, stepX;
          private string myG = "G4";                                   // Formatstring f�r anzuzeigende Stellenzahl
          private string _LineFitFktStr;                  // Ausgabestring f�r die Ausgleichsfunktion
          private double _LineFitAbweichung;              // Wert der mittleren Abweichung der Messpunkte von der Ausgleichsfunktion
          private int xNr, yNr, NumberOfObject;           // Spaltennummer des 1. bzw. 2. Wertes; Nummer des betrachteten Objekts
          private static double[] param;                                             // Parameter einer Ausgleichsfunktionen
          private static MatrixLibrary.Matrix p = new MatrixLibrary.Matrix(10, 6);   // Parameter der Ausgleichsfunktion
          private DataCollection orgDataSamples;        // orginale Videodaten, wie sie bei Daten angezeigt werden

          public DataCollection LineFitDisplaySample;   // mit der Ausgleichsfunktion berechnete Punkte, die sich in der x-Koordinate um einen Pixelabstand unterscheiden
          public DataCollection TheorieDisplaySample;   // analog f�r theoretische Funktion
         

          public List<double> wertX, wertY;                    // aus den Videodaten herausgelesene Messpaare - auf zwei Arrays aufgeteilt, Grunddaten der Berechnung der Ausgleichsfunktion 
          public delegate double AusgleichFunction(double x);  // Berechnet zu einem Wert den Funktionswert mit Hilfe der Ausgleichsfunktion
          //   private Parser.TFktTerm userFkt;

        #endregion //FIELDS

        ///////////////////////////////////////////////////////////////////////////////
        // Construction and Initializing methods                                     //
        ///////////////////////////////////////////////////////////////////////////////      
       
        public LineFitClass(DataCollection aktSamples, int aktObjectNr, int aktxNr, int aktyNr)
        {     
            regTyp = Constants.linReg;
            orgDataSamples = null;
            param = new double[3];           
            wertX = new List<double>();
            wertY = new List<double>();
            ExtractDataColumnsFromVideoSamples(aktSamples, NumberOfObject, aktxNr, aktyNr);
            LineFitDisplaySample = null;
            TheorieDisplaySample = null;
        }

        ///////////////////////////////////////////////////////////////////////////////
        // Defining Properties                                                       //
        ///////////////////////////////////////////////////////////////////////////////
        #region PROPERTIES
        public int regressionTyp
        {
            get { return regTyp; }
            set
            {
                if ((regTyp != value) && (value>=Constants.minRegWert) && (value<=Constants.maxRegWert) )
                {
                    regTyp = value;
                    if (wertX.Count > 0)                  // sind Datenreihen ausgew�hlt ?
                    {
                        CalculateLineFitFunction(value);  // neu berechnen !
                    }
                }
            }
        }

        public string gueltigeStellenFormatString
        {
           get { return myG; }
           set 
           {
               myG = value;
               if (aktFunc != null) { _LineFitFktStr = GetRegressionFunctionString(regTyp); }
           }
        }

        public string LineFitFktStr                  // Ausgabestring f�r die Ausgleichsfunktion; nur read-only
        {
            get { return _LineFitFktStr; }
        }

        public double LineFitAbweichung             // mittlere Abweichung der Ausgleichsfunktion bzgl. der Messwerte; nur read-only
        {
            get { return _LineFitAbweichung; }
        }

        #endregion //PROPERTIES


        public void ExtractDataColumnsFromVideoSamples(DataCollection aktSamples, int aktObjectNr, int aktxNr, int aktyNr)
        {
            if ((orgDataSamples != aktSamples) || (NumberOfObject != aktObjectNr) || (xNr != aktxNr) || (yNr != aktyNr) || (wertX.Count == 0))
            {
                orgDataSamples = aktSamples;
                NumberOfObject = aktObjectNr;
                xNr = aktxNr;
                yNr = aktyNr;
                CopySampleColumnsToArrays(aktObjectNr, aktxNr, aktyNr);
            }
        }

        private void CopySampleColumnsToArrays(int aktObjectNr,int aktxNr, int aktyNr)
        {
            DataSample aktDataSample;
            int firstPossibleValueNr = 0;

            if (wertX != null)
            {
                wertX.Clear();
                wertY.Clear();
            }
            anzahl = 0;           
            if ((aktxNr == 2) & (aktyNr == 3))     // x-y-Diagramm
            {
                foreach (TimeSample sample in orgDataSamples)
                {
                    aktDataSample = sample.Object[aktObjectNr];
                    if (anzahl == 0) { startPixelX = aktDataSample.PixelX; }
                    wertX.Add(aktDataSample.PositionX);
                    wertY.Add(aktDataSample.PositionY);
                    endPixelX = aktDataSample.PixelX;
                    anzahl++;
                }
                if (anzahl > 0) { startX = wertX[firstPossibleValueNr]; endX = wertX[anzahl - 1]; }
                else { startX = 0; endX = 0; }
            }
            else                               // t-?-Diagramm
            {
                if (aktyNr >= 7)               // t-a-Diagramm,                                               
                { firstPossibleValueNr = 2; }  // erst der dritte Wert f�r die Beschleunigung ist definiert - Z�hlung startet bei 0
                else
                {
                    if (aktyNr >= 4) { firstPossibleValueNr = 1; }  // t-v-Diagramm, erst der zweite Wert f�r die Beschleunigung ist definiert
                }
              
                startTime = 0;                 // sonst meckert der Compiler
                foreach (TimeSample sample in orgDataSamples)
                {
                    if (anzahl >= firstPossibleValueNr)
                    {
                        aktDataSample = sample.Object[aktObjectNr];
                        if (anzahl == firstPossibleValueNr) { startPixelX = aktDataSample.PixelX; startTime = sample.Timestamp - 40; }
               //im Diagramm ist die kleinste Zeit bei 1 und �ndert sich in 1er-Schritten, (realer) zeitlicher Abstand betr�gt 40 ms
                        wertX.Add((sample.Timestamp-startTime));                         
                        endPixelX = aktDataSample.PixelX;
                        switch (aktyNr)
                        {
                            case 2: wertY.Add(aktDataSample.PositionX); break;
                            case 3: wertY.Add(aktDataSample.PositionY); break;
                            case 4: wertY.Add(aktDataSample.Velocity.Value); break;
                            case 5: wertY.Add(aktDataSample.VelocityX.Value); break;
                            case 6: wertY.Add(aktDataSample.VelocityY.Value); break;
                            case 7: wertY.Add(aktDataSample.Acceleration.Value); break;
                            case 8: wertY.Add(aktDataSample.AccelerationX.Value); break;
                            case 9: wertY.Add(aktDataSample.AccelerationY.Value);  break;
                        }
                    }
                    anzahl++;
                }
                anzahl = wertX.Count;
                if (anzahl > 0) { startX = wertX[0]; endX = wertX[anzahl - 1]; }
                else { startX = 0; endX = 0; }
            }
           
            if (startPixelX > endPixelX)
            {
                startX = wertX[anzahl - 1]; endX = wertX[firstPossibleValueNr];
                double hilf = startPixelX; startPixelX = endPixelX; endPixelX = hilf;
            }
            stepX = (endX - startX) / (endPixelX - startPixelX);  // Schrittweite zum Ausf�llen der Zwischenr�ume ( in x-Achsen-Richtung )
        }


        private void CreateSampleFromCalculatedPoints(int aktObjectNr, int aktxNr, int aktyNr, List<Point> pointList, DataCollection samples)
        {
            DataSample aktDataSample;
            TimeSample timeHilf;
            Point p;
            int k,j;

            samples.Clear();
            
            if ((aktxNr == 2) & (aktyNr == 3))  // Ausgleichskurve f�r x-y-Diagramm vorbereiten
            {
                for (k = 0; k < pointList.Count(); k++)  //Punkte im PixelAbstand (waagerecht) werden mit der Ausgleichsfunktion bestimmt.
                {
                    timeHilf = new TimeSample();
                    timeHilf.Framenumber = k;
                    for (j = 0; j < Video.Instance.ImageProcessing.NumberOfTrackedObjects; j++)
                    { timeHilf.Object[j] = new DataSample(); }
                    aktDataSample = timeHilf.Object[aktObjectNr];
                    p = pointList[k];
                    aktDataSample.PositionX = p.X;
                    aktDataSample.PositionY = p.Y;
                    samples.Add(timeHilf);               
                } 
           }
            else                   // Ausgleichskurve f�r t-?-Diagramm vorbereiten
            {              
                for (k = 0; k < pointList.Count(); k++)  //Punkte im PixelAbstand (waagerecht) werden mit der Ausgleichsfunktion bestimmt.
                {
                    timeHilf = new TimeSample();
                    for (j = 0; j < Video.Instance.ImageProcessing.NumberOfTrackedObjects; j++)
                    { 
                        timeHilf.Object[j] = new DataSample(); 
                    }
                    aktDataSample = timeHilf.Object[aktObjectNr];
                    p = pointList[k];
                    timeHilf.Timestamp = (long)(p.X + startTime);                   
                    switch (aktyNr)
                        {
                            case 2: aktDataSample.PositionX=p.Y; break;
                            case 3: aktDataSample.PositionY=p.Y; break;
                            case 4: aktDataSample.VelocityI=p.Y; break;
                            case 5: aktDataSample.VelocityXI=p.Y; break;
                            case 6: aktDataSample.VelocityYI=p.Y; break;
                            case 7: aktDataSample.AccelerationI=p.Y; break;
                            case 8: aktDataSample.AccelerationXI=p.Y; break;
                            case 9: aktDataSample.AccelerationYI=p.Y; break;
                        }
                    samples.Add(timeHilf);
                    }
                
                } 
        }
               

        public void getMinMax(List<double> werte, out double Min, out double Max)
        {
          //  int k;
          //  double hilf;
            if (werte.Count == 0) { Min = 0; Max = 0; return; }  // Falls noch keine Werte vorliegen
            Min = werte.Min();
            Max = werte.Max();
    /*          Min = werte[0];
            Max = Min;
            for (k = 1; k < anzahl; k++)
            {
                hilf = werte[k];
                if (Min > hilf) { Min = hilf; }
                else if (Max < hilf) { Max = hilf; }
            }
     */ 
        }

        public static double AusgleichsGerade(double x) { return p[Constants.linReg, 0] * x + p[Constants.linReg, 1]; }
        public static double AusgleichsExpSpez(double x) { return p[Constants.expSpezReg, 0] * Math.Exp(x * p[Constants.expSpezReg, 1]); }
        public static double AusgleichsLog(double x) { return p[Constants.logReg, 0] * Math.Log(x * p[Constants.logReg, 1]); }
        public static double AusgleichsPot(double x) { return p[Constants.potReg, 0] * Math.Pow(x, p[Constants.potReg, 1]); }
        public static double AusgleichsParabel(double x) { return (p[Constants.quadReg, 0] * x + p[Constants.quadReg, 1]) * x + p[Constants.quadReg, 2]; }
        public static double AusgleichsExp(double x) { return p[Constants.expReg, 0] * Math.Exp(x * p[Constants.expReg, 1]) + p[Constants.expReg, 2]; }
        public static double AusgleichsSin(double x) { return p[Constants.sinReg, 0] * Math.Sin(x * p[Constants.sinReg, 1] + p[Constants.sinReg, 2]) + p[Constants.sinReg, 3]; }
        public static double AusgleichsSinExp(double x) { return p[Constants.sinExpReg, 0] * Math.Sin(x * p[Constants.sinExpReg, 1]) * Math.Exp(x * p[Constants.sinExpReg, 2]); }
        public static double AusgleichsReso(double x) { return p[Constants.resoReg, 0] / Math.Pow(1 + p[Constants.resoReg, 1] * Math.Pow(x - p[Constants.resoReg, 2] / x, 2), 0.5); }
        public static double NullFkt(double x) { return 0; }

        public AusgleichFunction aktFunc;
      //  private int aktAuswahl;

        public string GetRegressionFunctionString(int regTyp)
        {
            double a = p[regTyp, 0];
            double b = p[regTyp, 1];
            double c = p[regTyp, 2];
            double d = p[regTyp, 3];
            string fktStr;
            if (aktFunc != null)
            {
                switch (regTyp)
                {
                    case Constants.linReg:
                        fktStr = string.Concat(a.ToString(myG), "*x + ", b.ToString(myG));
                        break;
                    case Constants.expSpezReg:
                        fktStr = string.Concat(a.ToString(myG), "*exp(", b.ToString(myG), "*x)");
                        break;
                    case Constants.logReg:
                        fktStr = string.Concat(a.ToString(myG), "*ln(", b.ToString(myG), "*x)");
                        break;
                    case Constants.potReg:
                        fktStr = string.Concat(a.ToString(myG), "*x^", b.ToString(myG));
                        break;
                    case Constants.quadReg:
                        fktStr = string.Concat(a.ToString(myG), "x� + ", b.ToString(myG), "x + ", c.ToString(myG));
                        break;
                    case Constants.expReg:
                        fktStr = string.Concat(a.ToString(myG), "*exp(", b.ToString(myG), "*x) + ", c.ToString(myG));
                        break;
                    case Constants.sinReg:
                        fktStr = string.Concat(a.ToString(myG), "*Sin(", b.ToString(myG), "*x + ", c.ToString(myG), ") + ", d.ToString(myG));
                        aktFunc = AusgleichsSin;
                        break;
                    case Constants.sinExpReg:
                        fktStr = string.Concat(a.ToString(myG), "*Sin(", b.ToString(myG), "*x )*exp( ", c.ToString(myG), "*x)");
                        aktFunc = AusgleichsSinExp;
                        break;
                    case Constants.resoReg:
                        fktStr = string.Concat(a.ToString(myG), "/Sqrt( 1 +", b.ToString(myG), "*( x - ", c.ToString(myG), "/x)� )");
                        break;
                    default: fktStr = " - "; aktFunc = NullFkt; break;
                }
            }
            else { fktStr = " - "; }
            return fktStr;
        }

        private void GetRegressionFunctionStringAndAverageError(int regTyp, double fehler)
        {
            double yi;
            int k;
          
            switch (regTyp)
            {
                case Constants.linReg:     aktFunc = AusgleichsGerade;  break;
                case Constants.expSpezReg: aktFunc = AusgleichsExpSpez; break;
                case Constants.logReg:     aktFunc = AusgleichsLog;     break;
                case Constants.potReg:     aktFunc = AusgleichsPot;     break;
                case Constants.quadReg:    aktFunc = AusgleichsParabel; break;
                case Constants.expReg:     aktFunc = AusgleichsExp;     break;
                case Constants.sinReg:     aktFunc = AusgleichsSin;     break;
                case Constants.sinExpReg:  aktFunc = AusgleichsSinExp;  break;
                case Constants.resoReg:    aktFunc = AusgleichsReso;    break;
                default: aktFunc = NullFkt; break;
            }
            _LineFitFktStr = GetRegressionFunctionString(regTyp);
            if (aktFunc != NullFkt)    // Berechnung der Ausgleichsfunktion erfolgreich?
            {
                if (LineFitDisplaySample == null)
                {
                    LineFitDisplaySample = new DataCollection();
                }
                CalculateLineFitSeries(LineFitDisplaySample);  // Punkte mit Ausgleichsfunktion bestimmen

                if (fehler < -1.5) { fehler = -2; }
                else
                {
                    if (fehler < 0)
                    {
                        fehler = 0;
                        for (k = 0; k < anzahl; k++)
                        {
                            yi = aktFunc(wertX[k]) - wertY[k];
                            fehler = fehler + yi * yi;
                        }
                        fehler = fehler / anzahl;
                    }
                }
            }
            else { fehler = -2; }
            _LineFitAbweichung = fehler;
        }

        private void CalculateLineFitSeries( DataCollection lineFitSamples)
        {
            int k, anzahlPixel;
            double x;
            Point p;
            List<Point> tempLineFitPoints;
           
            if (aktFunc==null)
            { 
                return;
            }
            tempLineFitPoints = new List<Point>();
            if ( regTyp == Constants.linReg )  //Sonderfall lineare Regression; Anzahl der Berechnungen wird drastisch reduziert,                                  
            {                                  // da Chart selbst Geraden zeichnen kann. 
                if (xNr == 2)
                {                              // zwei Punkte gen�gen bei x-y-Diagramm
                    x = wertX[0];              // wertX[] - originale x-Werte der Wertepaare 
                    p = new Point(x, aktFunc(x));
                    tempLineFitPoints.Add(p);
                    x = wertX[anzahl - 1];
                    p = new Point(x, aktFunc(x));
                    tempLineFitPoints.Add(p);
                }
                else
                {                              // Workaround beim t-?-Diagramm: gleichviele Punkte wie bei Originalwerten und gleiche x Werte. 
                    for (k = 0; k < anzahl; k++)
                    {
                        x = wertX[k];
                        p = new Point(x, aktFunc(x));
                        tempLineFitPoints.Add(p);
                    }
                }
            }
            else
            {
                // endPixelX und startPixelX
                // startX,endX und stepX wurden in aktualisiereTab(int aktObjectNr,int aktxNr, int aktyNr) bestimmt
               anzahlPixel = (int)(endPixelX - startPixelX);  //Anzahl der Pixel im betrachtenen Bereich
                x = startX;
                for (k = 0; k < anzahlPixel; k++)  //Punkte im PixelAbstand (waagerecht) werden mit der Ausgleichsfunktion bestimmt.
                {                                  //f�hrt bei t-?-Diagrammen zu falschen Darstellungen !!   
                    p = new Point(x, aktFunc(x));
                    tempLineFitPoints.Add(p);
                    x = x + stepX;
                }
       /* 
        * Versuch, Zwischenpunkte zu erhalten, die die gleichen t-Koordinaten wie die Originalpunkte haben. Auch kein Erfolg 
        * 
                int nr = 1;
                anzahlPixel = (int)(endPixelX - startPixelX);  //Anzahl der Pixel im betrachtenen Bereich
                if (wertX[0] < wertX[anzahl-1]) 
                 {
                     if (stepX < 0) { stepX = -stepX; }
                     x = wertX[0];
                     for (k = 0; k < anzahlPixel; k++)  //Punkte im PixelAbstand (waagerecht) werden mit der Ausgleichsfunktion bestimmt.
                     {                                  //f�hrt bei t-?-Diagrammen zu falschen Darstellungen !!   
                         p = new Point(x, aktFunc(x));
                         tempLineFitPoints.Add(p);
                         x = x + stepX;
                         if (nr<anzahl)
                         {
                             if (x > wertX[nr]) { x = wertX[nr]; nr = nr + 1; }
                         }
                     }
                }
                else
                {
                    if (stepX < 0) { stepX = -stepX; }
                    nr = anzahl - 2;
                    x = wertX[anzahl - 1];
                    for (k = 0; k < anzahlPixel; k++)  //Punkte im PixelAbstand (waagerecht) werden mit der Ausgleichsfunktion bestimmt.
                    {                                  //f�hrt bei t-?-Diagrammen zu falschen Darstellungen !!   
                        p = new Point(x, aktFunc(x));
                        tempLineFitPoints.Add(p);
                        x = x - stepX;
                        if (nr > 0)
                        {
                            if (x < wertX[nr]) { x = wertX[nr]; nr = nr - 1; }
                        }

                    }
                }
        */ 
            }
            CreateSampleFromCalculatedPoints(NumberOfObject, xNr, yNr, tempLineFitPoints, lineFitSamples);
            tempLineFitPoints.Clear();      
        }

        public void CalculateLineFitTheorieSeries(DataCollection TheorieSamples, TFktTerm fx)
        {
            int k, anzahlPixel;
            double x;
            Point p = new Point();
            List<Point> tempTheoriePoints;
            Parse tempParser = new Parse();
                        
            if (fx==null)
            { 
                return;
            }
            tempTheoriePoints = new List<Point>();
            if (tempParser.isLinearFunction(fx))
            {
                if (xNr == 2)
                {                              // zwei Punkte gen�gen bei x-y-Diagramm
                    x = wertX[0];              // wertX[] - originale x-Werte der Wertepaare 
                    p = new Point(x, tempParser.FreierFktWert(fx, x));
                    tempTheoriePoints.Add(p);
                    x = wertX[anzahl - 1];
                    p = new Point(x, tempParser.FreierFktWert(fx, x));
                    tempTheoriePoints.Add(p);
                }
                else
                {                              // Workaround beim t-?-Diagramm: gleichviele Punkte wie bei Originalwerten und gleiche x Werte. 
                    for (k = 0; k < anzahl; k++)
                    {
                        x = wertX[k];
                        p = new Point(x, tempParser.FreierFktWert(fx, x));
                        tempTheoriePoints.Add(p);
                    }
                }
            }
            else
            {
                // endPixelX und startPixelX
                // startX und endX wurden in aktualisiereTab(int aktObjectNr,int aktxNr, int aktyNr) bestimmt
                anzahlPixel = (int)(endPixelX - startPixelX);
                x = startX;

                for (k = 0; k < anzahlPixel; k++)   //Punkte im PixelAbstand (waagerecht) werden mit der theoretischen Funktion bestimmt.
                {                                   //f�hrt bei t-?-Diagrammen zu falschen Darstellungen !!
                    p = new Point(x, tempParser.FreierFktWert(fx, x));
                    tempTheoriePoints.Add(p);
                    x = x + stepX;
                }
            }
            CreateSampleFromCalculatedPoints(NumberOfObject, xNr, yNr, tempTheoriePoints, TheorieSamples);
            tempTheoriePoints.Clear();
        }

        public void CalculateLineFitFunction(int regressionTyp)
        {
            if (wertX.Count == 0) { CopySampleColumnsToArrays(0, xNr, yNr); }
            regTyp = regressionTyp;
            if (anzahl > 2)
            {
                switch (regressionTyp)
                {
                    case Constants.linReg:      BestimmeLinFkt();      break;
                    case Constants.expSpezReg:  BestimmeExpSpezFkt();  break;
                    case Constants.logReg:      BestimmeLogFkt();      break;
                    case Constants.potReg:      BestimmePotFkt();      break;
                    case Constants.quadReg:     BestimmeQuadratFkt();  break;
                    case Constants.expReg:      BestimmeExpFkt();      break;
                    case Constants.sinReg:      BestimmeSinFkt();      break;
                    case Constants.sinExpReg:   BestimmeSinExpFkt();   break;
                    case Constants.resoReg:     BestimmeResonanzFkt(); break;
                    default: BestimmeLinFkt(); break;
                }
                GetRegressionFunctionStringAndAverageError(regressionTyp, -1);              
            }
        }


        ///////////////////////////////////////////////////////////////////////////////
        // Small helping Methods                                                     //
        ///////////////////////////////////////////////////////////////////////////////

        private void _doRegress(int anzahl, List<double> wertX, List<double> wertY, double[] param)
        {
            double sumX, sumX2, sumXY, sumY, x, y;
            int k;
            sumX = 0; sumX2 = 0; sumXY = 0; sumY = 0;
            for (k = 0; k < anzahl; k++)
            {
                x = wertX[k];
                y = wertY[k];
                sumX = sumX + x;
                sumX2 = sumX2 + x * x;
                sumY = sumY + y;
                sumXY = sumXY + x * y;
            }
            double nenner = anzahl * sumX2 - sumX * sumX;
            // y = param[1]*x+param[0]
            if (nenner != 0)
            {
                param[1] = (anzahl * sumXY - sumX * sumY) / nenner; //Steigung
                param[0] = (sumY * sumX2 - sumX * sumXY) / nenner;  //y-Achsenabschnitt
            }
        }


        private double _AbschaetzungFuerB()
        {
            /* f�r f(x)=a*exp(b*x)+c bzw. f(x)= a*sin(b*x+c)+d gilt:
             * b=f''(x)/f'(x)  (bei der Sinusfunktion ergibt sich -b !!
             * mit der Annahme (f(x3)-f(x1))/(x3-x1) ungef�hr gleich f'(x2)
             * ergibt sich nachfolgende Rechnung: )
            */
            return ((wertY[4] - wertY[2]) / (wertX[4] - wertX[2]) - (wertY[2] - wertY[0]) / (wertX[0]) - wertX[0]) / (wertY[3] - wertY[1]);
        }

        private void BerechneABC()
        {
            double a, b, c;
            int k;
            a = 0; c = 0; b = _AbschaetzungFuerB();
            for (k = 0; k < anzahl; k++) { a = a + (wertY[k] - wertY[k + 1]) / (Math.Exp(b * wertX[k]) - Math.Exp(b * wertX[k + 1])); }
            a = a / anzahl;
            for (k = 0; k < anzahl; k++) { c = c + wertY[0] - a * Math.Exp(b * wertX[0]); }
            c = c / anzahl;
        }

        private void _getPeriode(List<double> dataX, List<double> dataY, out double schaetzWert, out double maxSchaetz, out double schaetzStep)
        {
            double hilf, minX, maxX;
            int sign, anz, k;

            //Sch�tzwert f�r b ermitteln:  y=a*sin(b*x)*exp(c*x);
            getMinMax(dataX, out minX, out  maxX);
            sign = Math.Sign(dataY[0]);
            if (sign == 0)
            {
                if (Math.Sign(dataY[1]) >= 0) { sign = 1; }
                else { sign = -1; }
            }
            anz = 1;
            for (k = 0; k < anzahl - 1; k++)
            {
                if (sign != Math.Sign(dataY[k + 1])) { anz = anz + 1; sign = -sign; }
            }
            maxSchaetz = anz * Math.PI / (maxX - minX);
            schaetzWert = (anz - 1) * Math.PI / maxX;
            schaetzStep = 10000; hilf = maxSchaetz - schaetzWert;
            while (schaetzStep >= hilf) { schaetzStep = schaetzStep * 0.1; }
            schaetzWert = Math.Floor(schaetzWert / schaetzStep) * schaetzStep;
            maxSchaetz = Math.Floor(maxSchaetz / schaetzStep + 1) * schaetzStep;
            if (schaetzWert == 0) { schaetzWert = schaetzStep; }
        }


        private void BestimmeLinFkt()
        {
            _doRegress(anzahl, wertX, wertY, param);
            p[Constants.linReg, 0] = param[1]; p[Constants.linReg, 1] = param[0];
        }

        private void BestimmeExpSpezFkt()
        {
            int k;
            List<double> tempWertY = new List<double>();
            for (k = 0; k < anzahl; k++)
            {
                tempWertY.Add(Math.Log(wertY[k]));
            }
            _doRegress(anzahl, wertX, tempWertY, param);
            param[0] = Math.Exp(param[0]);
            p[Constants.expSpezReg, 0] = param[0]; p[Constants.expSpezReg, 1] = param[1];
        }

        private void BestimmeLogFkt()
        {
            int k;
            double hilf;
            List<double> tempWertX = new List<double>();
            for (k = 0; k < anzahl; k++)
            {
                tempWertX.Add(Math.Log(wertX[k]));
            }
            _doRegress(anzahl, tempWertX, wertY, param);

            hilf = param[1];
            param[1] = Math.Exp(param[0] / hilf);
            param[0] = hilf;
            p[Constants.logReg, 0] = param[0]; p[Constants.logReg, 1] = param[1];
        }

        private void BestimmePotFkt()
        {
            int k, start;
            List<double> tempWertX = new List<double>();
            List<double> tempWertY = new List<double>();

            if ((wertX[0] <= 0) || (wertY[0] <= 0))
            { start = 1; }
            else { start = 0; }

            for (k = start; k < anzahl; k++)
            {
                tempWertX.Add(Math.Log(wertX[k]));
                tempWertY.Add(Math.Log(wertY[k]));
            }
            _doRegress(anzahl - start, tempWertX, tempWertY, param);
            param[0] = Math.Exp(param[0]);
            p[Constants.potReg, 0] = param[0]; p[Constants.potReg, 1] = param[1];
        }

        private void BestimmeQuadratFkt()
        {
            int k;
            double sumX4, sumX3, sumX2, sumX, sumXY, sumX2Y, sumY,
                   xi, yi, a, b, c;
            MatrixLibrary.Matrix M, v, lsg;

            sumX4 = 0; sumX3 = 0; sumX2 = 0; sumX = 0; sumXY = 0; sumX2Y = 0; sumY = 0;

            for (k = 0; k < anzahl; k++)
            {
                xi = wertX[k];
                yi = wertY[k];
                sumX = sumX + xi;
                sumY = sumY + yi;
                sumXY = sumXY + yi * xi;
                xi = xi * xi;
                sumX2 = sumX2 + xi;
                sumX2Y = sumX2Y + xi * yi;
                sumX3 = sumX3 + xi * wertX[k];
                sumX4 = sumX4 + xi * xi;
            }
            //LGS:
            // a*sumX4 + b*sumX3 +c*sumX2 = sumX2Y
            // a*sumX3 + b*sumX2 +c*sumX  = sumXY
            // a*sumX2 + b*sumX  +c*k     = sumY
            M = new MatrixLibrary.Matrix(3, 3); v = new MatrixLibrary.Matrix(3, 1); lsg = new MatrixLibrary.Matrix(3, 1);
            M[0, 0] = sumX4; M[0, 1] = sumX3; M[0, 2] = sumX2; v[0, 0] = sumX2Y;
            M[1, 0] = sumX3; M[1, 1] = sumX2; M[1, 2] = sumX; v[1, 0] = sumXY;
            M[2, 0] = sumX2; M[2, 1] = sumX; M[2, 2] = anzahl; v[2, 0] = sumY;
            lsg = MatrixLibrary.Matrix.SolveLinear(M, v);
            a = lsg[0, 0];
            b = lsg[1, 0];
            c = lsg[2, 0];
            p[Constants.quadReg, 0] = a; p[Constants.quadReg, 1] = b; p[Constants.quadReg, 2] = c;
        }

        private void BestimmeExpFkt()
        {
            double yMin, yMax;
            double yi, fehler, abw, bestA, bestB, bestC, schaetzWert, schaetzStep, steigungAmAnfang, steigungAmEnde, fehlergrenze;
            int k, iter, sign, z;
            List<double> tempWertY;
            bool weiter;
            //Sch�tzwert f�r Verschiebung in y-Richtung; 

            getMinMax(wertY, out yMin, out yMax);
            schaetzWert = 0; schaetzStep = 1; sign = 1;

            steigungAmAnfang = (wertY[0] - wertY[1]) / (wertX[0] - wertX[1]);
            steigungAmEnde = (wertY[anzahl - 1] - wertY[anzahl - 2]) / (wertX[anzahl - 1] - wertX[anzahl - 2]);
            if (((steigungAmAnfang < 0) && (steigungAmAnfang > -0.2)) || ((steigungAmEnde > 0) && (steigungAmEnde < 0.2))) //Asymptote oben
            {
                if (yMax < 0)
                {
                    schaetzStep = Math.Pow(10, Math.Floor(Math.Log10(-yMax)));
                    schaetzWert = 0;
                }
                else
                {
                    if (yMax > 0)
                    {
                        schaetzStep = Math.Pow(10, Math.Floor(Math.Log10(yMax)));
                        schaetzWert = schaetzStep * 10;
                    }
                    else { schaetzWert = 1; schaetzStep = 0.1; }
                }
                sign = -1;
            }
            else
            {
                if (((steigungAmAnfang > 0) && (steigungAmAnfang < 0.2)) || ((steigungAmEnde < 0) && (steigungAmEnde > -0.2)))  //Asymptote unten
                {
                    if (yMin < 0)
                    {
                        schaetzStep = Math.Pow(10, Math.Floor(Math.Log10(-yMin)));
                        schaetzWert = -schaetzStep * 10;
                    }
                    else
                    {
                        if (yMin > 0)
                        {
                            schaetzStep = Math.Pow(10, Math.Floor(Math.Log10(yMin)));
                            schaetzWert = Math.Floor(yMin * schaetzStep) / schaetzStep;
                        }
                        else { schaetzWert = -1; schaetzStep = 0.1; }
                    }
                    sign = 1;
                }
                else
                {
                    //keine Aymptote erkennbar

                    if (steigungAmAnfang < steigungAmEnde)
                    {
                        if (yMin > 0)
                        {
                            schaetzStep = Math.Pow(10, Math.Floor(Math.Log10(yMin)));
                            schaetzWert = -schaetzStep;
                        }
                        else
                        {
                            if (yMin < 0)
                            {
                                schaetzStep = Math.Pow(10, Math.Floor(Math.Log10(-yMin)));
                                schaetzWert = Math.Floor(yMin * schaetzStep) / schaetzStep - schaetzStep;
                            }
                            else { schaetzWert = -1; schaetzStep = 0.1; }
                        }
                        sign = 1;
                    }
                    else
                    {
                        if (yMax > 0)
                        {
                            schaetzStep = Math.Pow(10, Math.Floor(Math.Log10(yMax)));
                            schaetzWert = 10 * schaetzStep;
                        }
                        else
                        {
                            if (yMax < 0)
                            {
                                schaetzStep = Math.Pow(10, Math.Floor(Math.Log10(-yMax)));
                                schaetzWert = 0;
                            }
                            else { schaetzWert = 1; schaetzStep = 0.1; }
                        }
                        sign = -1;
                    }
                }
            }

            tempWertY = new List<double>();
            for (k = 0; k < anzahl; k++) { tempWertY.Add(0); }
            //iterieren �ber c; 
            bestA = 0; bestB = 0; bestC = 0;
            abw = startAbw; iter = 1; weiter = true; fehlergrenze = anzahl * genauigkeit; z = 0;
            do
            {
                for (k = 0; k < anzahl; k++)
                {
                    tempWertY[k] = Math.Log(Math.Abs(wertY[k] - schaetzWert));
                }
                _doRegress(anzahl, wertX, tempWertY, param);
                param[0] = Math.Exp(param[0]);
                if (schaetzWert > yMax) { param[0] = -param[0]; }
                fehler = 0;
                for (k = 0; k < anzahl; k++)
                {
                    yi = param[0] * Math.Exp(param[1] * wertX[k]) + schaetzWert;
                    fehler = fehler + (yi - wertY[k]) * (yi - wertY[k]);
                }

                if (abw > fehler)
                {
                    abw = fehler;
                    bestA = param[0]; bestB = param[1]; bestC = schaetzWert;
                    if ((abw < fehlergrenze) || (schaetzStep < minStep)) { weiter = false; }
                }

                if (z < 9)
                {
                    schaetzWert = schaetzWert + sign * schaetzStep;
                    z = z + 1;
                    if (((sign == 1) && (schaetzWert > yMin)) || ((sign == -1) && (schaetzWert < yMax))) { z = 10; }
                }
                if (z >= 9)
                {
                    if (schaetzStep > minStep)
                    {
                        schaetzWert = bestC - sign * 0.9 * schaetzStep;
                        schaetzStep = schaetzStep * 0.1;
                        z = -10; iter = iter - 10;
                    }
                    else { weiter = false; }
                }
                iter++;

            } while (weiter && (iter < maxIteration));
            p[Constants.expReg, 0] = bestA; p[Constants.expReg, 1] = bestB; p[Constants.expReg, 2] = bestC; 
            p[Constants.expReg, 5] = abw / anzahl;
        }

        private void BestimmeSinFkt()
        {
            int n, k, iter, z;
            double yMin = 0;
            double yMax = 0;
            double maxSchaetz, schaetzWert, schaetzStep;
            double sumSin, sumSin2, sumSinY, sumCosY, sumSinCos, sumCos, sumCos2, sumY,
                   xi, xci, yi, fehler, a1, c1, a, b, c, d, abw;
            List<double> tempWertY;
            MatrixLibrary.Matrix M, v, lsg;
            double bestA, bestB, bestC, bestD;
            bool weiter;


            bestA = 0; bestB = 0; bestC = 0; bestD = 0;
            getMinMax(wertY, out yMin, out yMax);
            // Amplitude a:
            a = (yMax - yMin) * 1.02 / 2;
            // y-Verschiebung d:
            d = (yMax + yMin) / 2;
            //Periodenl�nge:
            tempWertY = new List<double>();

            for (k = 0; k < anzahl; k++)
            {
                tempWertY.Add(wertY[k] - d);
            }
            _getPeriode(wertX, tempWertY, out schaetzWert, out maxSchaetz, out schaetzStep);

            // a*sin(b*x+c)+d = a*cos(c)*sin(b*x) + a*sin(c)*cos(b*x)+d = a1*sin(b*x) + c1*cos(b*x) + d;
            // iteration �ber b:
            weiter = true; iter = 0; abw = startAbw; c = 0; b = schaetzWert; z = 0;
            while (weiter && (iter < maxIteration))
            {
                sumSin = 0; sumSin2 = 0; sumSinY = 0; sumSinCos = 0; sumCos = 0; sumCos2 = 0; sumCosY = 0; sumY = 0; fehler = 0;
                for (n = 0; n < anzahl; n++)
                {
                    xi = wertX[n];
                    yi = wertY[n];
                    xci = Math.Cos(schaetzWert * xi);
                    xi = Math.Sin(schaetzWert * xi);
                    sumSin = sumSin + xi;
                    sumY = sumY + yi;
                    sumSinY = sumSinY + yi * xi;

                    sumSin2 = sumSin2 + xi * xi;
                    sumCosY = sumCosY + xci * yi;
                    sumCos = sumCos + xci;
                    sumCos2 = sumCos2 + xci * xci;
                    sumSinCos = sumSinCos + xi * xci;
                }
                //LGS:
                // a1*sumSin2   + c1*sumSinCos + d*sumSin  = sumSinY
                // a1*sumSinCos + c1*sumCos2   + d*sumCos  = sumCosY
                // a1*sumSin    + c1*sumCos    + d*k       = sumY
                M = new MatrixLibrary.Matrix(3, 3); v = new MatrixLibrary.Matrix(3, 1); lsg = new MatrixLibrary.Matrix(3, 1);
                M[0, 0] = sumSin2; M[0, 1] = sumSinCos; M[0, 2] = sumSin; v[0, 0] = sumSinY;
                M[1, 0] = sumSinCos; M[1, 1] = sumCos2; M[1, 2] = sumCos; v[1, 0] = sumCosY;
                M[2, 0] = sumSin; M[2, 1] = sumCos; M[2, 2] = anzahl; v[2, 0] = sumY;
                lsg = MatrixLibrary.Matrix.SolveLinear(M, v);
                a1 = lsg[0, 0];
                c1 = lsg[1, 0];
                a = Math.Sqrt(a1 * a1 + c1 * c1);
                if (a1 < 0) { a = -a; }
                b = schaetzWert;
                c = Math.Asin(c1 / a);
                d = lsg[2, 0];

                fehler = 0;
                for (n = 0; n < anzahl; n++)
                {
                    yi = a * Math.Sin(b * wertX[n] + c) + d - wertY[n];
                    fehler = fehler + yi * yi;
                }
                iter = iter + 1;
                if (abw > fehler) { abw = fehler; bestA = a; bestB = schaetzWert; bestC = c; bestD = d; z = 5; }
                if ((z < 9) && (schaetzWert < maxSchaetz))
                {
                    schaetzWert = schaetzWert + schaetzStep;
                    z = z + 1;
                }
                else
                {
                    if (schaetzStep > minStep)
                    {
                        schaetzWert = bestB - 0.9 * schaetzStep;
                        z = -10; schaetzStep = schaetzStep * 0.1;
                    }
                    else { weiter = false; }
                }
            }  //Ende While-Schleife
            if (bestA < 0) { bestA = -bestA; bestC = bestC + Math.PI; }
            p[Constants.sinReg, 0] = bestA; p[Constants.sinReg, 1] = bestB; p[Constants.sinReg, 2] = bestC; p[Constants.sinReg, 3] = bestD;
            p[Constants.sinReg, 5] = abw / anzahl;
        }

        private void BestimmeSinExpFkt()
        {
            double maxSchaetz, schaetzWert, schaetzStep, hilf;
            double xi, sinbxi, a, b, c, abw, fehler;
            double bestA, bestB, bestC;
            int anz, k, z, iter;
            bool weiter;
            List<double> tempWertX, tempWertY;

            //Sch�tzwert f�r b ermitteln:  y=a*sin(b*x)*exp(c*x);
            _getPeriode(wertX, wertY, out schaetzWert, out maxSchaetz, out schaetzStep);

            weiter = true; z = 0; iter = 0; abw = startAbw;
            a = 0; b = schaetzWert; c = 0; bestA = 0; bestB = b; bestC = 0;
            tempWertX = new List<double>();
            tempWertY = new List<double>();
            for (k = 0; k < anzahl; k++) { tempWertX.Add(0); tempWertY.Add(0); }
            while (weiter && (iter < maxIteration))
            {
                anz = 0;
                for (k = 0; k < anzahl; k++)
                {
                    xi = wertX[k];
                    sinbxi = Math.Sin(schaetzWert * xi);
                    if (sinbxi != 0)
                    {

                        hilf = wertY[k] / sinbxi;
                        if (hilf > 0)
                        {
                            tempWertX[anz] = xi;
                            tempWertY[anz] = Math.Log(hilf);
                            anz = anz + 1;
                        }
                    }
                }  //for k
                if (anz >= 0.8 * anzahl) //mehr als 80% der Wertepaare k�nnen bei dieser Sch�tzung f�r b ber�cksichtigt werden
                {
                    _doRegress(anz, tempWertX, tempWertY, param);
                    a = Math.Exp(param[0]);
                    c = param[1];
                    fehler = 0;
                    for (k = 0; k < anzahl; k++)
                    {
                        xi = wertX[k];
                        hilf = a * Math.Sin(schaetzWert * xi) * Math.Exp(c * xi) - wertY[k];
                        fehler = fehler + hilf * hilf;
                    }
                    iter = iter + 1;
                    if (abw > fehler) { abw = fehler; bestA = a; bestB = schaetzWert; bestC = c; }
                }
                else { z = z - 1; }
                if ((z < 9) && (schaetzWert < maxSchaetz))
                {
                    schaetzWert = schaetzWert + schaetzStep;
                    z = z + 1;
                }
                else
                {
                    if (schaetzStep > minStep)
                    {
                        schaetzWert = bestB - 0.9 * schaetzStep;
                        z = -10; schaetzStep = schaetzStep * 0.1;
                    }
                    else { weiter = false; }
                }
            } //while weiter
            p[Constants.sinExpReg, 0] = bestA; p[Constants.sinExpReg, 1] = bestB; p[Constants.sinExpReg, 2] = bestC; p[Constants.sinExpReg, 5] = abw / anzahl;
        }


        /*mechanische Schwingungen:
         *Gerthsen und andere
         *
         *                         K0                                   K0                 
         * A(w) = --------------------------------------  -->  ------------------------ 
         *        Sqrt( (w0^2 - w^2)^2 + b^2/m^2 *w^2 )         Sqrt( (R - x)^2 + S*x )  
         *
         * 
         * 
         * SchwingKreis:
         *                 U0                                      U0                                U0*L                                 a
         * I0 = -------------------------------  =  ------------------------------------ = ----------------------------------- --> ------------------------
         *      Sqrt( R^2 + (w*L - 1/(w*C) )^2 )    Sqrt( R^2 + L^2*(w - 1/(w*L*C) )^2 )   Sqrt( (R/L)^2 + (w - 1/(w*L*C) )^2 )    Sqrt( b + (x - c/x )^2 )
         *      
         * a= U0*L; b = (R/L)^2; c = 1/(L*C)
         */

        private void BestimmeResonanzFkt()
        {
            double maxSchaetz, schaetzWert, schaetzStep, hilf, maxY, offset, grenze;
            double xi, yi, hilfX, a, b, c, abw, fehler;
            double bestA, bestB, bestC;
            int anz, k, z, iter, maxPos;
            bool weiter;
            List<double> tempWertX = new List<double>();
            List<double> tempWertY = new List<double>();
            for (k = 0; k < anzahl; k++) { tempWertX.Add(0); tempWertY.Add(0); }
            bestA = 0; bestB = 0; bestC = 0;
            maxSchaetz = 0; schaetzWert = 0; schaetzStep = 0;
            //Parameter absch�tzen
            maxPos = -1; k = 0; maxY = -1E150;
            for (k = 0; k < anzahl; k++)
            {
                yi = wertY[k];
                if (yi > maxY) { maxY = yi; maxPos = k; }
            }
            offset = 10000000000.0;
            while (offset > maxY / 100) { offset = offset * 0.1; }

            // Quadrat des x-Wertes der ResonanzStelle ist ungef�hr der Schaetzwert
            if (maxPos > 0) { schaetzWert = wertX[maxPos - 1]; }
            else { schaetzWert = wertX[maxPos]; }

            if (maxPos < anzahl - 11) { maxSchaetz = wertX[maxPos + 1]; }
            else { maxSchaetz = wertX[maxPos]; }

            maxSchaetz = maxSchaetz * maxSchaetz;
            schaetzWert = schaetzWert * schaetzWert;
            grenze = 1;
            while (grenze > (maxSchaetz - schaetzWert) / 1000) { grenze = grenze * 0.1; }
            schaetzStep = grenze * 1000;
            schaetzWert = Math.Floor(schaetzWert);

            if (maxPos == 0) { schaetzWert = schaetzWert - 2 * schaetzStep; }
            weiter = true;
            z = 9 - (int)Math.Floor((maxSchaetz - schaetzWert) / schaetzStep);

            abw = startAbw; iter = 0;
            while (weiter && (iter < maxIteration))  //Iteration �ber c
            {
                //lin. Reg vorbereiten    1/y^2 = 1/a^2 + b/a^2 * (x - c/x) = a' + b' * hilfX
                anz = 0;
                for (k = 0; k < anzahl; k++)
                {
                    xi = wertX[k]; yi = wertY[k];
                    if (yi != 0)
                    {
                        hilfX = xi - schaetzWert / xi;
                        tempWertY[anz] = hilfX * hilfX;
                        tempWertY[anz] = 1 / (yi * yi);
                        anz = anz + 1;
                    }
                }

                _doRegress(anz, tempWertX, tempWertY, param);
                a = param[0];      // a' 
                b = param[1] / a;  // b'/a'
                a = Math.Pow(a, -0.5);
                c = schaetzWert;
                fehler = 0;
                for (k = 0; k < anzahl; k++)
                {
                    xi = wertX[k];
                    hilfX = xi - c / xi;
                    hilf = a / Math.Pow(1 + b * hilfX * hilfX, 0.5) - wertY[k];
                    fehler = fehler + hilf * hilf;
                }
                iter++;
                if (abw > fehler) { abw = fehler; bestA = a; bestB = b; bestC = c; }
                if ((z < 9) && (schaetzWert <= maxSchaetz))
                {
                    schaetzWert = schaetzWert + schaetzStep;
                    z++;
                }
                else if (schaetzStep > grenze)
                {
                    schaetzWert = bestC - 0.9 * schaetzStep;
                    z = -10;
                    schaetzStep = schaetzStep * 0.1;
                }
            }

            p[Constants.resoReg, 0] = bestA; p[Constants.resoReg, 1] = bestB; p[Constants.resoReg, 2] = bestC;
            p[Constants.resoReg, 5] = abw / anzahl;
        }

  
        /*
        private void RegressAuswahl()
        {
            double minX, maxX, minY, maxY;

            getMinMax(wertX, anzahl, out minX, out maxX);
            getMinMax(wertY, anzahl, out minY, out maxY);
            LinefittingDialog RegAuswahlDlg = new LinefittingDialog(minX < 0, (minY < 0), regTyp);
            RegAuswahlDlg.ShowDialog();
            if ((bool)RegAuswahlDlg.DialogResult)
            {
                regTyp = (int)RegAuswahlDlg.GetAuswahl();
            }
        }
         */ 

    /*
        private void buttonCalcFkt_Click(object sender, RoutedEventArgs e)
        {
            CalculateLineFitFunction(regTyp);
        }
    */
   
    }
}