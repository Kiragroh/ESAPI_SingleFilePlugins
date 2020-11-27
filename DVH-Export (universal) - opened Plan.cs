////////////////////////////////////////////////////////////////////////////////
using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows.Controls;

// TODO: uncomment the line below if the script requires write access.
// [assembly: ESAPIScript(IsWriteable = true)

namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {
        }
        private static DVHData NewMethod(VolumePresentation volumePresentation, PlanSetup planSetup, Structure roi)
        {
            return planSetup.GetDVHCumulativeData(roi, DoseValuePresentation.Absolute, volumePresentation, planSetup.TotalDose.Dose / 1000.0);
        }
        public void Execute(ScriptContext context /*, System.Windows.Window window, ScriptEnvironment environment*/)
        {
            // Choose structure for DVH-export in all plans or one course of a patient
            var ss = context.StructureSet;
            //var roi2 = SelectStructureWindow.SelectStructure(ss);
            //if (roi2 == null) return;
            //string structureOfInterest = roi2.Id;

            // Choose Destination-Folder
            string startPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.InitialDirectory = startPath;
            fileDialog.Multiselect = false;
            fileDialog.Title = "Choose Destination-Folder and press OK";
            fileDialog.ShowReadOnly = true;
            fileDialog.FilterIndex = 0;
            fileDialog.ValidateNames = false;
            fileDialog.CheckFileExists = false;
            fileDialog.CheckPathExists = true;
            fileDialog.FileName = "Folder Selection";
            if (fileDialog.ShowDialog() == false)
            {
                return;    // user canceled
            }
            var filePath = fileDialog.FileName;
            string outputDestinationDirectory = Path.GetDirectoryName(filePath);


            //string outputDestinationDirectory = @"C:\Users\mg\Desktop\Skript-Output";
            string scriptname = "DVH-Export (opened Plan)";
            
            // Retrieve patient information
            Patient patient = context.Patient;
			PlanSetup planSetup = context.PlanSetup;
            // Iterate through all created courses and plans with dosis
            // Use all courses of a patient. Uncomment this:
            //var courses = patient.Courses.Where(c => c.HistoryDateTime != null).ToList();
            // Use specific Course. Uncomment this:
            // var courses = patient.Courses.Where(c => c.Id.Equals("ProstataOnly")).ToList();
            var listStructures = planSetup.StructureSet.Structures;
                foreach (Structure roi in listStructures.Where(x=>x.HasSegment && x.Volume!=0 &! x.Id.ToUpper().StartsWith("COUCH")))
                {
                    
                    try{
                    
                        // extract DVH data for ptv using bin width of 0.1.
                        DVHData dvh = planSetup.GetDVHCumulativeData(roi, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1);

                        string filename = string.Format(@"{0}\DVH_{1}_{2}_{3}.txt", outputDestinationDirectory, context.Patient.Id, roi.Id, planSetup.Id);

                        // write a header
                        string[] msg = { string.Format("Pat.Id:,{0}", context.Patient.Id), string.Format("Plan.Id:,{0}", planSetup.Id), string.Format("Structure:,{0}", roi.Id),"----,----","Dose,Volume","Gy,%" };
                        System.IO.File.WriteAllLines(filename, msg);

                        // write all dvh points
                        foreach (DVHPoint pt in dvh.CurveData)
                        {
                            string line = string.Format("{0},{1}", pt.DoseValue.Dose, pt.Volume);
                            File.AppendAllText(filename, line + Environment.NewLine);
                        }
                    }
					catch{}
                }
            
           string message = string.Format("DVH-Export for {0} is finished. \nData saved in: {1}", context.Patient.Id, outputDestinationDirectory);
           MessageBox.Show(message, scriptname, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    class SelectStructureWindow : Window
    {
        public static Structure SelectStructure(StructureSet ss)
        {
            m_w = new Window();
            m_w.Width = 450;
            m_w.Height = 400;
            m_w.SizeToContent = SizeToContent.Height;
            //m_w.SizeToContent = SizeToContent.Width;
            m_w.Title = "Select structure for DVH-Export:";
            var grid = new Grid();
            m_w.Content = grid;
            var list = new ListBox();
            foreach (var s in ss.Structures)
            {
               var tempStruct = s.ToString();
               if (tempStruct.Contains(":"))
               {
                   int index = tempStruct.IndexOf(":");
                   tempStruct = tempStruct.Substring(0, index);
               }
               list.Items.Add(s);
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