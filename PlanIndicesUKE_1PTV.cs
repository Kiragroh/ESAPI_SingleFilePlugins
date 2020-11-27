
////////////////////////////////////////////////////////////////////////////////
// PlanIndices.cs
//
//  A ESAPI v11+ script that demonstrates calculating  Conformity index (CI), Gradient index (GI), Heterogeneity index (HI) for a plan and display the information in a message box.
//      a.CI: V100/TV
//      b.GI: V50/V100
//      c.HI: Dmax/Dp
//
// Kata Advanced.7    
//   Calaculate CI, GI and HI and display them in a message box.
//
// Applies to:
//      Eclipse Scripting API
//      v11, 13, 13.5, 13.6, 13.7, 15.0
//
// Copyright (c) 2016 Varian Medical Systems, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in 
//  all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// THE SOFTWARE.
////////////////////////////////////////////////////////////////////////////////
using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows.Controls;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
// TODO: uncomment the line below if the script requires write access.
// [assembly: ESAPIScript(IsWriteable = true)]


namespace VMS.TPS
{
  public class Script
  {
    public Script()
    {
    }

    public void Execute(ScriptContext context /*, System.Windows.Window window, ScriptEnvironment environment*/)
    {
      	// TODO : Add here your code that is called when the script is launched from Eclipse
	// check if the plan has dose
        if (!context.PlanSetup.IsDoseValid)
        {
            MessageBox.Show("The plan selected has no valid dose.");
            return;
        }

            // get list of structures for loaded plan
            StructureSet ss = context.StructureSet;
            var listStructures = ss.Structures;
			

			
            // define PTV (selected)
            //Structure ptv = listStructures.Where(x => !x.IsEmpty && x.Id.ToUpper().Contains("PTV1 REKTM")).FirstOrDefault();
            //Structure ptv = listStructures.Where(x => x.Id == context.PlanSetup.TargetVolumeID).FirstOrDefault();

	    var ptv = SelectStructureWindow.SelectStructure(ss);
        // make sure the volume is non-zero
        if (ptv.IsEmpty == true)
        {
            MessageBox.Show("Target Volume has no contours.");
            return;
        }
           

            // search for body
             Structure body = listStructures.Where(x => !x.IsEmpty && (x.DicomType.ToUpper().Equals("EXTERNAL") || x.Id.ToUpper().Equals("KÖRPER") || x.Id.ToUpper().Equals("BODY") || x.Id.ToUpper().Equals("OUTER CONTOUR"))).FirstOrDefault();
            if (body == null)
            {
              MessageBox.Show("Unbekannte Körper-Struktur-Bezeichnung. Körper, Body oder Outer Contour.");
                return;
            }	


        // --- calc Conformity index (CI)
        DoseValue dose100 = new DoseValue(context.PlanSetup.TreatmentPercentage*100, DoseValue.DoseUnit.Percent);
        //DoseValue dose100 = new DoseValue(100, DoseValue.DoseUnit.Percent);
	    DoseValue dose95 = new DoseValue(95, DoseValue.DoseUnit.Percent);
        double ptv100 = context.PlanSetup.GetVolumeAtDose(ptv, dose100, VolumePresentation.AbsoluteCm3);
	
	    double v100 = context.PlanSetup.GetVolumeAtDose(body, dose100, VolumePresentation.AbsoluteCm3);
        double CI =Math.Round(v100 * ptv.Volume / ptv100 / ptv100, 2);
	    double ptv95 = context.PlanSetup.GetVolumeAtDose(ptv, dose95, VolumePresentation.AbsoluteCm3);
	
	    double v95 = context.PlanSetup.GetVolumeAtDose(body, dose95, VolumePresentation.AbsoluteCm3);
        double CI95 =Math.Round(v95 * ptv.Volume / ptv95 / ptv95, 2);

        // --- calc Gradient index (GI)
        DoseValue dose50 = new DoseValue(context.PlanSetup.TreatmentPercentage*100/2, DoseValue.DoseUnit.Percent);
        double v50 = context.PlanSetup.GetVolumeAtDose(body, dose50, VolumePresentation.AbsoluteCm3);
        double GI =Math.Round(v50/v100, 2); // C# can handle divide-by-zero (double.infinity) so not need to check the situation
	    double GI95 =Math.Round(v50/v95, 2); // C# can handle divide-by-zero (double.infinity) so not need to check the situation

        // --- calc Heterogeneity index (HI)
        // get prescription dose
        double Dp = context.PlanSetup.DosePerFraction.Dose * context.PlanSetup.NumberOfFractions.Value;
        double D2 = context.PlanSetup.GetDoseAtVolume(ptv, 2, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
	
	    double D98 = context.PlanSetup.GetDoseAtVolume(ptv, 98, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
        double HI = Math.Round((D2 - D98) / Dp, 2); // C# can handle divide-by-zero (double.infinity) so not need to check the situation
 
        MessageBox.Show(string.Format("Zielvolumen: {0}\rCI\t=   {1}\rCI95\t=   {2}\rGI\t=   {3}\rGI95\t=   {4}\rHI\t=   {5}\r\rBemerkung: Es sollte nur ein Zielvolumen geben.\r\rFormeln:\rCI\t=   TV*PIV/(TV_PIV)^2)\rCI95\t=   TV*V95/(TV_V95)^2)\rGI\t=   V50/PIV\rGI95\t=   V50/V95\rHI\t=   (D_2-D_98)/D_p", ptv.Id, CI, CI95, GI, GI95, HI));

       
    }
  }
	class SelectStructureWindow : Window
    {
        public static Structure SelectStructure(StructureSet ss)
        {
	
            m_w = new Window();
			//m_w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			m_w.WindowStartupLocation = WindowStartupLocation.Manual;
			m_w.Left = 500;
			m_w.Top = 150;
            m_w.Width = 300;
            m_w.Height = 350;
            //m_w.SizeToContent = SizeToContent.Height;
            //m_w.SizeToContent = SizeToContent.Width;
            m_w.Title = "ZIELVOLUMEN auswählen:";
            var grid = new Grid();
            m_w.Content = grid;
            var list = new ListBox();
            foreach (var s in ss.Structures.OrderByDescending(s => s.Id))
            {
                if (s.IsEmpty == true) continue;
                var tempStruct = s.ToString();
                if (tempStruct.ToUpper().Contains("PTV") || tempStruct.ToUpper().Contains("ZHK") || tempStruct.ToUpper().Contains("SIB") || tempStruct.ToUpper().Contains("CTV") || tempStruct.ToUpper().Contains("GTV") || tempStruct.ToUpper().StartsWith("Z"))
                {
                    if (tempStruct.Contains(":"))
                    {
                        int index = tempStruct.IndexOf(":");
                        tempStruct = tempStruct.Substring(0, index);
                    }
                    list.Items.Add(s);
                }
            }
            list.VerticalAlignment = VerticalAlignment.Top;
            list.Margin = new Thickness(10, 10, 10, 55);
            grid.Children.Add(list);
            var button = new Button();
            button.Content = "OK";
            button.Height = 40;
            button.VerticalAlignment = VerticalAlignment.Bottom;
            button.Margin = new Thickness(10, 10, 10, 10);
            button.Click += button_Click;
            grid.Children.Add(button);
            if (m_w.ShowDialog() == true)
            {
                return (Structure)list.SelectedItem;
            }
            return null;
        }

        static Window m_w = null;

        static void button_Click(object sender, RoutedEventArgs e)
        {
            m_w.DialogResult = true;
            m_w.Close();
        }
    }
}
