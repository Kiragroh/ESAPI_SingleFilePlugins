using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace VMS.TPS
{
  public class Script
  {
    public Script()
    {
    }

    public void Execute(ScriptContext context /*, System.Windows.Window window, ScriptEnvironment environment*/)
    {
// Enter Code here:
        // declare local variables that reference the objects we need.
        PlanSetup planSetup = context.PlanSetup;
        Patient p = context.Patient;
            string msg = "";
        
        StructureSet ss = context.StructureSet;
        if (ss == null)
        {
            MessageBox.Show("Please load a structure set before running this script.");
            return;
        }
            //var listStructures = context.StructureSet.Structures;
            // 'listStructures' if of type IEnumerable<Structure>
            var listStructures = ss.Structures;

        foreach (Structure s in listStructures)
            {


                //if (b.MLCPlanType.ToString() == "VMAT" && tableAngle == 0)
                //{
                //msg += string.Format("Structure-ID: {0}, Volume[cc]: {1:0.0000}\n, HD?: {2}", s.Id, s.Volume, s.IsHighResolution?"true":"false");
                msg += string.Format("Structure-ID:, {0}, Volume[cc]:, {1:0.0000}\n", s.Id, s.Volume);
                //MessageBox.Show(msg, "MG-Plugin"); 
                //}
            }
            MessageBox.Show(msg, "MG-Plugin");
            // loop through structure list and find biggest structure

            //string msg = string.Format("Found {0} normal structures.\rThe one with the largest volume is {1}.\rVolume is {2} cc.", structureCount, structureName, Math.Round(maxVolume, 2));
            //MessageBox.Show(msg, "MG-Plugin"); 
 		string filename = string.Format(@"D:\VolData_{0}_{1}.csv", ss.Id, planSetup.Id.Replace(",", string.Empty));
                using (System.IO.StreamWriter sw = new
                System.IO.StreamWriter(filename, false, Encoding.ASCII))
                {
                    sw.Write(msg);
                    //sw.Write(msgunion);
                }
        }
  }
}