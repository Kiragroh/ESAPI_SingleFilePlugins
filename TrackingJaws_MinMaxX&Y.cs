using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using System.Windows.Documents;
using System.Windows.Media;

[assembly: AssemblyVersion("1.0.0.1")]

namespace VMS.TPS
{
    public class Script
    {
        public Dictionary<string, Tuple<double, double, double, double>> minmaxJawSize = new Dictionary<string, Tuple<double, double, double, double>>();
        public Script()
        {
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context, System.Windows.Window window, ScriptEnvironment environment)
        {
            // TODO : Add here the code that is called when the script is launched from Eclipse.
            if(context.PlanSetup == null)
            {
                MessageBox.Show("Please select a plan");
            }
            foreach(Beam beam in context.PlanSetup.Beams.Where(x => !x.IsSetupField))
            {
                minmaxJawSize.Add(beam.Id, new Tuple<double, double, double, double>(GetMinXSize(beam), GetMinYSize(beam), GetMaxXSize(beam), GetMaxYSize(beam)));
                //maxJawSize.Add(beam.Id, new Tuple<double, double>(GetMinXSize(beam), GetMinYSize(beam)));
            }
            
            GenerateOutput(window);
        }

        private void GenerateOutput(Window window)
        {
            if(minmaxJawSize.Count() == 0)
            {
                window.Content = "No Fields Detected";
            }
            Table table = new Table();
            table.RowGroups.Add(new TableRowGroup());
            table.RowGroups.First().Rows.Add(new TableRow());
            table.RowGroups.First().Rows.Last().Cells.Add(new TableCell(new Paragraph(new Run("Field Id") { FontWeight = FontWeights.Bold })) { TextAlignment = TextAlignment.Center });
            table.RowGroups.First().Rows.Last().Cells.Add(new TableCell(new Paragraph(new Run("MinX[cm]") { FontWeight = FontWeights.Bold })) { TextAlignment = TextAlignment.Center });
            table.RowGroups.First().Rows.Last().Cells.Add(new TableCell(new Paragraph(new Run("MinY[cm]") { FontWeight = FontWeights.Bold })) { TextAlignment = TextAlignment.Center });
            table.RowGroups.First().Rows.Last().Cells.Add(new TableCell(new Paragraph(new Run("MaxX[cm]") { FontWeight = FontWeights.Bold })) { TextAlignment = TextAlignment.Center });
            table.RowGroups.First().Rows.Last().Cells.Add(new TableCell(new Paragraph(new Run("MaxY[cm]") { FontWeight = FontWeights.Bold })) { TextAlignment = TextAlignment.Center });

            foreach (var jawSize in minmaxJawSize)
            {
                table.RowGroups.First().Rows.Add(new TableRow());
                table.RowGroups.First().Rows.Last().Cells.Add(new TableCell(new Paragraph(new Run(jawSize.Key))) { BorderBrush = Brushes.Navy, BorderThickness = new Thickness(1) });
                table.RowGroups.First().Rows.Last().Cells.Add(new TableCell(new Paragraph(new Run(jawSize.Value.Item1.ToString("F2")))) { BorderBrush = Brushes.Navy, BorderThickness = new Thickness(1) });
                table.RowGroups.First().Rows.Last().Cells.Add(new TableCell(new Paragraph(new Run(jawSize.Value.Item2.ToString("F2")))) { BorderBrush = Brushes.Navy, BorderThickness = new Thickness(1) });
                table.RowGroups.First().Rows.Last().Cells.Add(new TableCell(new Paragraph(new Run(jawSize.Value.Item3.ToString("F2")))) { BorderBrush = Brushes.Navy, BorderThickness = new Thickness(1) });
                table.RowGroups.First().Rows.Last().Cells.Add(new TableCell(new Paragraph(new Run(jawSize.Value.Item4.ToString("F2")))) { BorderBrush = Brushes.Navy, BorderThickness = new Thickness(1) });
            }
            window.Width = 400;
            FlowDocument fd = new FlowDocument();
            fd.Blocks.Add(table);
            window.Content = fd;
        }

        private double GetMinXSize(Beam beam)
        {
            double minX = 42;
            foreach(ControlPoint cp in beam.ControlPoints)
            {
                if((cp.JawPositions.X2 - cp.JawPositions.X1)/10 < minX)
                {
                    //jaw size returned in mm. div by 10 for cm.
                    minX = (cp.JawPositions.X2 - cp.JawPositions.X1) / 10;
                }
            }
            return minX;
        }

        private double GetMinYSize(Beam beam)
        {
            double minY = 42;
            foreach (ControlPoint cp in beam.ControlPoints)
            {
                if ((cp.JawPositions.Y2 - cp.JawPositions.Y1) / 10 < minY)
                {
                    minY = (cp.JawPositions.Y2 - cp.JawPositions.Y1) / 10;
                }
            }
            return minY;
        }
        private double GetMaxXSize(Beam beam)
        {
            double maxX = 0;
            foreach (ControlPoint cp in beam.ControlPoints)
            {
                if ((cp.JawPositions.X2 - cp.JawPositions.X1) / 10 > maxX)
                {
                    //jaw size returned in mm. div by 10 for cm.
                    maxX = (cp.JawPositions.X2 - cp.JawPositions.X1) / 10;
                }
            }
            return maxX;
        }

        private double GetMaxYSize(Beam beam)
        {
            double maxY = 0;
            foreach (ControlPoint cp in beam.ControlPoints)
            {
                if ((cp.JawPositions.Y2 - cp.JawPositions.Y1) / 10 > maxY)
                {
                    maxY = (cp.JawPositions.Y2 - cp.JawPositions.Y1) / 10;
                }
            }
            return maxY;
        }
    }
}