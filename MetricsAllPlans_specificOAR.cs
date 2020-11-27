////////////////////////////////////////////////////////////////////////////////
using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

// TODO: uncomment the line below if the script requires write access.
// [assembly: ESAPIScript(IsWriteable = true)]


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
            // Hard coded parameters. For a real-world application, some or all of these would be input by the user 
            //string structureOfInterest = "Anus OAR";
            //DoseValue d40 = new DoseValue(40, DoseValue.DoseUnit.Gy);
            VolumePresentation volumePresentation = VolumePresentation.Relative;

	// Retrieve patient information
            Patient patient = context.Patient;

            // Output file. Again hard coded - in a real world application, we would likely use a file selection
            // dialog or some other method of getting the output destination from the user.
           
            //string outputDestinationDirectory = @"Z:\Für Alle\Test";

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
            fileDialog.FileName = "Folder Selection.";
            if (fileDialog.ShowDialog() == false)
            {
                return;    // user canceled
            }
            var filePath = fileDialog.FileName;
            string outputDestinationDirectory = Path.GetDirectoryName(filePath);


            //string outputDestinationDirectory = @"C:\Users\mg\Desktop\Skript-Output";
            //string scriptname = "DVH-Export (universal)";

            // String builder to contain result
            StringBuilder result = new StringBuilder();
            result.AppendFormat("ID,Course,Plan,Structure,Mean dose [Gy],Max dose [Gy],Min dose [Gy],D2 [Gy],D5 [Gy],D98 [Gy],D95 [Gy],D50 [Gy]\n");        

            // Iterate through all created courses and plans with dosis
            // Use all courses of a patient. Uncomment this:
            var courses = patient.Courses.Where(c => c.HistoryDateTime != null).ToList();
            // Use specific Course. Uncomment this:
            // var courses = patient.Courses.Where(c => c.Id.Equals("ProstataOnly")).ToList();
            foreach (var course in courses)
            {
                // Iterate through alls plans with dose
                var planSetups = course.PlanSetups.Where(p => (p.Dose != null)).ToList();
                foreach (var planSetup in planSetups)
                {
                    var listStructures = planSetup.StructureSet.Structures;
                   
                    // Check for structure of interest and compute DVH metrics
                    var roi = planSetup.StructureSet.Structures.FirstOrDefault(o => o.Id.ToUpper().Equals("ANUS_P"));
                    if (roi != null) 
                    {
                        planSetup.DoseValuePresentation = DoseValuePresentation.Absolute;
                        //double V40 = planSetup.GetVolumeAtDose(roi, d40, volumePresentation);
                        double D2 = planSetup.GetDoseAtVolume(roi, 2, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D98 = planSetup.GetDoseAtVolume(roi, 98, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D5 = planSetup.GetDoseAtVolume(roi, 5, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D95 = planSetup.GetDoseAtVolume(roi, 95, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D50 = planSetup.GetDoseAtVolume(roi, 50, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        DVHData dvh = NewMethod(volumePresentation, planSetup, roi);
                        result.AppendFormat("{0},{1},{2},{3},{4:0.000},{5:0.000},{6:0.000},{7:0.000},{8:0.000},{9:0.000},{10:0.000},{11:0.000}\n", patient.Id, course.Id, planSetup.Id, roi.Id, dvh.MeanDose.Dose, dvh.MaxDose.Dose, dvh.MinDose.Dose, D2, D5, D98, D95, D50);
                    }
                   
var roi2 = planSetup.StructureSet.Structures.FirstOrDefault(o => o.Id.ToUpper().Equals("DARM_P"));
                    if (roi2 != null) 
                    {
                        planSetup.DoseValuePresentation = DoseValuePresentation.Absolute;
                        //double V40 = planSetup.GetVolumeAtDose(roi2, d40, volumePresentation);
                        double D2 = planSetup.GetDoseAtVolume(roi2, 2, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D98 = planSetup.GetDoseAtVolume(roi2, 98, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D5 = planSetup.GetDoseAtVolume(roi2, 5, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D95 = planSetup.GetDoseAtVolume(roi2, 95, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D50 = planSetup.GetDoseAtVolume(roi2, 50, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        DVHData dvh = NewMethod(volumePresentation, planSetup, roi2);
                        result.AppendFormat("{0},{1},{2},{3},{4:0.000},{5:0.000},{6:0.000},{7:0.000},{8:0.000},{9:0.000},{10:0.000},{11:0.000}\n", patient.Id, course.Id, planSetup.Id, roi2.Id, dvh.MeanDose.Dose, dvh.MaxDose.Dose, dvh.MinDose.Dose, D2, D5, D98, D95, D50);
                    }
var roi3 = planSetup.StructureSet.Structures.FirstOrDefault(o => o.Id.ToUpper().Equals("FEMURKOPFRE_P"));
                    if (roi3 != null) 
                    {
                        planSetup.DoseValuePresentation = DoseValuePresentation.Absolute;
                        //double V40 = planSetup.GetVolumeAtDose(roi3, d40, volumePresentation);
                        double D2 = planSetup.GetDoseAtVolume(roi3, 2, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D98 = planSetup.GetDoseAtVolume(roi3, 98, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D5 = planSetup.GetDoseAtVolume(roi3, 5, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D95 = planSetup.GetDoseAtVolume(roi3, 95, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D50 = planSetup.GetDoseAtVolume(roi3, 50, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        DVHData dvh = NewMethod(volumePresentation, planSetup, roi3);
                        result.AppendFormat("{0},{1},{2},{3},{4:0.000},{5:0.000},{6:0.000},{7:0.000},{8:0.000},{9:0.000},{10:0.000},{11:0.000}\n", patient.Id, course.Id, planSetup.Id, roi3.Id, dvh.MeanDose.Dose, dvh.MaxDose.Dose, dvh.MinDose.Dose, D2, D5, D98, D95, D50);
                    }
var roi4 = planSetup.StructureSet.Structures.FirstOrDefault(o => o.Id.ToUpper().Equals("FEMURKOPFLI_P"));
                    if (roi4 != null) 
                    {
                        planSetup.DoseValuePresentation = DoseValuePresentation.Absolute;
                        //double V40 = planSetup.GetVolumeAtDose(roi4, d40, volumePresentation);
                        double D2 = planSetup.GetDoseAtVolume(roi4, 2, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D98 = planSetup.GetDoseAtVolume(roi4, 98, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D5 = planSetup.GetDoseAtVolume(roi4, 5, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D95 = planSetup.GetDoseAtVolume(roi4, 95, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D50 = planSetup.GetDoseAtVolume(roi4, 50, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        DVHData dvh = NewMethod(volumePresentation, planSetup, roi4);
                        result.AppendFormat("{0},{1},{2},{3},{4:0.000},{5:0.000},{6:0.000},{7:0.000},{8:0.000},{9:0.000},{10:0.000},{11:0.000}\n", patient.Id, course.Id, planSetup.Id, roi4.Id, dvh.MeanDose.Dose, dvh.MaxDose.Dose, dvh.MinDose.Dose, D2, D5, D98, D95, D50);
                    }
var roi5 = planSetup.StructureSet.Structures.FirstOrDefault(o => o.Id.ToUpper().Equals("BLASE_P"));
                    if (roi5 != null) 
                    {
                        planSetup.DoseValuePresentation = DoseValuePresentation.Absolute;
                        //double V40 = planSetup.GetVolumeAtDose(roi5, d40, volumePresentation);
                        double D2 = planSetup.GetDoseAtVolume(roi5, 2, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D98 = planSetup.GetDoseAtVolume(roi5, 98, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D5 = planSetup.GetDoseAtVolume(roi5, 5, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D95 = planSetup.GetDoseAtVolume(roi5, 95, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D50 = planSetup.GetDoseAtVolume(roi5, 50, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        DVHData dvh = NewMethod(volumePresentation, planSetup, roi5);
                        result.AppendFormat("{0},{1},{2},{3},{4:0.000},{5:0.000},{6:0.000},{7:0.000},{8:0.000},{9:0.000},{10:0.000},{11:0.000}\n", patient.Id, course.Id, planSetup.Id, roi5.Id, dvh.MeanDose.Dose, dvh.MaxDose.Dose, dvh.MinDose.Dose, D2, D5, D98, D95, D50);
                    }
var roi6 = planSetup.StructureSet.Structures.FirstOrDefault(o => o.Id.ToUpper().Equals("REKTUM_P"));
                    if (roi6 != null) 
                    {
                        planSetup.DoseValuePresentation = DoseValuePresentation.Absolute;
                        //double V40 = planSetup.GetVolumeAtDose(roi6, d40, volumePresentation);
                        double D2 = planSetup.GetDoseAtVolume(roi6, 2, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D98 = planSetup.GetDoseAtVolume(roi6, 98, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D5 = planSetup.GetDoseAtVolume(roi6, 5, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D95 = planSetup.GetDoseAtVolume(roi6, 95, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D50 = planSetup.GetDoseAtVolume(roi6, 50, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        DVHData dvh = NewMethod(volumePresentation, planSetup, roi6);
                        result.AppendFormat("{0},{1},{2},{3},{4:0.000},{5:0.000},{6:0.000},{7:0.000},{8:0.000},{9:0.000},{10:0.000},{11:0.000}\n", patient.Id, course.Id, planSetup.Id, roi6.Id, dvh.MeanDose.Dose, dvh.MaxDose.Dose, dvh.MinDose.Dose, D2, D5, D98, D95, D50);
                    }

var roi7 = planSetup.StructureSet.Structures.FirstOrDefault(o => o.Id.ToUpper().Equals("BODY"));
                    if (roi7 != null) 
                    {
                        planSetup.DoseValuePresentation = DoseValuePresentation.Absolute;
                        //double V40 = planSetup.GetVolumeAtDose(roi7, d40, volumePresentation);
                        double D2 = planSetup.GetDoseAtVolume(roi7, 2, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D98 = planSetup.GetDoseAtVolume(roi7, 98, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D5 = planSetup.GetDoseAtVolume(roi7, 5, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D95 = planSetup.GetDoseAtVolume(roi7, 95, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        double D50 = planSetup.GetDoseAtVolume(roi7, 50, VolumePresentation.Relative, DoseValuePresentation.Absolute).Dose;
                        DVHData dvh = NewMethod(volumePresentation, planSetup, roi7);
                        result.AppendFormat("{0},{1},{2},{3},{4:0.000},{5:0.000},{6:0.000},{7:0.000},{8:0.000},{9:0.000},{10:0.000},{11:0.000}\n", patient.Id, course.Id, planSetup.Id, roi7.Id, dvh.MeanDose.Dose, dvh.MaxDose.Dose, dvh.MinDose.Dose, D2, D5, D98, D95, D50);
                    }
                }
            }
            // Write results to file
string filename = string.Format(@"{0}\Metrics_{1}_.csv", outputDestinationDirectory,patient.Id); 
//string filename = String.Format("CBCT_{0}.csv", patient.Id);
            //string filePath = Path.Combine(outputDestinationDirectory, outputFileName);
            File.WriteAllText(filename, result.ToString());
            string message = string.Format("Metrics for OARs are written to: {0}", outputDestinationDirectory);
            MessageBox.Show(message, "SUCCESS");
        }
    }
}
